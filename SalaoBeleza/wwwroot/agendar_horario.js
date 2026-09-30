const agendamentoSelecionado =
    JSON.parse(localStorage.getItem("agendamentoSelecionado"));

const profissionalSelecionado =
    JSON.parse(localStorage.getItem("profissionalSelecionado"));

let profissional = null;
let servicos = [];

if (agendamentoSelecionado) {
    profissional =
        agendamentoSelecionado.profissional ||
        profissionalSelecionado;

    servicos =
        agendamentoSelecionado.servicos || [];
} else {
    profissional = profissionalSelecionado;

    const servicoSelecionado =
        JSON.parse(localStorage.getItem("servicoSelecionado"));

    if (servicoSelecionado) {
        servicos = [servicoSelecionado];
    }
}

let dataSelecionada = null;
let horarioSelecionado = null;

const mesAtual = document.getElementById("mesAtual");
const diasCalendario = document.getElementById("diasCalendario");
const listaHorarios = document.getElementById("listaHorarios");

let dataCalendario = new Date();


function carregarDados() {

    if (!profissional) {

        listaHorarios.innerHTML = `
            <p class="mensagem-horario">
                Profissional não selecionado.
            </p>
        `;

        return;
    }

    document.getElementById("resumoProfissional").textContent =
        profissional.nome || "Não selecionado";


    if (servicos.length > 0) {

        const nomes = servicos
            .map(servico => servico.nome)
            .join(", ");

        document.getElementById("resumoServico").textContent =
            nomes;

        const total = servicos.reduce(
            (soma, servico) =>
                soma + Number(servico.preco || 0),
            0
        );

        document.getElementById("resumoPreco").textContent =
            "R$ " +
            total
                .toFixed(2)
                .replace(".", ",");

    } else {

        document.getElementById("resumoServico").textContent =
            "Não selecionado";

        document.getElementById("resumoPreco").textContent =
            "R$ 0,00";
    }
}


async function carregarCalendario() {

    const ano = dataCalendario.getFullYear();
    const mes = dataCalendario.getMonth() + 1;

    mesAtual.textContent =
        dataCalendario.toLocaleDateString(
            "pt-BR",
            {
                month: "long",
                year: "numeric"
            }
        );

    try {

        const resposta = await fetch(
            `/api/agendamento/calendario?profissionalId=${profissional.id}&ano=${ano}&mes=${mes}`
        );

        const texto = await resposta.text();

        console.log(
            "CALENDARIO:",
            resposta.status,
            texto
        );

        if (!resposta.ok) {
            throw new Error(texto);
        }

        const dias = JSON.parse(texto);

        montarDias(dias);

    } catch (erro) {

        console.error(
            "ERRO CALENDARIO:",
            erro
        );

        diasCalendario.innerHTML = `
            <p>Erro ao carregar calendário.</p>
        `;
    }
}


function montarDias(dias) {

    diasCalendario.innerHTML = "";

    const primeiroDia = new Date(
        dataCalendario.getFullYear(),
        dataCalendario.getMonth(),
        1
    );

    const ultimoDia = new Date(
        dataCalendario.getFullYear(),
        dataCalendario.getMonth() + 1,
        0
    );

    for (
        let i = 0;
        i < primeiroDia.getDay();
        i++
    ) {

        const vazio =
            document.createElement("div");

        diasCalendario.appendChild(vazio);
    }


    for (
        let dia = 1;
        dia <= ultimoDia.getDate();
        dia++
    ) {

        const info = dias.find(d => {

            const partes =
                d.data.split("-");

            return Number(partes[2]) === dia;
        });


        const botao =
            document.createElement("button");

        botao.type = "button";

        botao.textContent = dia;

        botao.classList.add("dia");


        if (info && info.disponivel) {

            botao.classList.add(
                "disponivel"
            );

            botao.addEventListener(
                "click",
                () => {
                    selecionarData(
                        info.data,
                        botao
                    );
                }
            );

        } else {

            botao.classList.add(
                "indisponivel"
            );

            botao.disabled = true;
        }


        diasCalendario.appendChild(botao);
    }
}


async function selecionarData(
    data,
    botao
) {

    document
        .querySelectorAll(".dia.selecionado")
        .forEach(d =>
            d.classList.remove("selecionado")
        );

    botao.classList.add(
        "selecionado"
    );

    dataSelecionada = data;

    horarioSelecionado = null;

    document.getElementById(
        "resumoDataHorario"
    ).textContent =
        formatarData(data);

    await carregarHorarios(data);
}


async function carregarHorarios(data) {

    listaHorarios.innerHTML = `
        <p class="mensagem-horario">
            Carregando horários...
        </p>
    `;


    if (!profissional) {

        listaHorarios.innerHTML = `
            <p class="mensagem-horario">
                Profissional não selecionado.
            </p>
        `;

        return;
    }


    const servicoIds =
        servicos
            .map(servico => Number(servico.id))
            .filter(id => id > 0);


    if (servicoIds.length === 0) {

        listaHorarios.innerHTML = `
            <p class="mensagem-horario">
                Nenhum serviço selecionado.
            </p>
        `;

        return;
    }


    try {

        const parametros =
            servicoIds
                .map(id =>
                    `servicoIds=${encodeURIComponent(id)}`
                )
                .join("&");


        const url =
            `/api/agendamento/horarios-multiplos?profissionalId=${encodeURIComponent(profissional.id)}&${parametros}&data=${encodeURIComponent(data)}`;


        console.log(
            "URL HORÁRIOS:",
            url
        );


        const resposta =
            await fetch(url);


        const texto =
            await resposta.text();


        console.log(
            "STATUS HORÁRIOS:",
            resposta.status
        );

        console.log(
            "RESPOSTA HORÁRIOS:",
            texto
        );


        if (!resposta.ok) {

            throw new Error(
                texto || "Erro na API."
            );
        }


        const horarios =
            JSON.parse(texto);


        listaHorarios.innerHTML = "";


        if (
            !horarios ||
            horarios.length === 0
        ) {

            listaHorarios.innerHTML = `
                <p class="mensagem-horario">
                    Não há horários disponíveis para esta data.
                </p>
            `;

            return;
        }


        horarios.forEach(horario => {

            const botao =
                document.createElement("button");

            botao.type = "button";

            botao.classList.add(
                "horario"
            );

            botao.textContent =
                `${horario.inicio} - ${horario.fim}`;


            botao.addEventListener(
                "click",
                () => {

                    document
                        .querySelectorAll(
                            ".horario.selecionado"
                        )
                        .forEach(h =>
                            h.classList.remove(
                                "selecionado"
                            )
                        );


                    botao.classList.add(
                        "selecionado"
                    );


                    horarioSelecionado =
                        horario;


                    document.getElementById(
                        "resumoDataHorario"
                    ).textContent =
                        `${formatarData(data)} - ${horario.inicio}`;
                }
            );


            listaHorarios.appendChild(
                botao
            );
        });


    } catch (erro) {

        console.error(
            "ERRO HORÁRIOS:",
            erro
        );


        listaHorarios.innerHTML = `
            <p class="mensagem-horario">
                Erro ao carregar horários.
            </p>
        `;
    }
}


function formatarData(data) {

    const partes =
        data.split("-");

    return `${partes[2]}/${partes[1]}/${partes[0]}`;
}


document
    .getElementById("mesAnterior")
    .addEventListener(
        "click",
        () => {

            dataCalendario.setMonth(
                dataCalendario.getMonth() - 1
            );

            carregarCalendario();
        }
    );


document
    .getElementById("mesProximo")
    .addEventListener(
        "click",
        () => {

            dataCalendario.setMonth(
                dataCalendario.getMonth() + 1
            );

            carregarCalendario();
        }
    );


document
    .getElementById("btnContinuar")
    .addEventListener(
        "click",
        () => {

            if (
                !dataSelecionada ||
                !horarioSelecionado
            ) {

                alert(
                    "Selecione uma data e um horário."
                );

                return;
            }


            const agendamento = {

                profissional:
                    profissional,

                servicos:
                    servicos,

                data:
                    dataSelecionada,

                horario:
                    horarioSelecionado
            };


            localStorage.setItem(
                "agendamentoSelecionado",
                JSON.stringify(
                    agendamento
                )
            );


            window.location.href =
                "confirmar_agendamento.html";
        }
    );


carregarDados();

carregarCalendario();