using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DSLink.Broker
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            app.UseWebSockets();

            app.Use(WebSocketEndpoint);
        }

        private async Task WebSocketEndpoint(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path == "/ws")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var dsId = context.Request.Query["dsId"];
                    var auth = context.Request.Query["auth"];
                    var token = context.Request.Query["token"];
                    var format = context.Request.Query["format"];

                    var link = Program.Broker.ConnectionHandler.GetLink(dsId);
                    if (link == null)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.ProtocolError,
                            "Handshake was not performed", CancellationToken.None);
                    }
                    else
                    {
                        await HandleConnection(webSocket, link);
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                }
            }
            else
            {
                await next();
            }
        }

        private static async Task HandleConnection(WebSocket webSocket, ServerLink link)
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Text:
                        link.ReceiveStringMessage(Encoding.UTF8.GetString(buffer));
                        break;
                    case WebSocketMessageType.Binary:
                        link.ReceiveBinaryMessage(buffer);
                        break;
                    case WebSocketMessageType.Close:
                        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription,
                            CancellationToken.None);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
