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
        var agendamentos =
            await _repo.ListarTodos();

        await AtualizarStatusAgendamentos(
            agendamentos
        );

        return agendamentos
            .Select(CriarResposta)
            .ToList();
    }

    public async Task<List<AgendamentoRespostaDTO>> ListarPorCliente(
        int clienteId)
    {
        var agendamentos =
            await _repo.ListarPorCliente(
                clienteId
            );

        await AtualizarStatusAgendamentos(
            agendamentos
        );

        return agendamentos
            .Select(CriarResposta)
            .ToList();
    }

    public async Task<AgendamentoRespostaDTO> Criar(
        AgendamentoCriarDTO dto)
    {
        // ---------------------------------------------
        // VALIDA DATA
        // ---------------------------------------------

        if (dto.DataHora < DateTime.Now)
        {
            throw new Exception(
                "A data do agendamento não pode estar no passado."
            );
        }

        // ---------------------------------------------
        // VALIDA SERVIÇO OU COMBO
        // ---------------------------------------------

        if (
            dto.ServicoId == null &&
            dto.ComboId == null
        )
        {
            throw new Exception(
                "É necessário selecionar um serviço ou um combo."
            );
        }

        if (
            dto.ServicoId != null &&
            dto.ComboId != null
        )
        {
            throw new Exception(
                "O agendamento não pode possuir serviço e combo ao mesmo tempo."
            );
        }

        // ---------------------------------------------
        // CONFERE CLIENTE
        // ---------------------------------------------

        var cliente =
            await _clienteRepo.BuscarPorId(
                dto.ClienteId
            );

        if (cliente == null)
        {
            throw new Exception(
                "Cliente não encontrado."
            );
        }

        // ---------------------------------------------
        // VARIÁVEIS DO ATENDIMENTO
        // ---------------------------------------------

        string nomeAtendimento;

        decimal precoAtendimento;

        int duracaoAtendimento;

        // ---------------------------------------------
        // SERVIÇO
        // ---------------------------------------------

        if (dto.ServicoId != null)
        {
            var servico =
                await _servicoRepo.BuscarPorId(
                    dto.ServicoId.Value
                );

            if (servico == null)
            {
                throw new Exception(
                    "Serviço não encontrado."
                );
            }

            nomeAtendimento =
                servico.Nome;

            precoAtendimento =
                servico.Preco;

            duracaoAtendimento =
                servico.DuracaoMinutos;
        }

        // ---------------------------------------------
        // COMBO
        // ---------------------------------------------

        else
        {
            var combo =
                await _repo.BuscarComboPorId(
                    dto.ComboId!.Value
                );

            if (combo == null)
            {
                throw new Exception(
                    "Combo não encontrado."
                );
            }

            if (
                combo.ComboServicos == null ||
                combo.ComboServicos.Count == 0
            )
            {
                throw new Exception(
                    "O combo selecionado não possui serviços."
                );
            }

            nomeAtendimento =
                combo.Nome;

            precoAtendimento =
                combo.Preco;

            duracaoAtendimento =
                ObterDuracaoCombo(combo);
        }

        // ---------------------------------------------
        // HORÁRIOS DA PROFISSIONAL
        // ---------------------------------------------

        var horariosTrabalho =
            await _horarioRepo.BuscarPorProfissionalEDia(
                dto.ProfissionalId,
                dto.DataHora.DayOfWeek
            );

        if (!horariosTrabalho.Any())
        {
            throw new Exception(
                "A profissional não trabalha neste dia."
            );
        }

        // ---------------------------------------------
        // INÍCIO E FIM DO ATENDIMENTO
        // ---------------------------------------------

        var inicioNovo =
            dto.DataHora;

        var fimNovo =
            dto.DataHora.AddMinutes(
                duracaoAtendimento
            );

        // ---------------------------------------------
        // CONFERE EXPEDIENTE
        // ---------------------------------------------

        var dentroDoExpediente =
            horariosTrabalho.Any(h =>
            {
                var inicioExpediente =
                    dto.DataHora.Date.Add(
                        h.HoraInicio
                    );

                var fimExpediente =
                    dto.DataHora.Date.Add(
                        h.HoraFim
                    );

                return
                    inicioNovo >= inicioExpediente &&
                    fimNovo <= fimExpediente;
            });

        if (!dentroDoExpediente)
        {
            throw new Exception(
                "O horário escolhido está fora do expediente da profissional."
            );
        }

        // ---------------------------------------------
        // BUSCA AGENDAMENTOS EXISTENTES
        // ---------------------------------------------

        var agendamentos =
            await _repo.BuscarPorProfissionalEData(
                dto.ProfissionalId,
                dto.DataHora
            );

        // ---------------------------------------------
        // CONFERE CONFLITO
        // ---------------------------------------------

        var conflito =
            agendamentos.Any(a =>
            {
                var inicioExistente =
                    a.DataHora;

                var duracaoExistente =
                    ObterDuracao(a);

                var fimExistente =
                    inicioExistente.AddMinutes(
                        duracaoExistente
                    );

                return
                    inicioNovo < fimExistente &&
                    fimNovo > inicioExistente;
            });

        if (conflito)
        {
            throw new Exception(
                "Este horário já está ocupado."
            );
        }

        // ---------------------------------------------
        // CRIA AGENDAMENTO
        // ---------------------------------------------

        var agendamento =
            new Agendamento
            {
                DataHora =
                    dto.DataHora,

                ClienteId =
                    dto.ClienteId,

                ServicoId =
                    dto.ServicoId,

                ComboId =
                    dto.ComboId,

                ProfissionalId =
                    dto.ProfissionalId,

                Status =
                    "Aberto"
            };

        var criado =
            await _repo.Adicionar(
                agendamento
            );

        return new AgendamentoRespostaDTO(
            criado.Id,
            criado.DataHora,
            cliente.Nome,
            nomeAtendimento,
            precoAtendimento,
            criado.Status
        );
    }

    // =====================================================
    // HORÁRIOS DISPONÍVEIS
    // =====================================================

    public async Task<List<object>> BuscarHorariosDisponiveis(
        int profissionalId,
        int? servicoId,
        int? comboId,
        DateTime data)
    {
        // ---------------------------------------------
        // CONFERE SERVIÇO OU COMBO
        // ---------------------------------------------

        if (
            servicoId == null &&
            comboId == null
        )
        {
            throw new Exception(
                "É necessário informar um serviço ou um combo."
            );
        }

        if (
            servicoId != null &&
            comboId != null
        )
        {
            throw new Exception(
                "Informe apenas um serviço ou um combo."
            );
        }

        // ---------------------------------------------
        // DESCOBRE DURAÇÃO
        // ---------------------------------------------

        int duracaoAtendimento;

        if (servicoId != null)
        {
            var servico =
                await _servicoRepo.BuscarPorId(
                    servicoId.Value
                );

            if (servico == null)
            {
                throw new Exception(
                    "Serviço não encontrado."
                );
            }

            duracaoAtendimento =
                servico.DuracaoMinutos;
        }
        else
        {
            var combo =
                await _repo.BuscarComboPorId(
                    comboId!.Value
                );

            if (combo == null)
            {
                throw new Exception(
                    "Combo não encontrado."
                );
            }

            if (
                combo.ComboServicos == null ||
                combo.ComboServicos.Count == 0
            )
            {
                throw new Exception(
                    "O combo selecionado não possui serviços."
                );
            }

            duracaoAtendimento =
                ObterDuracaoCombo(combo);
        }

        // ---------------------------------------------
        // BUSCA EXPEDIENTE
        // ---------------------------------------------

        var horariosTrabalho =
            await _horarioRepo.BuscarPorProfissionalEDia(
                profissionalId,
                data.DayOfWeek
            );

        // ---------------------------------------------
        // BUSCA AGENDAMENTOS EXISTENTES
        // ---------------------------------------------

        var agendamentos =
            await _repo.BuscarPorProfissionalEData(
                profissionalId,
                data
            );

        var horarios =
            new List<object>();

        // ---------------------------------------------
        // PERCORRE CADA HORÁRIO DE TRABALHO
        // ---------------------------------------------

        foreach (var horarioTrabalho in horariosTrabalho)
        {
            var inicio =
                data.Date.Add(
                    horarioTrabalho.HoraInicio
                );

            var fim =
                data.Date.Add(
                    horarioTrabalho.HoraFim
                );

            // Horários começam de 30 em 30 minutos.
            //
            // A duração do serviço NÃO é 30 minutos.
            // 30 minutos é apenas o intervalo entre
            // os horários possíveis de início.

            for (
                var horario = inicio;
                horario.AddMinutes(
                    duracaoAtendimento
                ) <= fim;
                horario = horario.AddMinutes(30)
            )
            {
                var inicioNovo =
                    horario;

                var fimNovo =
                    horario.AddMinutes(
                        duracaoAtendimento
                    );

                // -------------------------------------
                // CONFERE CONFLITOS
                // -------------------------------------

                var conflito =
                    agendamentos.Any(a =>
                    {
                        var inicioExistente =
                            a.DataHora;

                        var duracaoExistente =
                            ObterDuracao(a);

                        var fimExistente =
                            inicioExistente.AddMinutes(
                                duracaoExistente
                            );

                        return
                            inicioNovo < fimExistente &&
                            fimNovo > inicioExistente;
                    });

                horarios.Add(
                    new
                    {
                        horario =
                            horario.ToString("HH:mm"),

                        disponivel =
                            !conflito
                    }
                );
            }
        }

        return horarios;
    }

    // =====================================================
    // DURAÇÃO DO COMBO
    // =====================================================

    private int ObterDuracaoCombo(
        Combo combo)
    {
        // Belleza completa possui duração fixa de 4 horas.
        if (
            combo.Nome.Equals(
                "Belleza completa",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return 240;
        }

        // Os demais combos continuam usando
        // a soma das durações dos serviços.
        return combo.ComboServicos
            .Sum(cs =>
                cs.Servico.DuracaoMinutos
            );
    }

    // =====================================================
    // DURAÇÃO DE UM AGENDAMENTO EXISTENTE
    // =====================================================

    private int ObterDuracao(
        Agendamento agendamento)
    {
        // Serviço normal.
        if (agendamento.Servico != null)
        {
            return agendamento.Servico.DuracaoMinutos;
        }

        // Combo.
        if (agendamento.Combo != null)
        {
            return ObterDuracaoCombo(
                agendamento.Combo
            );
        }

        return 0;
    }

    // =====================================================
    // RESPOSTA DA API
    // =====================================================

    private AgendamentoRespostaDTO CriarResposta(
        Agendamento agendamento)
    {
        if (agendamento.Combo != null)
        {
            return new AgendamentoRespostaDTO(
                agendamento.Id,
                agendamento.DataHora,
                agendamento.Cliente?.Nome ?? "",
                agendamento.Combo.Nome,
                agendamento.Combo.Preco,
                agendamento.Status
            );
        }

        return new AgendamentoRespostaDTO(
            agendamento.Id,
            agendamento.DataHora,
            agendamento.Cliente?.Nome ?? "",
            agendamento.Servico?.Nome ?? "",
            agendamento.Servico?.Preco ?? 0,
            agendamento.Status
        );
    }

    // =====================================================
    // CANCELAR
    // =====================================================

    public async Task<bool> Cancelar(
        int id,
        int clienteId)
    {
        var agendamento =
            await _repo.BuscarPorId(id);

        if (agendamento == null)
            return false;

        if (agendamento.ClienteId != clienteId)
            return false;

        if (agendamento.Status == "Cancelado")
            return true;

        agendamento.Status =
            "Cancelado";

        await _repo.Atualizar(
            agendamento
        );

        return true;
    }

    // =====================================================
    // REMOVER
    // =====================================================

    public async Task<bool> Remover(
        int id)
    {
        var agendamento =
            await _repo.BuscarPorId(id);

        if (agendamento == null)
            return false;

        await _repo.Remover(
            agendamento
        );

        return true;
    }

    // =====================================================
    // VERIFICAR DIA DE TRABALHO
    // =====================================================

    public async Task<bool> VerificarDiaTrabalho(
        int profissionalId,
        int diaSemana)
    {
        if (
            diaSemana < 0 ||
            diaSemana > 6
        )
        {
            return false;
        }

        var dia =
            (DayOfWeek)diaSemana;

        var horarios =
            await _horarioRepo.BuscarPorProfissionalEDia(
                profissionalId,
                dia
            );

        return horarios.Any();
    }

    // =====================================================
    // ATUALIZA STATUS
    // =====================================================

    private async Task AtualizarStatusAgendamentos(
        List<Agendamento> agendamentos)
    {
        var houveAlteracao =
            false;

        foreach (var agendamento in agendamentos)
        {
            if (
                agendamento.Status ==
                "Cancelado"
            )
            {
                continue;
            }

            if (
                agendamento.DataHora <= DateTime.Now &&
                agendamento.Status != "Confirmado"
            )
            {
                agendamento.Status =
                    "Confirmado";

                houveAlteracao =
                    true;
            }

            if (
                agendamento.DataHora > DateTime.Now &&
                string.IsNullOrWhiteSpace(
                    agendamento.Status
                )
            )
            {
                agendamento.Status =
                    "Aberto";

                houveAlteracao =
                    true;
            }
        }

        if (houveAlteracao)
        {
            await SalvarStatus(
                agendamentos
            );
        }
    }

    private async Task SalvarStatus(
        List<Agendamento> agendamentos)
    {
        foreach (var agendamento in agendamentos)
        {
            if (
                agendamento.Status ==
                    "Confirmado"
                ||
                agendamento.Status ==
                    "Aberto"
            )
            {
                await _repo.Atualizar(
                    agendamento
                );
            }
        }
    }
}