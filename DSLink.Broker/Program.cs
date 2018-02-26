using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace DSLink.Broker
{
    public class Program
    {
        public static DSBroker Broker;
        
        public static void Main(string[] args)
        {
            Broker = new DSBroker();
            
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
