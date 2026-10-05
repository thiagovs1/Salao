using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Repositories;
using SalaoBeleza.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Conecta o Entity Framework ao MySQL usando a string de conexao.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Registra os repositorios (acesso ao banco).
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IServicoRepository, ServicoRepository>();
builder.Services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
builder.Services.AddScoped<IHorarioProfissionalRepository, HorarioProfissionalRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

// Registra os services (regras de negocio).
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<ServicoService>();
builder.Services.AddScoped<AgendamentoService>();
builder.Services.AddScoped<UsuarioService>();

// Habilita os controllers.
builder.Services.AddControllers();

// Habilita o Swagger (documentacao e teste da API).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Libera o acesso do frontend (CORS).
builder.Services.AddCors(options =>
{
    options.AddPolicy("Liberado", policy =>
        policy.WithOrigins("http://localhost")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

var app = builder.Build();

// Ativa a Session.
app.UseSession();

// Ativa o Swagger.
app.UseSwagger();
app.UseSwaggerUI();

// Ativa o CORS.
app.UseCors("Liberado");

// Permite servir os arquivos do wwwroot.
app.UseStaticFiles();

// Quando acessar localhost, abre o login.
app.MapGet("/", () =>
    Results.Redirect("/html/login_cliente.html")
);

// Ativa os Controllers da API.
app.MapControllers();

app.Run();