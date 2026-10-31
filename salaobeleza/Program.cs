
using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using SalaoBeleza.Data;
using SalaoBeleza.Repositories;
using SalaoBeleza.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "A conexão com o banco não foi configurada. Defina ConnectionStrings__DefaultConnection nas variáveis de ambiente da hospedagem."
    );
}

// CLOUDINARY
var cloudName = builder.Configuration["CLOUDINARY_CLOUD_NAME"];
var apiKey = builder.Configuration["CLOUDINARY_API_KEY"];
var apiSecret = builder.Configuration["CLOUDINARY_API_SECRET"];

if (string.IsNullOrWhiteSpace(cloudName) ||
    string.IsNullOrWhiteSpace(apiKey) ||
    string.IsNullOrWhiteSpace(apiSecret))
{
    throw new InvalidOperationException(
        "As credenciais do Cloudinary não foram configuradas."
    );
}

var cloudinaryAccount = new Account(cloudName, apiKey, apiSecret);
var cloudinary = new Cloudinary(cloudinaryAccount);
cloudinary.Api.Secure = true;

builder.Services.AddSingleton<Cloudinary>(cloudinary);

// SESSION
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.Name = ".Belleza.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// DATABASE
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(connectionString)
);

// REPOSITORIES
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IServicoRepository, ServicoRepository>();
builder.Services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
builder.Services.AddScoped<IHorarioProfissionalRepository, HorarioProfissionalRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();

// SERVICES
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<ServicoService>();
builder.Services.AddScoped<AgendamentoService>();
builder.Services.AddScoped<UsuarioService>();

// CONTROLLERS
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseRouting();
app.UseSession();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/html/login_cliente.html"));
app.MapControllers();

app.Run();
