# Sistema de Salao de Beleza — Backend em C#

API REST feita em .NET 8 com PostgreSQL e Docker.
Inclui um frontend simples (pagina HTML) que consome a API.

## O que o sistema faz

- Cadastra e lista clientes
- Cadastra e lista servicos (corte, manicure, etc.)
- Registra agendamentos ligando um cliente a um servico

## Estrutura das pastas

```
SalaoBeleza/
├── Models/           -> as entidades (viram tabelas no banco)
│   ├── Cliente.cs
│   ├── Servico.cs
│   └── Agendamento.cs
├── Data/
│   └── AppDbContext.cs   -> ponte entre C# e o banco
├── DTOs/             -> objetos de entrada e saida da API
├── Repositories/     -> unica camada que fala com o banco
├── Services/         -> regras de negocio
├── Controllers/      -> os endpoints da API
├── wwwroot/
│   └── index.html    -> o frontend
├── Program.cs        -> liga tudo (configuracao)
├── appsettings.json  -> string de conexao com o banco
└── docker-compose.yml -> sobe o PostgreSQL
```

## Como rodar (passo a passo)

### 1. Subir o banco de dados no Docker
```
docker compose up -d
```
Confira se subiu:
```
docker ps
```

### 2. Restaurar os pacotes do projeto
```
dotnet restore
```

### 3. Instalar a ferramenta de migrations (so na primeira vez)
```
dotnet tool install --global dotnet-ef
```

### 4. Criar as tabelas no banco
```
dotnet ef migrations add CriacaoInicial
dotnet ef database update
```

### 5. Rodar a aplicacao
```
dotnet run
```

### 6. Abrir no navegador
- Frontend:  http://localhost:5000
- Swagger (testar a API):  http://localhost:5000/swagger

(a porta pode variar; veja o endereco que aparece no terminal)

## Como o frontend se conecta ao backend

O frontend (wwwroot/index.html) usa `fetch` para chamar a API.
Como ele esta servido pelo proprio backend, usa caminho relativo:

```
fetch("/api/clientes")            -> lista os clientes (GET)
fetch("/api/clientes", { POST })  -> cadastra um cliente
```

O caminho de uma requisicao dentro do backend e sempre:

Controller -> Service -> Repository -> Banco de Dados

e a resposta volta pelo mesmo caminho ao contrario.
