﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nimbus.Configuration;
using Nimbus.Extensions;
using Nimbus.IntegrationTests;

namespace Nimbus.ThroughputTests
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var connstring = CommonResources.ConnectionString;

            var messageCount = 50;

            var broker = new FakeBroker(messageCount);

            var typeProvider = new AssemblyScanningTypeProvider(typeof (Program).Assembly);

            var bus = new BusBuilder()
                .Configure()
                .WithConnectionString(connstring)
                .WithInstanceName(Environment.MachineName + ".ThroughputTests")
                .WithEventBroker(broker)
                .WithCommandBroker(broker)
                .WithRequestBroker(broker)
                .WithHandlerTypesFrom(typeProvider)
                .Build();

            Console.WriteLine("Press any key to start");
            Console.ReadKey();

            bus.Start();

            var sw = Stopwatch.StartNew();
            var tasks = Enumerable.Range(0, messageCount)
                                  .AsParallel()
                                  .Select(i => bus.Publish(new MyEvent()))
                                  .Do(t => Console.Write("."))
                                  .ToArray();
            Task.WaitAll(tasks);

            Console.WriteLine();
            Console.WriteLine("Finished sending messages. Waiting for them to all find their way back...");
            broker.WaitUntilDone();
            sw.Stop();

            Console.WriteLine("All done. Took {0} milliseconds to process {1} messages", sw.ElapsedMilliseconds, messageCount);
            var messagesPerSecond = messageCount/sw.Elapsed.TotalSeconds;
            Console.WriteLine("Average throughput: {0} messages/second", messagesPerSecond);
            Console.ReadKey();
        }
    }
}