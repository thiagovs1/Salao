using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Belleza", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("Belleza");


// ============================================
// CONEXÃO COM O BANCO
// ============================================

string conexao =
    "Server=localhost;" +
    "Port=3306;" +
    "Database=Belleza;" +
    "User ID=root;" +
    "Password=;";


// ============================================
// CALENDÁRIO
// ============================================

app.MapGet(
    "/api/agendamento/calendario",
    async (
        int profissionalId,
        int servicoId,
        int ano,
        int mes) =>
    {

        var resultado =
            new List<object>();


        using var connection =
            new MySqlConnection(conexao);

        await connection.OpenAsync();


        // ====================================
        // DURAÇÃO DO SERVIÇO
        // ====================================

        int duracaoServico;


        using (
            var cmdServico =
                new MySqlCommand(
                    @"SELECT duracaominutos
                      FROM Servico
                      WHERE id = @id",
                    connection))
        {

            cmdServico.Parameters.AddWithValue(
                "@id",
                servicoId);


            var valor =
                await cmdServico
                    .ExecuteScalarAsync();


            if (valor == null)
            {

                return Results.NotFound(
                    "Serviço não encontrado.");

            }


            duracaoServico =
                Convert.ToInt32(valor);

        }


        // ====================================
        // QUANTIDADE DE DIAS DO MÊS
        // ====================================

        int quantidadeDias =
            DateTime.DaysInMonth(
                ano,
                mes);


        for (
            int dia = 1;
            dia <= quantidadeDias;
            dia++)
        {

            DateTime data =
                new DateTime(
                    ano,
                    mes,
                    dia);


            string diaSemana =
                TraduzirDiaSemana(
                    data.DayOfWeek);


            // =================================
            // HORÁRIO DE TRABALHO
            // =================================

            bool trabalha =
                false;


            var horariosTrabalho =
                new List<(TimeSpan inicio, TimeSpan fim)>();


            using (
                var cmdHorario =
                    new MySqlCommand(
                        @"SELECT hora_inicio, hora_fim
                          FROM HorarioProfissional
                          WHERE idProfissional = @profissional
                          AND dia_semana = @dia",
                        connection))
            {

                cmdHorario.Parameters.AddWithValue(
                    "@profissional",
                    profissionalId);

                cmdHorario.Parameters.AddWithValue(
                    "@dia",
                    diaSemana);


                using var reader =
                    await cmdHorario
                        .ExecuteReaderAsync();


                while (
                    await reader.ReadAsync())
                {

                    trabalha = true;


                    horariosTrabalho.Add(
                        (
                            reader.GetTimeSpan(0),
                            reader.GetTimeSpan(1)
                        )
                    );

                }

            }


            if (!trabalha)
            {

                resultado.Add(
                    new
                    {
                        dia,
                        disponivel = false
                    });

                continue;

            }


            // =================================
            // VERIFICAR SE EXISTE ALGUM HORÁRIO
            // =================================

            bool existeHorario =
                false;


            foreach (
                var periodo
                in horariosTrabalho)
            {

                var inicio =
                    periodo.inicio;


                while (
                    inicio.Add(
                        TimeSpan.FromMinutes(
                            duracaoServico))
                    <= periodo.fim)
                {

                    var fim =
                        inicio.Add(
                            TimeSpan.FromMinutes(
                                duracaoServico));


                    bool ocupado =
                        await HorarioOcupado(
                            connection,
                            profissionalId,
                            data,
                            inicio,
                            fim);


                    if (!ocupado)
                    {

                        existeHorario = true;

                        break;

                    }


                    inicio =
                        inicio.Add(
                            TimeSpan.FromMinutes(30));

                }


                if (existeHorario)
                    break;

            }


            resultado.Add(
                new
                {
                    dia,
                    disponivel =
                        existeHorario
                });

        }


        return Results.Ok(
            resultado);

    });


// ============================================
// HORÁRIOS DE UM DIA
// ============================================

app.MapGet(
    "/api/agendamento/horarios",
    async (
        int profissionalId,
        int servicoId,
        DateTime data) =>
    {

        var resultado =
            new List<object>();


        using var connection =
            new MySqlConnection(conexao);

        await connection.OpenAsync();


        // ====================================
        // DURAÇÃO DO SERVIÇO
        // ====================================

        int duracaoServico;


        using (
            var cmdServico =
                new MySqlCommand(
                    @"SELECT duracaominutos
                      FROM Servico
                      WHERE id = @id",
                    connection))
        {

            cmdServico.Parameters.AddWithValue(
                "@id",
                servicoId);


            var valor =
                await cmdServico
                    .ExecuteScalarAsync();


            if (valor == null)
            {

                return Results.NotFound(
                    "Serviço não encontrado.");

            }


            duracaoServico =
                Convert.ToInt32(valor);

        }


        string diaSemana =
            TraduzirDiaSemana(
                data.DayOfWeek);


        // ====================================
        // HORÁRIOS DO PROFISSIONAL
        // ====================================

        using (
            var cmd =
                new MySqlCommand(
                    @"SELECT hora_inicio, hora_fim
                      FROM HorarioProfissional
                      WHERE idProfissional = @profissional
                      AND dia_semana = @dia",
                    connection))
        {

            cmd.Parameters.AddWithValue(
                "@profissional",
                profissionalId);

            cmd.Parameters.AddWithValue(
                "@dia",
                diaSemana);


            using var reader =
                await cmd.ExecuteReaderAsync();


            var periodos =
                new List<(
                    TimeSpan inicio,
                    TimeSpan fim
                )>();


            while (
                await reader.ReadAsync())
            {

                periodos.Add(
                    (
                        reader.GetTimeSpan(0),
                        reader.GetTimeSpan(1)
                    )
                );

            }


            reader.Close();


            // =================================
            // GERAR HORÁRIOS
            // =================================

            foreach (
                var periodo
                in periodos)
            {

                TimeSpan inicio =
                    periodo.inicio;


                while (
                    inicio.Add(
                        TimeSpan.FromMinutes(
                            duracaoServico))
                    <= periodo.fim)
                {

                    TimeSpan fim =
                        inicio.Add(
                            TimeSpan.FromMinutes(
                                duracaoServico));


                    bool ocupado =
                        await HorarioOcupado(
                            connection,
                            profissionalId,
                            data,
                            inicio,
                            fim);


                    resultado.Add(
                        new
                        {
                            inicio =
                                inicio.ToString(
                                    @"hh\:mm"),

                            fim =
                                fim.ToString(
                                    @"hh\:mm"),

                            disponivel =
                                !ocupado
                        });


                    inicio =
                        inicio.Add(
                            TimeSpan.FromMinutes(30));

                }

            }

        }


        return Results.Ok(
            resultado);

    });


// ============================================
// INICIAR SERVIDOR
// ============================================

app.Run();


// ============================================
// VERIFICAR CONFLITO
// ============================================

static async Task<bool> HorarioOcupado(
    MySqlConnection connection,
    int profissionalId,
    DateTime data,
    TimeSpan inicioNovo,
    TimeSpan fimNovo)
{

    using var cmd =
        new MySqlCommand(
            @"
            SELECT
                a.datahora,

                COALESCE(
                    SUM(s.duracaominutos),
                    0
                ) AS duracao

            FROM Agendamento a

            INNER JOIN AgendamentoServico ags
                ON ags.idAgendamento = a.id

            INNER JOIN Servico s
                ON s.id = ags.idServico

            WHERE
                a.idProfissional = @profissional

                AND DATE(a.datahora) = @data

                AND a.status_tipo <> 'Cancelado'

            GROUP BY a.id

            ",
            connection);


    cmd.Parameters.AddWithValue(
        "@profissional",
        profissionalId);

    cmd.Parameters.AddWithValue(
        "@data",
        data.Date);


    using var reader =
        await cmd.ExecuteReaderAsync();


    while (
        await reader.ReadAsync())
    {

        DateTime inicioExistente =
            reader.GetDateTime(0);


        int duracao =
            Convert.ToInt32(
                reader.GetDecimal(1));


        TimeSpan inicio =
            inicioExistente.TimeOfDay;


        TimeSpan fim =
            inicio.Add(
                TimeSpan.FromMinutes(
                    duracao));


        // =================================
        // VERIFICAÇÃO DE SOBREPOSIÇÃO
        // =================================

        bool conflito =
            inicioNovo < fim &&
            fimNovo > inicio;


        if (conflito)
        {

            return true;

        }

    }


    return false;

}


// ============================================
// DIAS DA SEMANA
// ============================================

static string TraduzirDiaSemana(
    DayOfWeek dia)
{

    return dia switch
    {

        DayOfWeek.Sunday =>
            "Domingo",

        DayOfWeek.Monday =>
            "Segunda",

        DayOfWeek.Tuesday =>
            "Terça",

        DayOfWeek.Wednesday =>
            "Quarta",

        DayOfWeek.Thursday =>
            "Quinta",

        DayOfWeek.Friday =>
            "Sexta",

        DayOfWeek.Saturday =>
            "Sábado",

        _ =>
            ""

    };

}