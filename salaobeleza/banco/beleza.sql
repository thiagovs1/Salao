-- =========================================
-- CLIENTES
-- =========================================

CREATE TABLE IF NOT EXISTS Clientes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Telefone VARCHAR(20) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    CPF VARCHAR(14) NULL,
    Endereco VARCHAR(255) NULL,
    DataCadastro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY UQ_Clientes_Email (Email),
    UNIQUE KEY UQ_Clientes_Telefone (Telefone)
);

-- =========================================
-- SERVIÇOS
-- =========================================

CREATE TABLE IF NOT EXISTS Servicos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Preco DECIMAL(10,2) NOT NULL,
    DuracaoMinutos INT NOT NULL
);

-- =========================================
-- PROFISSIONAIS
-- =========================================

CREATE TABLE IF NOT EXISTS Profissionais (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Profissao VARCHAR(100) NOT NULL,
    Foto VARCHAR(255) NOT NULL DEFAULT '',
    Ativo BOOLEAN NOT NULL DEFAULT TRUE,
    Imagem VARCHAR(255) NOT NULL DEFAULT '',
    CPF VARCHAR(14) NULL,
    Telefone VARCHAR(20) NULL,
    Endereco VARCHAR(255) NULL,
    TaxaAtendimento DECIMAL(10,2) NOT NULL DEFAULT 0.00
);

-- =========================================
-- HORÁRIOS DAS PROFISSIONAIS
-- =========================================

CREATE TABLE IF NOT EXISTS HorariosProfissionais (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ProfissionalId INT NOT NULL,
    DiaSemana INT NOT NULL,
    HoraInicio TIME NOT NULL,
    HoraFim TIME NOT NULL,

    CONSTRAINT CK_Horarios_DiaSemana
        CHECK (DiaSemana BETWEEN 1 AND 7),

    CONSTRAINT CK_Horarios_Intervalo
        CHECK (HoraInicio < HoraFim),

    CONSTRAINT UQ_Horarios_Profissional_Dia_Hora
        UNIQUE (ProfissionalId, DiaSemana, HoraInicio, HoraFim),

    CONSTRAINT FK_HorariosProfissionais_Profissionais
        FOREIGN KEY (ProfissionalId)
        REFERENCES Profissionais(Id)
        ON DELETE CASCADE
);

-- =========================================
-- COMBOS
-- =========================================

CREATE TABLE IF NOT EXISTS `Combo` (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Preco DECIMAL(10,2) NOT NULL,
    Imagem VARCHAR(255) NULL
);

-- =========================================
-- SERVIÇOS DOS COMBOS
-- =========================================

CREATE TABLE IF NOT EXISTS ComboServico (
    ComboId INT NOT NULL,
    ServicoId INT NOT NULL,

    PRIMARY KEY (ComboId, ServicoId),

    CONSTRAINT FK_ComboServico_Combo
        FOREIGN KEY (ComboId)
        REFERENCES `Combo`(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_ComboServico_Servicos
        FOREIGN KEY (ServicoId)
        REFERENCES Servicos(Id)
        ON DELETE CASCADE
);

-- =========================================
-- USUÁRIOS
-- =========================================

CREATE TABLE IF NOT EXISTS Usuario (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL,
    Senha VARCHAR(255) NOT NULL,
    TipoUsuario VARCHAR(30) NOT NULL DEFAULT 'Cliente',
    ProfissionalId INT NULL,

    UNIQUE KEY UQ_Usuario_Email (Email),

    CONSTRAINT FK_Usuario_Profissionais
        FOREIGN KEY (ProfissionalId)
        REFERENCES Profissionais(Id)
        ON DELETE SET NULL
);

-- =========================================
-- AGENDAMENTOS
-- =========================================

CREATE TABLE IF NOT EXISTS Agendamentos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    DataHora DATETIME NOT NULL,
    ClienteId INT NOT NULL,
    ServicoId INT NULL,
    ProfissionalId INT NOT NULL,
    Status VARCHAR(30) NOT NULL DEFAULT 'Aberto',
    ComboId INT NULL,
    FormaPagamento VARCHAR(30) NULL,

    CONSTRAINT FK_Agendamentos_Clientes
        FOREIGN KEY (ClienteId)
        REFERENCES Clientes(Id),

    CONSTRAINT FK_Agendamentos_Servicos
        FOREIGN KEY (ServicoId)
        REFERENCES Servicos(Id)
        ON DELETE SET NULL,

    CONSTRAINT FK_Agendamentos_Profissionais
        FOREIGN KEY (ProfissionalId)
        REFERENCES Profissionais(Id),

    CONSTRAINT FK_Agendamentos_Combo
        FOREIGN KEY (ComboId)
        REFERENCES `Combo`(Id)
        ON DELETE SET NULL
);

-- =========================================
-- VÁRIOS SERVIÇOS POR AGENDAMENTO
-- =========================================

CREATE TABLE IF NOT EXISTS AgendamentoServicos (
    AgendamentoId INT NOT NULL,
    ServicoId INT NOT NULL,

    PRIMARY KEY (AgendamentoId, ServicoId),

    CONSTRAINT FK_AgendamentoServicos_Agendamentos
        FOREIGN KEY (AgendamentoId)
        REFERENCES Agendamentos(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_AgendamentoServicos_Servicos
        FOREIGN KEY (ServicoId)
        REFERENCES Servicos(Id)
        ON DELETE RESTRICT
);

-- =========================================
-- DADOS DOS SERVIÇOS
-- =========================================

INSERT INTO Servicos
    (Id, Nome, Preco, DuracaoMinutos)
VALUES
    (1, 'Corte', 40.00, 60),
    (2, 'Hidratação', 50.00, 60),
    (3, 'Manicure', 30.00, 45),
    (4, 'Pedicure', 35.00, 45),
    (5, 'Penteado', 70.00, 90),
    (6, 'Maquiagem', 90.00, 90),
    (7, 'Design de Sobrancelha', 25.00, 30)
ON DUPLICATE KEY UPDATE
    Nome = VALUES(Nome),
    Preco = VALUES(Preco),
    DuracaoMinutos = VALUES(DuracaoMinutos);

-- =========================================
-- DADOS DAS PROFISSIONAIS
-- =========================================

INSERT INTO Profissionais
    (Id, Nome, Profissao, Foto, Ativo, Imagem)
VALUES
    (1, 'Camila Ferreira', 'Cabeleireira',
     'camila.png', TRUE, 'camila.png'),
    (2, 'Beatriz Costa', 'Cabeleireira',
     'beatriz.png', TRUE, 'beatriz.png')
ON DUPLICATE KEY UPDATE
    Nome = VALUES(Nome),
    Profissao = VALUES(Profissao),
    Foto = VALUES(Foto),
    Ativo = VALUES(Ativo),
    Imagem = VALUES(Imagem);

-- =========================================
-- DADOS DOS COMBOS
-- =========================================

INSERT INTO `Combo` (Id, Nome, Preco)
VALUES
    (1, 'Dia de beleza', 130.00),
    (2, 'Produção para festa', 160.00),
    (3, 'Belleza completa', 190.00)
ON DUPLICATE KEY UPDATE
    Nome = VALUES(Nome),
    Preco = VALUES(Preco);

-- =========================================
-- SERVIÇOS DOS COMBOS
-- =========================================

INSERT INTO ComboServico (ComboId, ServicoId)
VALUES
    (1, 1), (1, 2), (1, 3), (1, 4),
    (2, 5), (2, 6), (2, 7),
    (3, 1), (3, 2), (3, 3), (3, 4), (3, 6), (3, 7)
ON DUPLICATE KEY UPDATE
    ServicoId = VALUES(ServicoId);

-- =========================================
-- HORÁRIOS: SEGUNDA A SÁBADO
-- =========================================

INSERT INTO HorariosProfissionais
    (ProfissionalId, DiaSemana, HoraInicio, HoraFim)
SELECT
    p.Id,
    d.DiaSemana,
    d.HoraInicio,
    d.HoraFim
FROM Profissionais p
CROSS JOIN (
    SELECT 1 AS DiaSemana, '08:00:00' AS HoraInicio, '12:00:00' AS HoraFim
    UNION ALL SELECT 1, '13:00:00', '18:00:00'
    UNION ALL SELECT 2, '08:00:00', '12:00:00'
    UNION ALL SELECT 2, '13:00:00', '18:00:00'
    UNION ALL SELECT 3, '08:00:00', '12:00:00'
    UNION ALL SELECT 3, '13:00:00', '18:00:00'
    UNION ALL SELECT 4, '08:00:00', '12:00:00'
    UNION ALL SELECT 4, '13:00:00', '18:00:00'
    UNION ALL SELECT 5, '08:00:00', '12:00:00'
    UNION ALL SELECT 5, '13:00:00', '18:00:00'
    UNION ALL SELECT 6, '08:00:00', '12:00:00'
    UNION ALL SELECT 6, '13:00:00', '18:00:00'
) d
WHERE NOT EXISTS (
    SELECT 1
    FROM HorariosProfissionais h
    WHERE h.ProfissionalId = p.Id
      AND h.DiaSemana = d.DiaSemana
      AND h.HoraInicio = d.HoraInicio
      AND h.HoraFim = d.HoraFim
);

-- =========================================
-- CONTAS DAS FUNCIONÁRIAS
-- =========================================

INSERT INTO Usuario
    (Nome, Email, Senha, TipoUsuario, ProfissionalId)
VALUES
    ('Camila Ferreira', 'camila@belleza.com',
     '123456', 'Funcionario', 1),
    ('Beatriz Costa', 'beatriz@belleza.com',
     '123456', 'Funcionario', 2)
ON DUPLICATE KEY UPDATE
    Nome = VALUES(Nome),
    Senha = VALUES(Senha),
    TipoUsuario = VALUES(TipoUsuario),
    ProfissionalId = VALUES(ProfissionalId);

-- =========================================
-- AVALIAÇÕES
-- =========================================

CREATE TABLE IF NOT EXISTS Avaliacoes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ClienteId INT NOT NULL,
    Nota INT NOT NULL,
    Comentario VARCHAR(1000) NOT NULL,
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT CK_Avaliacoes_Nota
        CHECK (Nota BETWEEN 1 AND 5),

    CONSTRAINT FK_Avaliacoes_Clientes
        FOREIGN KEY (ClienteId)
        REFERENCES Clientes(Id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS AvaliacaoCurtidas (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    AvaliacaoId INT NOT NULL,
    ClienteId INT NOT NULL,
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT UQ_AvaliacaoCurtidas_Cliente
        UNIQUE (AvaliacaoId, ClienteId),

    CONSTRAINT FK_AvaliacaoCurtidas_Avaliacoes
        FOREIGN KEY (AvaliacaoId)
        REFERENCES Avaliacoes(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_AvaliacaoCurtidas_Clientes
        FOREIGN KEY (ClienteId)
        REFERENCES Clientes(Id)
        ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS AvaliacaoRespostas (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    AvaliacaoId INT NOT NULL,
    ClienteId INT NOT NULL,
    Resposta VARCHAR(1000) NOT NULL,
    DataCriacao DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_AvaliacaoRespostas_Avaliacoes
        FOREIGN KEY (AvaliacaoId)
        REFERENCES Avaliacoes(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_AvaliacaoRespostas_Clientes
        FOREIGN KEY (ClienteId)
        REFERENCES Clientes(Id)
        ON DELETE CASCADE
);

-- =========================================
-- SERVIÇOS E PROFISSIONAIS
-- =========================================

CREATE TABLE IF NOT EXISTS ServicoProfissional (
    ServicoId INT NOT NULL,
    ProfissionalId INT NOT NULL,
    NomeServico VARCHAR(100) NULL,
    NomeProfissional VARCHAR(100) NULL,

    PRIMARY KEY (ServicoId, ProfissionalId),

    CONSTRAINT FK_ServicoProfissional_Servicos
        FOREIGN KEY (ServicoId)
        REFERENCES Servicos(Id)
        ON DELETE CASCADE,

    CONSTRAINT FK_ServicoProfissional_Profissionais
        FOREIGN KEY (ProfissionalId)
        REFERENCES Profissionais(Id)
        ON DELETE CASCADE
);

-- =========================================
-- VINCULAR SERVIÇOS ÀS PROFISSIONAIS
-- =========================================

INSERT IGNORE INTO ServicoProfissional
    (ServicoId, ProfissionalId, NomeServico, NomeProfissional)
SELECT
    s.Id,
    p.Id,
    s.Nome,
    p.Nome
FROM Servicos s
CROSS JOIN Profissionais p
WHERE p.Nome IN ('Camila Ferreira', 'Beatriz Costa');

-- =========================================
-- PREENCHER OS NOMES DOS VÍNCULOS EXISTENTES
-- =========================================

UPDATE ServicoProfissional sp
INNER JOIN Servicos s
    ON s.Id = sp.ServicoId
INNER JOIN Profissionais p
    ON p.Id = sp.ProfissionalId
SET
    sp.NomeServico = s.Nome,
    sp.NomeProfissional = p.Nome;

-- =========================================
-- TRIGGERS
-- =========================================

DROP TRIGGER IF EXISTS trg_servico_atualizar_nome;
DROP TRIGGER IF EXISTS trg_profissional_atualizar_nome;
DROP TRIGGER IF EXISTS trg_vinculo_preencher_nomes;

DELIMITER $$

CREATE TRIGGER trg_servico_atualizar_nome
AFTER UPDATE ON Servicos
FOR EACH ROW
BEGIN
    UPDATE ServicoProfissional
    SET NomeServico = NEW.Nome
    WHERE ServicoId = NEW.Id;
END$$

CREATE TRIGGER trg_profissional_atualizar_nome
AFTER UPDATE ON Profissionais
FOR EACH ROW
BEGIN
    UPDATE ServicoProfissional
    SET NomeProfissional = NEW.Nome
    WHERE ProfissionalId = NEW.Id;
END$$

CREATE TRIGGER trg_vinculo_preencher_nomes
BEFORE INSERT ON ServicoProfissional
FOR EACH ROW
BEGIN
    SET NEW.NomeServico = (
        SELECT Nome
        FROM Servicos
        WHERE Id = NEW.ServicoId
    );

    SET NEW.NomeProfissional = (
        SELECT Nome
        FROM Profissionais
        WHERE Id = NEW.ProfissionalId
    );
END$$

DELIMITER ;

-- =========================================
-- GARANTIR COLUNAS EM TABELAS EXISTENTES
-- =========================================

ALTER TABLE Profissionais
ADD COLUMN IF NOT EXISTS CPF VARCHAR(14) NULL;

ALTER TABLE Profissionais
ADD COLUMN IF NOT EXISTS Telefone VARCHAR(20) NULL;

ALTER TABLE Profissionais
ADD COLUMN IF NOT EXISTS Endereco VARCHAR(255) NULL;

ALTER TABLE Profissionais
ADD COLUMN IF NOT EXISTS TaxaAtendimento DECIMAL(10,2) NOT NULL DEFAULT 0.00;

ALTER TABLE Agendamentos
ADD COLUMN IF NOT EXISTS FormaPagamento VARCHAR(30) NULL;

ALTER TABLE `Combo`
ADD COLUMN IF NOT EXISTS Imagem VARCHAR(255) NULL;

-- =========================================
-- DESPESAS
-- =========================================

CREATE TABLE IF NOT EXISTS Despesas (
    Id INT NOT NULL AUTO_INCREMENT,
    Descricao VARCHAR(200) NOT NULL,
    Categoria VARCHAR(100) NOT NULL DEFAULT 'Outros',
    Valor DECIMAL(10,2) NOT NULL,
    Data DATETIME NOT NULL,
    PRIMARY KEY (Id)
);

-- =========================================
-- CONFERÊNCIA FINAL
-- =========================================

SHOW TABLES;

SHOW COLUMNS FROM Profissionais;

SHOW COLUMNS FROM Agendamentos;

SHOW COLUMNS FROM `Combo`;

SELECT
    ServicoId,
    NomeServico,
    ProfissionalId,
    NomeProfissional
FROM ServicoProfissional
ORDER BY NomeServico, NomeProfissional;