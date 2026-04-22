using Microsoft.EntityFrameworkCore;
using VendSys.Api.Endpoints;
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

// Auth services — scheme will be replaced in Feature 8
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapDexEndpoints();

app.Run();
