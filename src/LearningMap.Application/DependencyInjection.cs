using FluentValidation;
using LearningMap.Application.Common;
using LearningMap.Application.Mappings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LearningMap.Application;

public static class DependencyInjection
{
    /// <summary>Registers everything this layer owns. Called from Program.cs.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);           // finds every IRequestHandler<,>
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));   // runs validators before each handler
        });

        services.AddValidatorsFromAssembly(assembly);             // finds every AbstractValidator<T>
        services.AddAutoMapper(typeof(MappingProfile).Assembly);  // finds every Profile

        // .NET 8's abstraction over "now". Tests can swap in a fake clock.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
