
using SalaoBeleza.DTOs;
using SalaoBeleza.Models;
using SalaoBeleza.Repositories;

namespace SalaoBeleza.Services;

public class AgendamentoService
{
    private readonly IAgendamentoRepository _repo;
    private readonly IClienteRepository _clienteRepo;
    private readonly IServicoRepository _servicoRepo;
    private readonly IHorarioProfissionalRepository _horarioRepo;

    public AgendamentoService(
        IAgendamentoRepository repo,
        IClienteRepository clienteRepo,
        IServicoRepository servicoRepo,
        IHorarioProfissionalRepository horarioRepo)
    {
        _repo = repo;
        _clienteRepo = clienteRepo;
        _servicoRepo = servicoRepo;
        _horarioRepo = horarioRepo;
    }

    public async Task<List<AgendamentoRespostaDTO>> Listar()
    {
        var itens = await _repo.ListarTodos();
        return itens.Select(CriarResposta).ToList();
    }

    public async Task<List<AgendamentoRespostaDTO>> ListarPorCliente(int clienteId)
    {
        var itens = await _repo.ListarPorCliente(clienteId);
        return itens.Select(CriarResposta).ToList();
    }

    public async Task<AgendamentoRespostaDTO> Criar(AgendamentoCriarDTO dto)
    {
        if (dto.DataHora <= DateTime.Now)
            throw new InvalidOperationException("A data e o horário do agendamento devem estar no futuro.");

        if (dto.ClienteId <= 0)
            throw new InvalidOperationException("Cliente inválido.");

        if (dto.ProfissionalId <= 0)
            throw new InvalidOperationException("Selecione uma profissional válida.");

        if (dto.ComboId.HasValue && dto.ComboId.Value <= 0)
            throw new InvalidOperationException("O combo selecionado é inválido.");

        var ids = (dto.ServicoIds ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (ids.Count == 0 && dto.ServicoId.GetValueOrDefault() > 0)
            ids.Add(dto.ServicoId!.Value);

        bool temCombo = dto.ComboId.HasValue;

        if (ids.Count == 0 && !temCombo)
            throw new InvalidOperationException("Selecione pelo menos um serviço ou um combo.");

        var cliente = await _clienteRepo.BuscarPorId(dto.ClienteId)
            ?? throw new InvalidOperationException("Cliente não encontrado.");

        var servicosSelecionados = new List<Servico>();

        foreach (var id in ids)
        {
            var servico = await _servicoRepo.BuscarPorId(id);

            if (servico == null)
                throw new InvalidOperationException($"O serviço de ID {id} não foi encontrado.");

            servicosSelecionados.Add(servico);
        }

        Combo? combo = null;

        if (temCombo)
        {
            combo = await _repo.BuscarComboPorId(dto.ComboId!.Value)
                ?? throw new InvalidOperationException("Combo não encontrado.");

            if (combo.ComboServicos == null || combo.ComboServicos.Count == 0)
                throw new InvalidOperationException("O combo selecionado não possui serviços.");
        }

        // Remove os serviços que já fazem parte do combo.
        // Eles não podem ser cobrados nem ter a duração somada novamente.
        servicosSelecionados = RemoverServicosIncluidosNoCombo(
            combo,
            servicosSelecionados
        );

        decimal precoTotal = (combo?.Preco ?? 0)
            + servicosSelecionados.Sum(s => s.Preco);

        int duracaoTotal = (combo == null ? 0 : ObterDuracaoCombo(combo))
            + servicosSelecionados.Sum(s => s.DuracaoMinutos);

        if (duracaoTotal <= 0)
            throw new InvalidOperationException("Não foi possível calcular a duração do atendimento.");

        var expedientes = await _horarioRepo.BuscarPorProfissionalEDia(
            dto.ProfissionalId,
            dto.DataHora.DayOfWeek
        );

        if (!expedientes.Any())
            throw new InvalidOperationException("A profissional não trabalha neste dia.");

        var inicioNovo = dto.DataHora;
        var fimNovo = inicioNovo.AddMinutes(duracaoTotal);

        bool dentroDoExpediente = expedientes.Any(h =>
        {
            var inicioExpediente = dto.DataHora.Date.Add(h.HoraInicio);
            var fimExpediente = dto.DataHora.Date.Add(h.HoraFim);

            return inicioNovo >= inicioExpediente &&
                   fimNovo <= fimExpediente;
        });

        if (!dentroDoExpediente)
            throw new InvalidOperationException(
                "O horário escolhido não comporta a duração total dos serviços dentro do expediente."
            );

        var existentes = await _repo.BuscarPorProfissionalEData(
            dto.ProfissionalId,
            dto.DataHora
        );

        bool conflito = existentes
            .Where(a => a.Status != "Cancelado")
            .Any(a =>
            {
                var inicioExistente = a.DataHora;
                var fimExistente = inicioExistente.AddMinutes(ObterDuracao(a));

                return inicioNovo < fimExistente &&
                       fimNovo > inicioExistente;
            });

        if (conflito)
            throw new InvalidOperationException(
                "Este horário está ocupado durante parte do atendimento. Selecione outro horário."
            );

        var agendamento = new Agendamento
        {
            DataHora = dto.DataHora,
            ClienteId = dto.ClienteId,

            ServicoId = !temCombo && servicosSelecionados.Count == 1
                ? servicosSelecionados[0].Id
                : null,

            ComboId = combo?.Id,
            ProfissionalId = dto.ProfissionalId,
            Status = "Aberto",

            // Salva somente os serviços extras.
            // Os serviços que pertencem ao combo não são duplicados aqui.
            AgendamentoServicos = servicosSelecionados.Select(s =>
                new AgendamentoServico
                {
                    ServicoId = s.Id,
                    Servico = s
                }).ToList()
        };

        var criado = await _repo.Adicionar(agendamento);

        string nomeAtendimento = MontarNomeAtendimento(
            combo,
            servicosSelecionados
        );

        return new AgendamentoRespostaDTO(
            criado.Id,
            criado.DataHora,
            cliente.Nome,
            nomeAtendimento,
            precoTotal,
            criado.Status
        );
    }

    public async Task<List<object>> BuscarHorariosDisponiveis(
        int profissionalId,
        List<int>? servicoIds,
        int? comboId,
        DateTime data)
    {
        if (profissionalId <= 0)
            throw new InvalidOperationException("Selecione uma profissional válida.");

        if (comboId.HasValue && comboId.Value <= 0)
            throw new InvalidOperationException("O combo selecionado é inválido.");

        var ids = (servicoIds ?? new List<int>())
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        bool temCombo = comboId.HasValue;

        if (ids.Count == 0 && !temCombo)
            throw new InvalidOperationException("Informe pelo menos um serviço ou um combo.");

        var servicosSelecionados = new List<Servico>();

        foreach (var id in ids)
        {
            var servico = await _servicoRepo.BuscarPorId(id);

            if (servico == null)
                throw new InvalidOperationException($"O serviço de ID {id} não foi encontrado.");

            servicosSelecionados.Add(servico);
        }

        Combo? combo = null;

        if (temCombo)
        {
            combo = await _repo.BuscarComboPorId(comboId!.Value)
                ?? throw new InvalidOperationException("Combo não encontrado.");

            if (combo.ComboServicos == null || combo.ComboServicos.Count == 0)
                throw new InvalidOperationException("O combo selecionado não possui serviços.");
        }

        // Aplica a mesma regra usada na criação do agendamento.
        servicosSelecionados = RemoverServicosIncluidosNoCombo(
            combo,
            servicosSelecionados
        );

        int duracaoTotal = (combo == null ? 0 : ObterDuracaoCombo(combo))
            + servicosSelecionados.Sum(s => s.DuracaoMinutos);

        if (duracaoTotal <= 0)
            throw new InvalidOperationException("Não foi possível calcular a duração do atendimento.");

        var expedientes = await _horarioRepo.BuscarPorProfissionalEDia(
            profissionalId,
            data.DayOfWeek
        );

        var existentes = await _repo.BuscarPorProfissionalEData(
            profissionalId,
            data
        );

        var resultado = new List<object>();
        var agora = DateTime.Now;

        foreach (var expediente in expedientes)
        {
            var inicioExpediente = data.Date.Add(expediente.HoraInicio);
            var fimExpediente = data.Date.Add(expediente.HoraFim);

            for (
                var horario = inicioExpediente;
                horario.AddMinutes(duracaoTotal) <= fimExpediente;
                horario = horario.AddMinutes(30)
            )
            {
                if (data.Date == agora.Date && horario <= agora)
                    continue;

                var fimNovo = horario.AddMinutes(duracaoTotal);

                bool conflito = existentes
                    .Where(a => a.Status != "Cancelado")
                    .Any(a =>
                    {
                        var inicioExistente = a.DataHora;
                        var fimExistente = inicioExistente.AddMinutes(ObterDuracao(a));

                        return horario < fimExistente &&
                               fimNovo > inicioExistente;
                    });

                resultado.Add(new
                {
                    horario = horario.ToString("HH:mm"),
                    disponivel = !conflito
                });
            }
        }

        return resultado;
    }

    // Retira da lista os serviços que já estão incluídos no combo.
    private List<Servico> RemoverServicosIncluidosNoCombo(
        Combo? combo,
        List<Servico> servicos)
    {
        if (combo?.ComboServicos == null || combo.ComboServicos.Count == 0)
            return servicos
                .DistinctBy(s => s.Id)
                .ToList();

        var idsIncluidos = combo.ComboServicos
            .Select(cs => cs.ServicoId)
            .ToHashSet();

        return servicos
            .Where(s => !idsIncluidos.Contains(s.Id))
            .DistinctBy(s => s.Id)
            .ToList();
    }

    private int ObterDuracaoCombo(Combo combo)
    {
        if (combo.Nome.Equals(
            "Belleza completa",
            StringComparison.OrdinalIgnoreCase))
        {
            return 240;
        }

        return combo.ComboServicos.Sum(
            cs => cs.Servico?.DuracaoMinutos ?? 0
        );
    }

    private int ObterDuracao(Agendamento agendamento)
    {
        int duracaoServicos = 0;

        if (agendamento.AgendamentoServicos != null &&
            agendamento.AgendamentoServicos.Count > 0)
        {
            duracaoServicos = agendamento.AgendamentoServicos
                .Sum(x => x.Servico?.DuracaoMinutos ?? 0);
        }
        else if (agendamento.Servico != null)
        {
            duracaoServicos = agendamento.Servico.DuracaoMinutos;
        }

        int duracaoCombo = agendamento.Combo != null
            ? ObterDuracaoCombo(agendamento.Combo)
            : 0;

        return duracaoCombo + duracaoServicos;
    }

    private string MontarNomeAtendimento(
        Combo? combo,
        List<Servico> servicos)
    {
        var nomes = new List<string>();

        if (combo != null)
            nomes.Add(combo.Nome);

        nomes.AddRange(servicos.Select(s => s.Nome));

        return string.Join(" + ", nomes);
    }

    private AgendamentoRespostaDTO CriarResposta(Agendamento agendamento)
    {
        var servicos = agendamento.AgendamentoServicos?
            .Where(x => x.Servico != null)
            .Select(x => x.Servico!)
            .ToList() ?? new List<Servico>();

        if (servicos.Count == 0 && agendamento.Servico != null)
            servicos.Add(agendamento.Servico);

        var combo = agendamento.Combo;

        // Também evita duplicidade ao consultar agendamentos existentes.
        servicos = RemoverServicosIncluidosNoCombo(combo, servicos);

        decimal precoTotal = (combo?.Preco ?? 0)
            + servicos.Sum(s => s.Preco);

        string nomeAtendimento = MontarNomeAtendimento(combo, servicos);

        return new AgendamentoRespostaDTO(
            agendamento.Id,
            agendamento.DataHora,
            agendamento.Cliente?.Nome ?? "",
            nomeAtendimento,
            precoTotal,
            agendamento.Status,
            agendamento.Profissional?.Nome ?? ""
        );
    }

    public async Task<bool> MarcarComoAtendido(int id, int? profissionalId)
    {
        var agendamento = await _repo.BuscarPorId(id);

        if (agendamento == null)
            return false;

        if (profissionalId.HasValue &&
            agendamento.ProfissionalId != profissionalId.Value)
        {
            throw new InvalidOperationException(
                "Você não pode alterar o atendimento de outra profissional."
            );
        }

        if (agendamento.Status == "Cancelado")
            throw new InvalidOperationException(
                "Não é possível marcar um agendamento cancelado como atendido."
            );

        if (agendamento.Status == "Atendido")
            return true;

        if (agendamento.DataHora > DateTime.Now)
            throw new InvalidOperationException(
                "Só é possível marcar como atendido depois do horário agendado."
            );

        agendamento.Status = "Atendido";

        await _repo.Atualizar(agendamento);

        return true;
    }

    public async Task<bool> Cancelar(int id, int clienteId)
    {
        var agendamento = await _repo.BuscarPorId(id);

        if (agendamento == null || agendamento.ClienteId != clienteId)
            return false;

        if (agendamento.Status == "Cancelado")
            return true;

        if (agendamento.Status == "Atendido")
            throw new InvalidOperationException(
                "Não é possível cancelar um atendimento já realizado."
            );

        agendamento.Status = "Cancelado";

        await _repo.Atualizar(agendamento);

        return true;
    }

    public async Task<bool> Remover(int id)
    {
        var agendamento = await _repo.BuscarPorId(id);

        if (agendamento == null)
            return false;

        await _repo.Remover(agendamento);

        return true;
    }

    public async Task<bool> VerificarDiaTrabalho(
        int profissionalId,
        int diaSemana)
    {
        if (diaSemana < 0 || diaSemana > 6)
            return false;

        var horarios = await _horarioRepo.BuscarPorProfissionalEDia(
            profissionalId,
            (DayOfWeek)diaSemana
        );

        return horarios.Any();
    }

    public async Task<bool> RegistrarFormaPagamento(
        int id,
        int clienteId,
        string formaPagamento)
    {
        var agendamento = await _repo.BuscarPorId(id);

        if (agendamento == null || agendamento.ClienteId != clienteId)
            return false;

        if (formaPagamento != "Pix" && formaPagamento != "No salão")
            throw new InvalidOperationException("Forma de pagamento inválida.");

        agendamento.FormaPagamento = formaPagamento;

        await _repo.Atualizar(agendamento);

        return true;
    }
}
