using System;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopMemo.Infrastructure
{
    public class ServiceLocator
    {
        private static ServiceProvider? _serviceProvider;

        public static IServiceProvider ServiceProvider => _serviceProvider
            ?? throw new InvalidOperationException("ServiceProvider not initialized");

        public static void Initialize(IServiceCollection services)
        {
            _serviceProvider = services.BuildServiceProvider();
        }

        public static T GetService<T>() where T : class
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        public static void Dispose()
        {
            _serviceProvider?.Dispose();
            _serviceProvider = null;
        }
    }
}