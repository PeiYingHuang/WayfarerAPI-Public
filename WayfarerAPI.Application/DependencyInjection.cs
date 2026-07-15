using Autofac;

namespace WayfarerAPI.Application;

public class ApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }
}
