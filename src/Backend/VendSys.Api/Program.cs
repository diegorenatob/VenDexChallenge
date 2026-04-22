using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using VendSys.Api.Auth;
using VendSys.Api.Endpoints;
using VendSys.Api.Middleware;
using VendSys.Application.Interfaces;
using VendSys.Application.UseCases;
using VendSys.Infrastructure.Data;
using VendSys.Infrastructure.Parsing;
using VendSys.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

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

// Pipeline order: (Serilog — Feature 10) → GlobalException → Auth → Endpoint
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapDexEndpoints();

app.Run();
