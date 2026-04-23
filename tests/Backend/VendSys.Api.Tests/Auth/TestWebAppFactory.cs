using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using VendSys.Application.DTOs;
using VendSys.Application.Interfaces;
using VendSys.Infrastructure.Data;

namespace VendSys.Api.Tests.Auth;

internal sealed class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BasicAuth:Username"] = "testuser",
                ["BasicAuth:Password"] = "testpass",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace SQL Server DbContext with InMemory so no real DB is needed
            var dbDesc = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<VenDexDbContext>));
            if (dbDesc is not null) services.Remove(dbDesc);
            services.AddDbContext<VenDexDbContext>(o =>
                o.UseInMemoryDatabase("auth-tests"));

            // Replace IDexRepository with a substitute that returns fixed values
            var repoDesc = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IDexRepository));
            if (repoDesc is not null) services.Remove(repoDesc);

            var repoSub = Substitute.For<IDexRepository>();
            repoSub.SaveDexMeterAsync(Arg.Any<DexMeterDto>()).Returns(Task.FromResult(1));
            repoSub.SaveDexLaneMeterAsync(Arg.Any<int>(), Arg.Any<DexLaneMeterDto>()).Returns(Task.CompletedTask);
            services.AddScoped<IDexRepository>(_ => repoSub);
        });
    }
}
