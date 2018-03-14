using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace DSLink.Request
{
    public class DSLinkRequesterInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<DSLinkRequester>().ImplementedBy<DSLinkRequester>());
            container.Register(Component.For<RequestManager>().ImplementedBy<RequestManager>());
            container.Register(Component.For<RemoteSubscriptionManager>().ImplementedBy<RemoteSubscriptionManager>());
        }
    }
}