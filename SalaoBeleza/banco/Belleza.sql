CREATE DATABASE Belleza;

USE Belleza;


-- =====================================================
-- CLIENTE
-- =====================================================

CREATE TABLE Cliente (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(30) NOT NULL,
    email VARCHAR(30) NOT NULL UNIQUE,
    telefone VARCHAR(20) NOT NULL UNIQUE,
    senha VARCHAR(10) NOT NULL,
    cpf VARCHAR(14) NULL UNIQUE
);


-- =====================================================
-- USUARIO
-- =====================================================

CREATE TABLE Usuario (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(30) NOT NULL,
    email VARCHAR(30) NOT NULL UNIQUE,
    senha VARCHAR(10) NOT NULL,
    tipo_usuario VARCHAR(20) NOT NULL DEFAULT 'Cliente'
);


-- =====================================================
-- SERVICO
-- =====================================================

CREATE TABLE Servico (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(30) NOT NULL,
    categoria VARCHAR(30) NOT NULL,
    preco DECIMAL(7,2) NOT NULL,
    duracaominutos INT NOT NULL
);


-- =====================================================
-- PROFISSIONAL
-- =====================================================

CREATE TABLE Profissional (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    especialidade VARCHAR(100) NOT NULL,
    avaliacao DECIMAL(2,1) DEFAULT 0.0
);


-- =====================================================
-- HORARIO DO PROFISSIONAL
-- =====================================================

CREATE TABLE HorarioProfissional (
    id INT AUTO_INCREMENT PRIMARY KEY,
    idProfissional INT NOT NULL,
    dia_semana VARCHAR(20) NOT NULL,
    hora_inicio TIME NOT NULL,
    hora_fim TIME NOT NULL,

    CONSTRAINT FK_Horario_Profissional
        FOREIGN KEY (idProfissional)
        REFERENCES Profissional(id)
);


-- =====================================================
-- AGENDAMENTO
-- =====================================================

CREATE TABLE Agendamento (
    id INT AUTO_INCREMENT PRIMARY KEY,
    idCliente INT NOT NULL,
    idProfissional INT NOT NULL,
    datahora TIMESTAMP NOT NULL,
    status_tipo VARCHAR(20) DEFAULT 'Pendente',

    CONSTRAINT FK_Agendamentos_Clientes
        FOREIGN KEY (idCliente)
        REFERENCES Cliente(id),

    CONSTRAINT FK_Agendamentos_Profissional
        FOREIGN KEY (idProfissional)
        REFERENCES Profissional(id)
);


-- =====================================================
-- SERVICOS DO AGENDAMENTO
-- =====================================================

CREATE TABLE AgendamentoServico (
    idAgendamento INT NOT NULL,
    idServico INT NOT NULL,

    PRIMARY KEY (idAgendamento, idServico),

    CONSTRAINT FK_AgendamentoServico_Agendamento
        FOREIGN KEY (idAgendamento)
        REFERENCES Agendamento(id),

    CONSTRAINT FK_AgendamentoServico_Servico
        FOREIGN KEY (idServico)
        REFERENCES Servico(id)
);


-- =====================================================
-- COMBO
-- =====================================================

CREATE TABLE Combo (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    preco DECIMAL(7,2) NOT NULL
);


-- =====================================================
-- SERVICOS DO COMBO
-- =====================================================

CREATE TABLE ComboServico (
    idCombo INT NOT NULL,
    idServico INT NOT NULL,

    PRIMARY KEY (idCombo, idServico),

    CONSTRAINT FK_ComboServico_Combo
        FOREIGN KEY (idCombo)
        REFERENCES Combo(id),

    CONSTRAINT FK_ComboServico_Servico
        FOREIGN KEY (idServico)
        REFERENCES Servico(id)
);


-- =====================================================
-- PROMOCAO
-- =====================================================

CREATE TABLE Promocao (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    descricao VARCHAR(255),
    percentual_desconto DECIMAL(5,2),
    data_inicio DATE,
    data_fim DATE,
    ativo BOOLEAN DEFAULT TRUE
);


-- =====================================================
-- AVALIACAO
-- =====================================================

CREATE TABLE Avaliacao (
    id INT AUTO_INCREMENT PRIMARY KEY,
    idCliente INT NOT NULL,
    idProfissional INT NOT NULL,
    nota DECIMAL(2,1) NOT NULL,
    comentario VARCHAR(500),
    data_avaliacao DATE,

    CONSTRAINT FK_Avaliacao_Cliente
        FOREIGN KEY (idCliente)
        REFERENCES Cliente(id),

    CONSTRAINT FK_Avaliacao_Profissional
        FOREIGN KEY (idProfissional)
        REFERENCES Profissional(id)
);


-- =====================================================
-- PEDIDO
-- =====================================================
-- idCombo pode ser NULL.
-- Portanto, o cliente NÃO é obrigado a escolher um combo.

CREATE TABLE Pedido (
    id INT AUTO_INCREMENT PRIMARY KEY,
    idCliente INT NOT NULL,
    idCombo INT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Em andamento',
    data_criacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_Pedido_Cliente
        FOREIGN KEY (idCliente)
        REFERENCES Cliente(id),

    CONSTRAINT FK_Pedido_Combo
        FOREIGN KEY (idCombo)
        REFERENCES Combo(id)
);


-- =====================================================
-- SERVICOS DO PEDIDO
-- =====================================================
-- Permite que o cliente tenha serviços individuais,
-- mesmo sem possuir um combo.

CREATE TABLE PedidoServico (
    idPedido INT NOT NULL,
    idServico INT NOT NULL,

    PRIMARY KEY (idPedido, idServico),

    CONSTRAINT FK_PedidoServico_Pedido
        FOREIGN KEY (idPedido)
        REFERENCES Pedido(id),

    CONSTRAINT FK_PedidoServico_Servico
        FOREIGN KEY (idServico)
        REFERENCES Servico(id)
);


-- =====================================================
-- CADASTRO DOS SERVICOS
-- =====================================================

INSERT INTO Servico
(nome, categoria, preco, duracaominutos)
VALUES
('Corte', 'Cabelo', 60.00, 60),

('Hidratação', 'Cabelo', 45.00, 45),

('Manicure', 'Manicure', 35.00, 30),

('Pedicure', 'Pedicure', 35.00, 30),

('Penteado', 'Cabelo', 80.00, 60),

('Maquiagem', 'Maquiagem', 80.00, 60),

('Design de Sobrancelha', 'Sobrancelha', 30.00, 35);


-- =====================================================
-- CADASTRO DOS COMBOS
-- =====================================================

INSERT INTO Combo
(nome, preco)
VALUES
('Dia de beleza', 200.00),

('Produção para festa', 190.00),

('Belleza completa', 280.00);


-- =====================================================
-- SERVICOS DO COMBO
-- =====================================================

-- DIA DE BELEZA
-- Corte
-- Hidratação
-- Manicure
-- Pedicure

INSERT INTO ComboServico
(idCombo, idServico)
SELECT
    c.id,
    s.id
FROM Combo c
INNER JOIN Servico s
    ON s.nome IN (
        'Corte',
        'Hidratação',
        'Manicure',
        'Pedicure'
    )
WHERE c.nome = 'Dia de beleza';


-- PRODUÇÃO PARA FESTA
-- Penteado
-- Maquiagem
-- Design de Sobrancelha

INSERT INTO ComboServico
(idCombo, idServico)
SELECT
    c.id,
    s.id
FROM Combo c
INNER JOIN Servico s
    ON s.nome IN (
        'Penteado',
        'Maquiagem',
        'Design de Sobrancelha'
    )
WHERE c.nome = 'Produção para festa';


-- BELLEZA COMPLETA
-- Corte
-- Hidratação
-- Manicure
-- Pedicure
-- Maquiagem
-- Design de Sobrancelha

INSERT INTO ComboServico
(idCombo, idServico)
SELECT
    c.id,
    s.id
FROM Combo c
INNER JOIN Servico s
    ON s.nome IN (
        'Corte',
        'Hidratação',
        'Manicure',
        'Pedicure',
        'Maquiagem',
        'Design de Sobrancelha'
    )
WHERE c.nome = 'Belleza completa';


-- =====================================================
-- PROFISSIONAIS
-- =====================================================

INSERT INTO Profissional
(nome, especialidade, avaliacao)
VALUES
('Camila Ferreira', 'Cabeleireira', 4.9),

('Betriz Costa', 'Cabeleireira', 4.8);


INSERT INTO Servico (nome, categoria, preco, duracaominutos)
SELECT 'Corte + Escove', 'Cabelo', 65.00, 60
WHERE NOT EXISTS (
    SELECT 1 FROM Servico
    WHERE nome = 'Corte + Escove'
);

INSERT INTO Servico (nome, categoria, preco, duracaominutos)
SELECT 'Corte + Hidratação', 'Cabelo', 90.00, 90
WHERE NOT EXISTS (
    SELECT 1 FROM Servico
    WHERE nome = 'Corte + Hidratação'
);

INSERT INTO Servico (nome, categoria, preco, duracaominutos)
SELECT 'Mecha + Tratamento', 'Cabelo', 220.00, 210
WHERE NOT EXISTS (
    SELECT 1 FROM Servico
    WHERE nome = 'Mecha + Tratamento'
);

INSERT INTO Servico (nome, categoria, preco, duracaominutos)
SELECT 'Escova + Hidratação', 'Cabelo', 75.00, 75
WHERE NOT EXISTS (
    SELECT 1 FROM Servico
    WHERE nome = 'Escova + Hidratação'
);

INSERT INTO Servico (nome, categoria, preco, duracaominutos)
SELECT 'Coloração + Reconstrução', 'Cabelo', 180.00, 150
WHERE NOT EXISTS (
    SELECT 1 FROM Servico
    WHERE nome = 'Coloração + Reconstrução'
);

INSERT INTO Servico (nome, categoria, preco, duracaominutos)
SELECT 'Progressiva + Hidratação', 'Cabelo', 200.00, 180
WHERE NOT EXISTS (
    SELECT 1 FROM Servico
    WHERE nome = 'Progressiva + Hidratação'
);

-- =====================================================
-- HORÁRIOS DOS PROFISSIONAIS
-- =====================================================

INSERT INTO HorarioProfissional
(idProfissional, dia_semana, hora_inicio, hora_fim)
VALUES

-- CAMILA FERREIRA
(1, 'Segunda', '08:00:00', '18:00:00'),
(1, 'Terça',   '08:00:00', '18:00:00'),
(1, 'Quarta',  '08:00:00', '18:00:00'),
(1, 'Quinta',  '08:00:00', '18:00:00'),
(1, 'Sexta',   '08:00:00', '18:00:00'),
(1, 'Sábado',  '08:00:00', '18:00:00'),

-- BETRIZ COSTA
(2, 'Segunda', '08:00:00', '18:00:00'),
(2, 'Terça',   '08:00:00', '18:00:00'),
(2, 'Quarta',  '08:00:00', '18:00:00'),
(2, 'Quinta',  '08:00:00', '18:00:00'),
(2, 'Sexta',   '08:00:00', '18:00:00'),
(2, 'Sábado',  '08:00:00', '18:00:00');

INSERT INTO HorarioProfissional
(idProfissional, dia_semana, hora_inicio, hora_fim)
VALUES
(1, 'Segunda', '08:00:00', '11:00:00'),
(1, 'Segunda', '13:00:00', '18:00:00'),
(1, 'Terça', '08:00:00', '11:00:00'),
(1, 'Terça', '13:00:00', '18:00:00'),
(1, 'Quarta', '08:00:00', '11:00:00'),
(1, 'Quarta', '13:00:00', '18:00:00'),
(1, 'Quinta', '08:00:00', '11:00:00'),
(1, 'Quinta', '13:00:00', '18:00:00'),
(1, 'Sexta', '08:00:00', '11:00:00'),
(1, 'Sexta', '13:00:00', '18:00:00'),
(1, 'Sábado', '08:00:00', '11:00:00'),
(1, 'Sábado', '13:00:00', '18:00:00'),

(2, 'Segunda', '08:00:00', '11:00:00'),
(2, 'Segunda', '13:00:00', '18:00:00'),
(2, 'Terça', '08:00:00', '11:00:00'),
(2, 'Terça', '13:00:00', '18:00:00'),
(2, 'Quarta', '08:00:00', '11:00:00'),
(2, 'Quarta', '13:00:00', '18:00:00'),
(2, 'Quinta', '08:00:00', '11:00:00'),
(2, 'Quinta', '13:00:00', '18:00:00'),
(2, 'Sexta', '08:00:00', '11:00:00'),
(2, 'Sexta', '13:00:00', '18:00:00'),
(2, 'Sábado', '08:00:00', '11:00:00'),
(2, 'Sábado', '13:00:00', '18:00:00');