using System.Text;
using GlicoNutri.Api.Data;
using GlicoNutri.Api.Repositories;
using GlicoNutri.Api.Security;
using GlicoNutri.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Persistência ────────────────────────────────────────────────────────────
// Por padrão usa o Postgres local do docker-compose. Com USAR_SUPABASE=true a
// aplicação aponta para a connection string do Supabase guardada no user-secrets
// — nunca no repositório.
var usarSupabase = builder.Configuration.GetValue<bool>("USAR_SUPABASE");
var nomeConexao = usarSupabase ? "Supabase" : "Default";

var conexao = builder.Configuration.GetConnectionString(nomeConexao)
    ?? throw new InvalidOperationException(
        $"Connection string '{nomeConexao}' não configurada. " +
        "Para o Supabase: dotnet user-secrets set \"ConnectionStrings:Supabase\" \"...\"");

builder.Services.AddDbContext<GlicoNutriDbContext>(opt => opt.UseNpgsql(conexao));

// ── Configuração ────────────────────────────────────────────────────────────
// RN01 — autenticação federada com Google. Sem ClientId o recurso fica desligado
// e a rota recusa, em vez de tentar validar contra uma audiência vazia.
builder.Services.AddOptions<GoogleOptions>()
    .Bind(builder.Configuration.GetSection(GoogleOptions.Secao));

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.Secao))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ── Serviços de domínio ─────────────────────────────────────────────────────
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ISenhaService, SenhaService>();
builder.Services.AddScoped<ITokenService, TokenService>();
// RF10.2 e RF10.3 — envio por SMTP quando a seção Email estiver configurada.
// Sem credenciais, cai no registrador de log, e o desenvolvimento segue sem
// depender de servidor de e-mail.
builder.Services.AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection(EmailOptions.Secao))
    .ValidateDataAnnotations();

var email = builder.Configuration.GetSection(EmailOptions.Secao).Get<EmailOptions>() ?? new EmailOptions();

if (email.EstaConfigurado)
    builder.Services.AddScoped<IEmailService, EmailServiceSmtp>();
else
    builder.Services.AddScoped<IEmailService, EmailServiceLog>();
builder.Services.AddScoped<INutricionistaService, NutricionistaService>();
builder.Services.AddScoped<IPacienteService, PacienteService>();
builder.Services.AddScoped<IAcessoPacienteService, AcessoPacienteService>();
builder.Services.AddScoped<IAntropometriaService, AntropometriaService>();
builder.Services.AddScoped<INecessidadeEnergeticaService, NecessidadeEnergeticaService>();
builder.Services.AddScoped<IAlimentoService, AlimentoService>();
builder.Services.AddScoped<IImportadorTacoService, ImportadorTacoService>();
builder.Services.AddScoped<IPlanoAlimentarService, PlanoAlimentarService>();
builder.Services.AddScoped<IGlicemiaService, GlicemiaService>();
builder.Services.AddScoped<IRegistroEmocionalService, RegistroEmocionalService>();
builder.Services.AddScoped<IAlertaService, AlertaService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IRepositorioEducativoService, RepositorioEducativoService>();
builder.Services.AddScoped<IRelatorioService, RelatorioService>();

// ── Autenticação: o JwtMiddleware do C4 ─────────────────────────────────────
var jwt = builder.Configuration.GetSection(JwtOptions.Secao).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Seção 'Jwt' ausente na configuração.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        // Sem isto o handler remapeia "sub" e "email" para URIs do WS-Federation e
        // as claims não são encontradas pelo nome com que foram emitidas.
        opt.MapInboundClaims = false;

        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };
    });

// ── Autorização por perfil: o RbacMiddleware do C4 (RN04) ───────────────────
// O Administrador recebe também a role de Nutricionista no token, então herda
// todas as permissões daquele perfil sem precisar de política própria.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Politicas.Paciente, p => p.RequireRole(Codigos.Perfil.Paciente))
    .AddPolicy(Politicas.Nutricionista, p => p.RequireRole(Codigos.Perfil.Nutricionista))
    .AddPolicy(Politicas.Administrador, p => p.RequireRole(Codigos.Perfil.Administrador));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// O front Vue roda em outra origem durante o desenvolvimento.
builder.Services.AddCors(opt => opt.AddPolicy("web", p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await GlicoNutri.Api.Data.SeedDesenvolvimento.AplicarAsync(app.Services);
}
else
{
    // RN31 — fora do desenvolvimento, todo tráfego é redirecionado para HTTPS.
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors("web");

app.UseAuthentication();
app.UseSenhaProvisoria();   // RN03, entre autenticar e autorizar
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>Exposto para os testes de integração.</summary>
public partial class Program;
