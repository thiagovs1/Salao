using SalaoBeleza.DTOs;
using SalaoBeleza.Models;
using SalaoBeleza.Repositories;

namespace SalaoBeleza.Services;

public class AgendamentoService
{
    private readonly IAgendamentoRepository _repo;
    private readonly IClienteRepository _clienteRepo;
    private readonly IServicoRepository _servicoRepo;

    public AgendamentoService(
        IAgendamentoRepository repo,
        IClienteRepository clienteRepo,
        IServicoRepository servicoRepo)
    {
        _repo = repo;
        _clienteRepo = clienteRepo;
        _servicoRepo = servicoRepo;
    }

    public async Task<List<AgendamentoRespostaDTO>> Listar()
    {
        var agendamentos = await _repo.ListarTodos();

        return agendamentos.Select(a =>
        {
            var servicos = a.AgendamentoServicos
                .Where(x => x.Servico != null)
                .Select(x => x.Servico!.Nome)
                .ToList();

            var precoTotal = a.AgendamentoServicos
                .Where(x => x.Servico != null)
                .Sum(x => x.Servico!.Preco);

            return new AgendamentoRespostaDTO(
                a.Id,
                a.DataHora,
                a.Cliente?.Nome ?? "",
                a.Profissional?.Nome ?? "",
                servicos,
                precoTotal,
                a.Status
            );
        }).ToList();
    }

    public async Task<AgendamentoRespostaDTO> Criar(
        AgendamentoCriarDTO dto)
    {
        if (dto.DataHora < DateTime.Now)
            throw new Exception(
                "A data do agendamento nao pode estar no passado.");

        if (dto.ServicoIds == null || dto.ServicoIds.Count == 0)
            throw new Exception(
                "Selecione pelo menos um servico.");

        var cliente = await _clienteRepo.BuscarPorId(dto.ClienteId);

        if (cliente == null)
            throw new Exception("Cliente nao encontrado.");

        if (dto.ProfissionalId <= 0)
            throw new Exception("Profissional nao informado.");

        var servicos = new List<Servico>();

        foreach (var idServico in dto.ServicoIds.Distinct())
        {
            var servico = await _servicoRepo.BuscarPorId(idServico);

            if (servico == null)
                throw new Exception(
                    $"Servico {idServico} nao encontrado.");

            servicos.Add(servico);
        }

        var duracaoTotal = servicos.Sum(
            s => s.DuracaoMinutos
        );

        var dataHoraInicio = dto.DataHora;
        var dataHoraFim = dataHoraInicio.AddMinutes(duracaoTotal);

        var horarios = await _repo.ListarHorariosProfissional(
            dto.ProfissionalId
        );

        var diaSemana = ObterDiaSemana(
            dataHoraInicio.DayOfWeek
        );

        var horariosDoDia = horarios
            .Where(h =>
                string.Equals(
                    h.DiaSemana,
                    diaSemana,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            .ToList();

        if (horariosDoDia.Count == 0)
            throw new Exception(
                "O profissional nao atende neste dia.");

        var estaDentroDoHorario = horariosDoDia.Any(h =>
        {
            var inicioPeriodo = dataHoraInicio.Date.Add(h.HoraInicio);
            var fimPeriodo = dataHoraInicio.Date.Add(h.HoraFim);

            return dataHoraInicio >= inicioPeriodo &&
                   dataHoraFim <= fimPeriodo;
        });

        if (!estaDentroDoHorario)
            throw new Exception(
                "O horario escolhido esta fora do horario de atendimento.");

        var conflito = await _repo.ExisteConflito(
            dto.ProfissionalId,
            dataHoraInicio,
            dataHoraFim
        );

        if (conflito)
            throw new Exception(
                "Esse horario ja esta ocupado.");

        var agendamento = new Agendamento
        {
            DataHora = dataHoraInicio,
            ClienteId = dto.ClienteId,
            ProfissionalId = dto.ProfissionalId,
            Status = "Pendente"
        };

        foreach (var servico in servicos)
        {
            agendamento.AgendamentoServicos.Add(
                new AgendamentoServico
                {
                    Servico = servico
                }
            );
        }

        var criado = await _repo.Adicionar(agendamento);

        return new AgendamentoRespostaDTO(
            criado.Id,
            criado.DataHora,
            cliente.Nome,
            "Profissional",
            servicos.Select(s => s.Nome).ToList(),
            servicos.Sum(s => s.Preco),
            criado.Status
        );
    }

    public async Task<bool> Remover(int id)
    {
        var agendamento = await _repo.BuscarPorId(id);

        if (agendamento == null)
            return false;

        await _repo.Remover(agendamento);

        return true;
    }

    public async Task<List<CalendarioDiaDTO>> Calendario(
        int profissionalId,
        int ano,
        int mes)
    {
        var primeiroDia = new DateTime(ano, mes, 1);
        var ultimoDia = primeiroDia.AddMonths(1).AddDays(-1);

        var horarios = await _repo.ListarHorariosProfissional(
            profissionalId
        );

        var resultado = new List<CalendarioDiaDTO>();

        for (
            var data = primeiroDia;
            data <= ultimoDia;
            data = data.AddDays(1))
        {
            var diaSemana = ObterDiaSemana(
                data.DayOfWeek
            );

            var trabalha = horarios.Any(h =>
                string.Equals(
                    h.DiaSemana,
                    diaSemana,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            var disponivel =
                trabalha &&
                data.Date >= DateTime.Today;

            resultado.Add(
                new CalendarioDiaDTO(
                    data.ToString("yyyy-MM-dd"),
                    disponivel
                )
            );
        }

        return resultado;
    }

    public async Task<List<HorarioDisponivelDTO>> Horarios(
        int profissionalId,
        int servicoId,
        DateTime data)
    {
        var servico = await _servicoRepo.BuscarPorId(servicoId);

        if (servico == null)
            throw new Exception("Servico nao encontrado.");

        return await GerarHorarios(
            profissionalId,
            data,
            servico.DuracaoMinutos
        );
    }

    public async Task<List<HorarioDisponivelDTO>> Horarios(
        int profissionalId,
        List<int> servicoIds,
        DateTime data)
    {
        if (servicoIds == null || servicoIds.Count == 0)
            throw new Exception("Nenhum servico informado.");

        var servicos = new List<Servico>();

        foreach (var id in servicoIds.Distinct())
        {
            var servico = await _servicoRepo.BuscarPorId(id);

            if (servico == null)
                throw new Exception(
                    $"Servico {id} nao encontrado.");

            servicos.Add(servico);
        }

        var duracaoTotal = servicos.Sum(
            s => s.DuracaoMinutos
        );

        return await GerarHorarios(
            profissionalId,
            data,
            duracaoTotal
        );
    }

    private async Task<List<HorarioDisponivelDTO>> GerarHorarios(
        int profissionalId,
        DateTime data,
        int duracaoMinutos)
    {
        var agendamentos = await _repo.ListarAgendamentosDoDia(
            profissionalId,
            data
        );

        var resultado = new List<HorarioDisponivelDTO>();

        var periodos = new[]
        {
            new
            {
                Inicio = new TimeSpan(8, 0, 0),
                Fim = new TimeSpan(11, 0, 0)
            },
            new
            {
                Inicio = new TimeSpan(13, 0, 0),
                Fim = new TimeSpan(18, 0, 0)
            }
        };

        foreach (var periodo in periodos)
        {
            var atual = data.Date.Add(periodo.Inicio);
            var fimPeriodo = data.Date.Add(periodo.Fim);

            while (atual.AddMinutes(duracaoMinutos) <= fimPeriodo)
            {
                var fim = atual.AddMinutes(duracaoMinutos);

                var ocupado = false;

                foreach (var agendamento in agendamentos)
                {
                    var duracaoAgendamento =
                        agendamento.AgendamentoServicos
                            .Where(x => x.Servico != null)
                            .Sum(x => x.Servico!.DuracaoMinutos);

                    var inicioExistente = agendamento.DataHora;

                    var fimExistente =
                        inicioExistente.AddMinutes(
                            duracaoAgendamento
                        );

                    if (
                        atual < fimExistente &&
                        fim > inicioExistente)
                    {
                        ocupado = true;
                        break;
                    }
                }

                if (!ocupado &&
                    (data.Date > DateTime.Today ||
                     atual >= DateTime.Now))
                {
                    resultado.Add(
                        new HorarioDisponivelDTO(
                            atual.ToString("HH:mm"),
                            fim.ToString("HH:mm")
                        )
                    );
                }

                atual = atual.AddMinutes(30);
            }
        }

        return resultado;
    }

    private static string ObterDiaSemana(DayOfWeek dia)
    {
        return dia switch
        {
            DayOfWeek.Sunday => "Domingo",
            DayOfWeek.Monday => "Segunda",
            DayOfWeek.Tuesday => "Terça",
            DayOfWeek.Wednesday => "Quarta",
            DayOfWeek.Thursday => "Quinta",
            DayOfWeek.Friday => "Sexta",
            DayOfWeek.Saturday => "Sábado",
            _ => ""
        };
    }
}