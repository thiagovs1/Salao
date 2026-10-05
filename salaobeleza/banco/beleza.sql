USE Belleza;

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
    SELECT 1
    FROM Servicos
    WHERE Nome = 'Corte'
);

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Hidratação', 50.00, 60
WHERE NOT EXISTS (
    SELECT 1
    FROM Servicos
    WHERE Nome = 'Hidratação'
);

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Manicure', 30.00, 45
WHERE NOT EXISTS (
    SELECT 1
    FROM Servicos
    WHERE Nome = 'Manicure'
);

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Pedicure', 35.00, 45
WHERE NOT EXISTS (
    SELECT 1
    FROM Servicos
    WHERE Nome = 'Pedicure'
);

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Penteado', 70.00, 90
WHERE NOT EXISTS (
    SELECT 1
    FROM Servicos
    WHERE Nome = 'Penteado'
);

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Maquiagem', 90.00, 90
WHERE NOT EXISTS (
    SELECT 1
    FROM Servicos
    WHERE Nome = 'Maquiagem'
);

INSERT INTO Servicos (Nome, Preco, DuracaoMinutos)
SELECT 'Design de Sobrancelha', 25.00, 30
WHERE NOT EXISTS (
    SELECT 1
    FROM Servicos
    WHERE Nome = 'Design de Sobrancelha'
);


-- =====================================================
-- PROFISSIONAIS
-- =====================================================

CREATE TABLE IF NOT EXISTS Profissionais (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Profissao VARCHAR(100) NOT NULL,
    Foto VARCHAR(255) NOT NULL,
    Ativo BOOLEAN NOT NULL DEFAULT TRUE
);


-- =====================================================
-- PROFISSIONAIS INICIAIS
-- =====================================================

INSERT INTO Profissionais
    (Nome, Profissao, Foto, Ativo)
SELECT
    'Camila Ferreira',
    'Cabeleireira',
    'camila.png',
    TRUE
WHERE NOT EXISTS (
    SELECT 1
    FROM Profissionais
    WHERE Nome = 'Camila Ferreira'
);


INSERT INTO Profissionais
    (Nome, Profissao, Foto, Ativo)
SELECT
    'Beatriz Costa',
    'Cabeleireira',
    'beatriz.png',
    TRUE
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
        '11:00:00' AS HoraFim

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
-- AGENDAMENTOS
-- =====================================================

CREATE TABLE IF NOT EXISTS Agendamentos (
    Id INT AUTO_INCREMENT PRIMARY KEY,

    DataHora DATETIME NOT NULL,

    ClienteId INT NOT NULL,

    ServicoId INT NOT NULL,

    ProfissionalId INT NOT NULL,

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

    CONSTRAINT FK_Agendamento_Profissional
        FOREIGN KEY (ProfissionalId)
        REFERENCES Profissionais(Id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
);

ALTER TABLE Agendamentos
ADD COLUMN Status VARCHAR(20) NOT NULL DEFAULT 'Aberto';

USE Belleza;

ALTER TABLE Agendamentos
MODIFY COLUMN ServicoId INT NULL;

ALTER TABLE Agendamentos
ADD COLUMN ComboId INT NULL;

ALTER TABLE Agendamentos
ADD CONSTRAINT FK_Agendamentos_Combo
FOREIGN KEY (ComboId)
REFERENCES Combo(Id)
ON DELETE CASCADE
ON UPDATE CASCADE;


UPDATE HorariosProfissionais
SET HoraFim = '12:00:00'
WHERE HoraInicio = '08:00:00'
  AND HoraFim = '11:00:00';

-- =====================================================
-- COMBOS
-- =====================================================

CREATE TABLE IF NOT EXISTS Combo (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Preco DECIMAL(10,2) NOT NULL
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