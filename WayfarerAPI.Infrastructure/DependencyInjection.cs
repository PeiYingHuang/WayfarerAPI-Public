using Autofac;
using Microsoft.Extensions.Configuration;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Utilities;
using WayfarerAPI.Application.Models.Common;
using WayfarerAPI.Infrastructure.Data;
using WayfarerAPI.Infrastructure.Utilities;

namespace WayfarerAPI.Infrastructure;

public class InfrastructureModule : Module
{
    private readonly IConfiguration _configuration;

    public InfrastructureModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        var jwtSettings = _configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                          ?? throw new InvalidOperationException("JWT settings are missing.");

        if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) || jwtSettings.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters.");
        }

        builder.RegisterInstance(jwtSettings).AsSelf().SingleInstance();

        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsImplementedInterfaces()
            .Except<WayfarerDbConnectionFactory>()
            .Except<DbSession>()
            .Except<GoogleCloudStorageClient>()
            .InstancePerLifetimeScope();

        builder.RegisterType<DbSession>()
            .As<IDbSession>()
            .As<IUnitOfWork>()
            .InstancePerLifetimeScope();

        builder.RegisterInstance(new WayfarerDbConnectionFactory(connectionString))
            .As<IWayfarerDbConnectionFactory>()
            .SingleInstance();

        builder.RegisterType<GoogleCloudStorageClient>()
            .As<IGoogleCloudStorageClient>()
            .WithParameter((pi, ctx) => pi.ParameterType == typeof(Microsoft.Extensions.Configuration.IConfiguration), (pi, ctx) => ctx.Resolve<Microsoft.Extensions.Configuration.IConfiguration>())
            .SingleInstance();
    }
}
