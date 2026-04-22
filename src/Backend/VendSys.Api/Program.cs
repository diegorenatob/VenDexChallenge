using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Serilog;
using VendSys.Api.Auth;
using VendSys.Api.Endpoints;
using VendSys.Api.Middleware;
using VendSys.Application.Interfaces;
using VendSys.Application.UseCases;
using VendSys.Infrastructure.Data;
using VendSys.Infrastructure.Parsing;
using VendSys.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/api-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14));

builder.Services.AddDbContext<VenDexDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDexParserService, DexParserService>();
builder.Services.AddScoped<IDexRepository, DexRepository>();
builder.Services.AddScoped<ProcessDexFileUseCase>();

builder.Services
    .AddAuthentication("Basic")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("Basic", null);

builder.Services.AddAuthorization();

builder.Services.Configure<BasicAuthOptions>(
    builder.Configuration.GetSection("BasicAuth"));

builder.Services.AddTransient<GlobalExceptionMiddleware>();

var app = builder.Build();

// Pipeline order: Serilog → GlobalException → Auth → Endpoint
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("Machine", httpContext.Request.Query["machine"].ToString());
        diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
        diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
    };
});

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapDexEndpoints();

app.Run();

// Exposes Program to WebApplicationFactory<Program> in the test project
public partial class Program { }
