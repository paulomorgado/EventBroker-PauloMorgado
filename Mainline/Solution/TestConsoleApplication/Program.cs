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
            Console.WriteLine("Creating event brokers");
            var events1 = new HierarchicalEventBroker();
            var events11 = events1.CreateChild();
            var events12 = events1.CreateChild();
            var events111 = events11.CreateChild();
            var events121 = events12.CreateChild();
            Console.WriteLine();

            SubscribeEvent(events1, "event", "subscriber1");
            SubscribeEvent(events11, "event", "subscriber1");
            SubscribeEvent(events12, "event", "subscriber2");
            SubscribeEvent(events111, "event", "subscriber2");
            SubscribeEvent(events121, "event", "subscriber1");

            PublishEvent(events1, "event", "publisher 1", EventArgs.Empty);
            PublishEvent(events11, "event", "publisher 11", EventArgs.Empty);
            PublishEvent(events12, "event", "publisher 12", EventArgs.Empty);
            PublishEvent(events111, "event", "publisher 111", EventArgs.Empty);
            PublishEvent(events121, "event", "publisher 121", EventArgs.Empty);

            events12.Unsubscribe("subscriber2");
            events111.Unsubscribe("subscriber2");

            PublishEvent(events1, "event", "publisher 1", EventArgs.Empty);
            PublishEvent(events11, "event", "publisher 11", EventArgs.Empty);
            PublishEvent(events12, "event", "publisher 12", new CancelEventArgs());
            PublishEvent(events111, "event", "publisher 111", EventArgs.Empty);
            PublishEvent(events121, "event", "publisher 121", new CancelEventArgs());

            Console.WriteLine();
            events1.AcquireExclusive("event", "publisher 1");
            Console.WriteLine();

            try
            {
                events1.AcquireExclusive("event", "publisher 1");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine();

            try
            {
                events1.AcquireExclusive("event", "publisher 2");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine();

            try
            {
                events1.ReleaseExclusive("event", "publisher 2");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine();

            PublishEvent(events1, "event", "publisher 2", EventArgs.Empty);

            try
            {
                events12.Dispose();
                events12.Publish("event", "publisher disposed 12", EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine();
        }

        private static void SubscribeEvent(EventBroker eventBroker, string @event, string subscriber)
        {
            Console.WriteLine("Subscribing to event. Event: {0}, Subscriber: {1}", @event, subscriber);

            try
            {
                eventBroker.Subscribe(@event, subscriber, e => Handler(subscriber, e));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine();
        }

        private static void PublishEvent(EventBroker eventBroker, string @event, string publisher, EventArgs eventArgs)
        {
            Console.WriteLine("Publishing event. Event: {0}, Publisher: {1}", @event, publisher);

            try
            {
                eventBroker.Publish(@event, publisher, eventArgs);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine();
        }
    }
}
