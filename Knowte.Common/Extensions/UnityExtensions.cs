using Microsoft.Practices.Unity;

namespace Knowte.Common.Extensions
{
    public static class UnityExtensions
    {
        public static void RegisterSingletonType<TFrom, TTo>(this IUnityContainer container) where TTo : TFrom
        {
            container.RegisterType<TFrom, TTo>(new ContainerControlledLifetimeManager());
        }
    }
}
