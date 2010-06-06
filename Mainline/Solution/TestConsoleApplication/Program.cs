using System;
using System.ComponentModel;
using PauloMorgado.Events;

namespace TestConsoleApplication
{
    class Program
    {
        private static void Handler(string subscriber, EventData eventData)
        {
            Console.WriteLine("Subscriber: {0}, Event: {1}, Publisher: {2}, Arguments: {3}", subscriber, eventData.Event, eventData.Publisher, eventData.Arguments);
        }

        static void Main(string[] args)
        {
            var events1 = new HierarchicalEventBroker();
            var events11 = events1.CreateChild();
            var events12 = events1.CreateChild();
            var events111 = events11.CreateChild();
            var events121 = events12.CreateChild();

            events1.Subscribe("event", "subscriber1", e => Handler("subscriber1", e));
            events11.Subscribe("event", "subscriber1", e => Handler("subscriber1", e));
            events12.Subscribe("event", "subscriber2", e => Handler("subscriber2", e));
            events111.Subscribe("event", "subscriber2", e => Handler("subscriber2", e));
            events121.Subscribe("event", "subscriber1", e => Handler("subscriber1", e));

            events1.Publish("event", "publisher 1", EventArgs.Empty);
            Console.WriteLine();

            events11.Publish("event", "publisher 11", EventArgs.Empty);
            Console.WriteLine();

            events12.Publish("event", "publisher 12", CancelEventArgs.Empty);
            Console.WriteLine();

            events111.Publish("event", "publisher 111", EventArgs.Empty);
            Console.WriteLine();

            events121.Publish("event", "publisher 121", EventArgs.Empty);
            Console.WriteLine();

            events12.Unsubscribe("subscriber2");
            events111.Unsubscribe("subscriber2");

            events1.Publish("event", "publisher 1", EventArgs.Empty);
            Console.WriteLine();

            events11.Publish("event", "publisher 11", EventArgs.Empty);
            Console.WriteLine();

            events12.Publish("event", "publisher 12", CancelEventArgs.Empty);
            Console.WriteLine();

            events111.Publish("event", "publisher 111", EventArgs.Empty);
            Console.WriteLine();

            events121.Publish("event", "publisher 121", EventArgs.Empty);
            Console.WriteLine();

            events12.Dispose();
            events12.Publish("event", "publisher disposed 12", EventArgs.Empty);
        }
    }
}
