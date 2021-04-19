using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Formatter;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace MqttConsole
{
    class Program
    {
        static async Task Main(string[] _)
        {
            try
            {
                var factory = new MqttFactory();

                var serverOptions = new MqttServerOptionsBuilder()
                    .WithDefaultEndpointPort(1234)
                    .WithClientCertificate()
                    .WithClientId("TestServer")
                    .WithPersistentSessions()
                    .Build();

                var clientOptions = new MqttClientOptionsBuilder()
                    // .WithProtocolVersion(MqttProtocolVersion.V500)
                    .WithClientId("TestClient")
                    .WithCredentials("mqtt-user", "mqtt-user")
                    .WithTcpServer("172.16.20.212", 1883)
                    .WithCleanSession()
                    .WithSessionExpiryInterval(0xFFFFFFFF)
                    .Build();

                var server = factory.CreateMqttServer();
                server.ClientDisconnectedHandler = new MqttServerClientDisconnectedHandlerDelegate(args =>
                {
                    Console.WriteLine($"client {args.ClientId} disconnected ({args.DisconnectType})");
                });
                await server.StartAsync(serverOptions);

                var client = factory.CreateMqttClient();

                client.UseApplicationMessageReceivedHandler(msg =>
                {
                    Console.WriteLine($"Received msg, payload len: " + msg?.ApplicationMessage?.Payload?.Length);
                });

                await client.ConnectAsync(clientOptions, CancellationToken.None);

                await client.SubscribeAsync(
                    new MqttTopicFilter
                    {
                        Topic = "/test/+",
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce
                    }
                );

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("/test/test2")
                    .WithPayload("{ \"test\": 42 }")
                    .WithPayloadFormatIndicator(MqttPayloadFormatIndicator.CharacterData)
                    .WithContentType("application/json")
                    .WithExactlyOnceQoS()
                    .Build();

                message.UserProperties = new List<MqttUserProperty>
                {
                    new MqttUserProperty("TestProp", "TestPropValue")
                };

                await server.PublishAsync(message);

                Console.WriteLine("done");
                Console.ReadLine();
                client.Dispose();
                await server.StopAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
