using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;

namespace GrpcBook
{
    public class RacerService : Racer.RacerBase
    {
        public override async Task ReadySetGo(IAsyncStreamReader<RaceMessage> requestStream, IServerStreamWriter<RaceMessage> responseStream, ServerCallContext context)
        {
            var raceDuration = TimeSpan.Parse(context.RequestHeaders.Single(h => h.Key == "race-duration").Value);

            RaceMessage? lastMessageReceived = null;
            var readTask = Task.Run(async () =>
            {
                await foreach (var message in requestStream.ReadAllAsync())
                {
                    lastMessageReceived = message;
                }
            });

            var sw = Stopwatch.StartNew();
            var sent = 0;
            while (sw.Elapsed < raceDuration)
            {
                await responseStream.WriteAsync(new RaceMessage { Count = ++sent });
            }

            Console.WriteLine($"消息发送次数: {sent}");
            Console.WriteLine($"消息接收次数: {lastMessageReceived?.Count ?? 0}");
            await readTask;
        }
    }
}
