using System.Text.Json.Serialization;
using Clinica.Api.Configuracao;
using Clinica.Api.Seguranca;
using Clinica.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serviços ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAplicacao();                                 // casos de uso (Application)
builder.Services.AddInfrastructure(builder.Configuration);       // banco, fotos, relógio (Infrastructure)
builder.Services.AddAutenticacaoJwt(builder.Configuration, builder.Environment);   // login com token JWT
builder.Services.AddLimitesDeRequisicao();
builder.Services.AddExceptionHandler<TratadorDeErros>();
builder.Services.AddProblemDetails();

// Em produção a aplicação fica atrás de um proxy (Caddy) que cuida do HTTPS.
// Estes cabeçalhos dizem à API o IP real do visitante e que a conexão original era HTTPS.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

await app.Services.InicializarInfraestruturaAsync(app.Configuration);

// ---------- Pipeline HTTP (a ordem importa) ----------
app.UseForwardedHeaders();
app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// O site em Vue é compilado para wwwroot e servido por esta mesma aplicação.
app.UseDefaultFiles();
app.UseStaticFiles();

// Fotos enviadas pelo painel.
var pastaDeFotos = DependencyInjection.PastaDeFotos(app.Configuration);
Directory.CreateDirectory(pastaDeFotos);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(pastaDeFotos),
    RequestPath = "/fotos"
});

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallback("/api/{**rota}", () => Results.NotFound());   // rota de API inexistente = 404
app.MapFallbackToFile("index.html");                          // qualquer outra rota = site em Vue

app.Run();
