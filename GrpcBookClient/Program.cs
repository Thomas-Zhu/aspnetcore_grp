using Grpc.Core;
using Grpc.Net.Client;
using GrpcBook;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GrpcBookClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new Greeter.GreeterClient(channel);
            var reply = await client.SayHelloAsync(new HelloRequest { Name = "GrpcBook 客户端" });
            Console.WriteLine("GrpcBook 服务端应答：" + reply.Message);
            var racerClient = new Racer.RacerClient(channel);
            await BidirectionalStreamingExample(racerClient);
            Console.WriteLine("Shutting down");
            Console.ReadLine();
        }

        private static async Task BidirectionalStreamingExample(Racer.RacerClient client)
        {
            var RaceDuration = TimeSpan.FromSeconds(3);
            var headers = new Metadata
            {
                new Metadata.Entry("race-duration",RaceDuration.ToString())
            };
            Console.WriteLine("Ready,set,go!");
            using (var call=client.ReadySetGo(new CallOptions(headers)))
            {
                RaceMessage? lastMessageReceived = null;
                var readTask = Task.Run(async () =>
                  {
                      await foreach (var item in call.ResponseStream.ReadAllAsync())
                      {
                          lastMessageReceived = item;
                      }
                  });
                var sw = Stopwatch.StartNew();
                var sent = 0;
                while (sw.Elapsed<RaceDuration)
                {
                    await call.RequestStream.WriteAsync(new RaceMessage { Count=++sent});
                }
                await call.RequestStream.CompleteAsync();
                await readTask;
                Console.WriteLine($"消息发送次数: {sent}");
                Console.WriteLine($"消息接收次数: {lastMessageReceived?.Count ?? 0}");
            }
        }
    }
}
