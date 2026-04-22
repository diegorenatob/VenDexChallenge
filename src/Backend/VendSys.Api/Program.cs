using Microsoft.EntityFrameworkCore;
using VendSys.Application.Interfaces;
using VendSys.Infrastructure.Data;
using VendSys.Infrastructure.Parsing;
using VendSys.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<VenDexDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IDexParserService, DexParserService>();
builder.Services.AddScoped<IDexRepository, DexRepository>();

var app = builder.Build();

app.Run();
