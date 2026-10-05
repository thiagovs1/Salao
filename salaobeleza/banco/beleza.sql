
-- BANCO DE DADOS BELLEZA
-- =====================================================

CREATE DATABASE IF NOT EXISTS Belleza
CHARACTER SET utf8mb4
COLLATE utf8mb4_0900_ai_ci;

USE Belleza;


-- =====================================================
-- USUÁRIOS
-- =====================================================

CREATE TABLE IF NOT EXISTS Usuario (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    Senha VARCHAR(255) NOT NULL
);


-- =====================================================
-- CLIENTES
-- =====================================================

CREATE TABLE IF NOT EXISTS Clientes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Telefone VARCHAR(20) NOT NULL,
    Email VARCHAR(150) NOT NULL
);


-- =====================================================
-- SERVIÇOS
-- =====================================================

CREATE TABLE IF NOT EXISTS Servicos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Preco DECIMAL(10,2) NOT NULL,
    DuracaoMinutos INT NOT NULL
);


-- =====================================================
-- SERVIÇOS INICIAIS
-- =====================================================

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Corte', 40.00, 60
WHERE NOT EXISTS (
    SELECT 1 FROM Servicos WHERE Nome = 'Corte'
);

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Hidratação', 50.00, 60
WHERE NOT EXISTS (
    SELECT 1 FROM Servicos WHERE Nome = 'Hidratação'
);

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Manicure', 30.00, 45
WHERE NOT EXISTS (
    SELECT 1 FROM Servicos WHERE Nome = 'Manicure'
);

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Pedicure', 35.00, 45
WHERE NOT EXISTS (
    SELECT 1 FROM Servicos WHERE Nome = 'Pedicure'
);

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Penteado', 70.00, 90
WHERE NOT EXISTS (
    SELECT 1 FROM Servicos WHERE Nome = 'Penteado'
);

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Maquiagem', 90.00, 90
WHERE NOT EXISTS (
    SELECT 1 FROM Servicos WHERE Nome = 'Maquiagem'
);

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Design de Sobrancelha', 25.00, 30
WHERE NOT EXISTS (
    SELECT 1 FROM Servicos WHERE Nome = 'Design de Sobrancelha'
);


-- =====================================================
-- PROFISSIONAIS
-- =====================================================

CREATE TABLE IF NOT EXISTS Profissionais (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Profissao VARCHAR(100) NOT NULL,
    Foto VARCHAR(255) NOT NULL,
    Ativo BOOLEAN NOT NULL DEFAULT TRUE,
    Imagem VARCHAR(255) NOT NULL DEFAULT ''
);


-- =====================================================
-- PROFISSIONAIS INICIAIS
-- =====================================================

INSERT INTO Profissionais
    (Nome, Profissao, Foto, Ativo, Imagem)
SELECT
    'Camila Ferreira',
    'Cabeleireira',
    'camila.png',
    TRUE,
    ''
WHERE NOT EXISTS (
    SELECT 1
    FROM Profissionais
    WHERE Nome = 'Camila Ferreira'
);


INSERT INTO Profissionais
    (Nome, Profissao, Foto, Ativo, Imagem)
SELECT
    'Beatriz Costa',
    'Cabeleireira',
    'beatriz.png',
    TRUE,
    ''
WHERE NOT EXISTS (
    SELECT 1
    FROM Profissionais
    WHERE Nome = 'Beatriz Costa'
);


-- =====================================================
-- HORÁRIOS DOS PROFISSIONAIS
-- =====================================================

CREATE TABLE IF NOT EXISTS HorariosProfissionais (
    Id INT AUTO_INCREMENT PRIMARY KEY,

    ProfissionalId INT NOT NULL,

    DiaSemana INT NOT NULL,

    HoraInicio TIME NOT NULL,

    HoraFim TIME NOT NULL,

    CONSTRAINT FK_Horario_Profissional
        FOREIGN KEY (ProfissionalId)
        REFERENCES Profissionais(Id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);


-- =====================================================
-- HORÁRIOS INICIAIS
--
-- DiaSemana:
-- 0 = Domingo
-- 1 = Segunda
-- 2 = Terça
-- 3 = Quarta
-- 4 = Quinta
-- 5 = Sexta
-- 6 = Sábado
--
-- Horários:
-- 08:00 às 12:00
-- 13:00 às 18:00
-- =====================================================

INSERT INTO HorariosProfissionais
    (ProfissionalId, DiaSemana, HoraInicio, HoraFim)

SELECT
    p.Id,
    d.DiaSemana,
    h.HoraInicio,
    h.HoraFim

FROM Profissionais p

CROSS JOIN (
    SELECT 1 AS DiaSemana
    UNION ALL SELECT 2
    UNION ALL SELECT 3
    UNION ALL SELECT 4
    UNION ALL SELECT 5
) d

CROSS JOIN (
    SELECT
        '08:00:00' AS HoraInicio,
        '12:00:00' AS HoraFim

    UNION ALL

    SELECT
        '13:00:00',
        '18:00:00'
) h

WHERE p.Nome IN (
    'Camila Ferreira',
    'Beatriz Costa'
)

AND NOT EXISTS (
    SELECT 1
    FROM HorariosProfissionais hp

    WHERE hp.ProfissionalId = p.Id
      AND hp.DiaSemana = d.DiaSemana
      AND hp.HoraInicio = h.HoraInicio
      AND hp.HoraFim = h.HoraFim
);


-- =====================================================
-- COMBOS
-- =====================================================

CREATE TABLE IF NOT EXISTS Combo (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Preco DECIMAL(10,2) NOT NULL
);


-- =====================================================
-- COMBOS FIXOS
-- =====================================================

INSERT INTO Combo (Nome, Preco)
SELECT 'Dia de beleza', 130.00
WHERE NOT EXISTS (
    SELECT 1
    FROM Combo
    WHERE Nome = 'Dia de beleza'
);


INSERT INTO Combo (Nome, Preco)
SELECT 'Produção para festa', 160.00
WHERE NOT EXISTS (
    SELECT 1
    FROM Combo
    WHERE Nome = 'Produção para festa'
);


INSERT INTO Combo (Nome, Preco)
SELECT 'Belleza completa', 190.00
WHERE NOT EXISTS (
    SELECT 1
    FROM Combo
    WHERE Nome = 'Belleza completa'
);


-- =====================================================
-- SERVIÇOS DOS COMBOS
-- =====================================================

CREATE TABLE IF NOT EXISTS ComboServico (
    ComboId INT NOT NULL,
    ServicoId INT NOT NULL,

    PRIMARY KEY (ComboId, ServicoId),

    CONSTRAINT FK_ComboServico_Combo
        FOREIGN KEY (ComboId)
        REFERENCES Combo(Id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT FK_ComboServico_Servico
        FOREIGN KEY (ServicoId)
        REFERENCES Servicos(Id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);


-- =====================================================
-- DIA DE BELEZA
-- Corte + Hidratação + Manicure + Pedicure
-- =====================================================

INSERT INTO ComboServico (ComboId, ServicoId)

SELECT
    c.Id,
    s.Id

FROM Combo c

INNER JOIN Servicos s
    ON s.Nome IN (
        'Corte',
        'Hidratação',
        'Manicure',
        'Pedicure'
    )

WHERE c.Nome = 'Dia de beleza'

AND NOT EXISTS (
    SELECT 1
    FROM ComboServico cs
    WHERE cs.ComboId = c.Id
      AND cs.ServicoId = s.Id
);


-- =====================================================
-- PRODUÇÃO PARA FESTA
-- Penteado + Maquiagem + Design de Sobrancelha
-- =====================================================

INSERT INTO ComboServico (ComboId, ServicoId)

SELECT
    c.Id,
    s.Id

FROM Combo c

INNER JOIN Servicos s
    ON s.Nome IN (
        'Penteado',
        'Maquiagem',
        'Design de Sobrancelha'
    )

WHERE c.Nome = 'Produção para festa'

AND NOT EXISTS (
    SELECT 1
    FROM ComboServico cs
    WHERE cs.ComboId = c.Id
      AND cs.ServicoId = s.Id
);


-- =====================================================
-- BELLEZA COMPLETA
-- Corte + Hidratação + Manicure + Pedicure
-- + Maquiagem + Design de Sobrancelha
-- =====================================================

INSERT INTO ComboServico (ComboId, ServicoId)

SELECT
    c.Id,
    s.Id

FROM Combo c

INNER JOIN Servicos s
    ON s.Nome IN (
        'Corte',
        'Hidratação',
        'Manicure',
        'Pedicure',
        'Maquiagem',
        'Design de Sobrancelha'
    )

WHERE c.Nome = 'Belleza completa'

AND NOT EXISTS (
    SELECT 1
    FROM ComboServico cs
    WHERE cs.ComboId = c.Id
      AND cs.ServicoId = s.Id
);


-- =====================================================
-- AGENDAMENTOS
-- =====================================================

CREATE TABLE IF NOT EXISTS Agendamentos (
    Id INT AUTO_INCREMENT PRIMARY KEY,

    DataHora DATETIME NOT NULL,

    ClienteId INT NOT NULL,

    ServicoId INT NULL,

    ProfissionalId INT NOT NULL,

    Status VARCHAR(20) NOT NULL DEFAULT 'Aberto',

    ComboId INT NULL,

    CONSTRAINT FK_Agendamento_Cliente
        FOREIGN KEY (ClienteId)
        REFERENCES Clientes(Id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT FK_Agendamento_Servico
        FOREIGN KEY (ServicoId)
        REFERENCES Servicos(Id)
        ON DELETE CASCADE
        ON UPDATE CASCADE,

    CONSTRAINT FK_Agendamentos_Profissionais
        FOREIGN KEY (ProfissionalId)
        REFERENCES Profissionais(Id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE,

    CONSTRAINT FK_Agendamentos_Combo
        FOREIGN KEY (ComboId)
        REFERENCES Combo(Id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);


-- =====================================================
-- FIM DO BANCO BELLEZA
-- =====================================================

