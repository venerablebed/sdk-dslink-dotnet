using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace DSLink.Respond
{
    public class DSLinkResponderInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(Component.For<Responder>().ImplementedBy<DSLinkResponder>());
            container.Register(Component.For<SubscriptionManager>().ImplementedBy<SubscriptionManager>());
            container.Register(Component.For<StreamManager>().ImplementedBy<StreamManager>());
            container.Register(Component.For<DiskSerializer>().ImplementedBy<DiskSerializer>());
        }
    }
}