using System.Threading.Tasks;
using DSLink.Connection;

namespace DSLink.Test.Implementation
{
    public class TestConnector : Connector
    {
        public TestConnector(Configuration config) : base(config)
        {
        }

        protected override Task<ConnectionState> Open()
        {
            return Task.FromResult(ConnectionState.Connected);
        }

        protected override Task<ConnectionState> Close()
        {
            return Task.FromResult(ConnectionState.Disconnected);
        }

        protected override Task Write(string data)
        {
            return Task.FromResult(true);
        }

        protected override Task Write(byte[] data)
        {
            return Task.FromResult(true);
        }
    }
}