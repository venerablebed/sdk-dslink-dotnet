using System.Collections.Generic;
using System.Threading;
using DSLink;
using DSLink.NET;
using DSLink.Util.Logger;
using System.Threading.Tasks;
using DSLink.Respond;
using DSLink.Nodes;
using System;

namespace RNG
{
    public class ExampleDSLink : DSLinkContainer
    {
        //private byte[] picture = File.ReadAllBytes("/Users/logan/Pictures/very_large_test.jpg");

        public ExampleDSLink(Configuration config) : base(config)
        {
            /*Task.Run(async () =>
            {
                await Task.Delay(2000);
                while (true)
                {
                    Disconnect();
                    await Connect();
                    await Task.Delay(50);
                }
            });*/

            Requester.List("/upstream/GorenceHome/downstream/", (ListResponse response) =>
            {
                Console.WriteLine(response.Node.Path);
                foreach (KeyValuePair<string, Node> kp in response.Node.Children)
                {
                    Console.WriteLine(kp.Value.Path);
                }
            });

            /*var testNode = Responder.SuperRoot.CreateChild("test")
                                    .SetDisplayName("Test")
                                    .SetType("bytes")
                                    .SetValue(new byte[] { 0x00, 0x01, 0x02 })
                                    .BuildNode();

            var numberNode = Responder.SuperRoot.CreateChild("number")
                                      .SetDisplayName("Number")
                                      .SetType("number")
                                      .SetValue(0.0)
                                      .BuildNode();

            var numberAction = Responder.SuperRoot.CreateChild("set_number")
                                        .SetDisplayName("Set Number")
                                        .SetWritable(Permission.Write)
                                        .AddParameter(new Parameter("Number", "number"))
                                        .SetAction(new Action(Permission.Write, (Dictionary<string, Value> parameters, InvokeRequest request) =>
                                        {
                                            numberNode.Value.Set(parameters["Number"].Get());
                                            request.Close();
                                        }))
                                        .BuildNode();*/

            /*var test = Responder.SuperRoot.CreateChild("bytes")
                                .SetType("binary")
                                .BuildNode();

            var getPicture = Responder.SuperRoot.CreateChild("get_picture")
                                      .AddColumn(new Column("bytes", "binary"))
                                      .SetInvokable(Permission.Write)
                                      .SetAction(new Action(Permission.Write, (parameters, request) =>
                                      {
                                        request.SendUpdates(new List<dynamic>
                                        {
                                            new List<dynamic>
                                            {
                                                picture
                                            }
                                        }, true);
                                      }))
                                      .BuildNode();*/

            /*var task = new Task(() =>
            {
                //while (true)
                {
                    //byte[] buffer = new byte[random.Next(1, 10)];
                    byte[] buffer = new byte[4000000];
                    random.NextBytes(buffer);
                    testNode.Value.Set(buffer);
                    Thread.Sleep(10);
                }
            });
            task.Start();*/
        }

        private static void Main(string[] args)
        {
            Initialize();

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        public static async void Initialize()
        {
            NETPlatform.Initialize();
            var dslink =
                new ExampleDSLink(new Configuration(new List<string>(), "sdk-dotnet",
                                                    responder: true, requester: true,
                                                    logLevel: LogLevel.Debug,
                                                    communicationFormat: "json",
                                                    connectionAttemptLimit: -1));
            
            dslink.Connect();
        }
    }
}
