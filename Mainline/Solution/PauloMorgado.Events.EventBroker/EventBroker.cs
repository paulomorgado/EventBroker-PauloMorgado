//-----------------------------------------------------------------------
// <copyright file="EventBroker.cs"
//            project="PauloMorgado.Events.EventBroker"
//            assembly="PauloMorgado.Events.EventBroker"
//            solution="PMEventBroker"
//            company="Paulo Morgado">
//     Copyright (c) Paulo Morgado. All rights reserved.
// </copyright>
// <author>Paulo Morgado</author>
// <summary>An event broker that delivers published events to he interested subscribers.</summary>
//-----------------------------------------------------------------------

namespace PauloMorgado.Events
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Threading;

    /// <summary>
    /// An event broker that delivers published events to he interested subscribers.
    /// </summary>
    public class EventBroker : IDisposable
    {
        /// <summary>
        /// Synchronizes access to the <see cref="F:subscriptions" />.
        /// </summary>
        private readonly ReaderWriterLockSlim sync = new ReaderWriterLockSlim();

        /// <summary>
        /// The list of subscriptions.
        /// </summary>
        private Dictionary<object, List<EventSubscriptionInfo>> subscriptions = new Dictionary<object, List<EventSubscriptionInfo>>();

        /// <summary>
        /// <see langword="true" /> if this instance has already been disposed; otherwise <see langword="false" />.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventBroker"/> class.
        /// </summary>
        public EventBroker()
        {
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="EventBroker"/> class.
        /// </summary>
        ~EventBroker()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Publishes an event.
        /// </summary>
        /// <param name="event">The event token.</param>
        /// <param name="publisher">The publisher of the event.</param>
        /// <param name="arguments">The <see cref="T:System.EventArgs"/> instance containing the event arguments.</param>
        public void Publish(object @event, object publisher, EventArgs arguments)
        {
            Contract.Requires(@event != null, "event is null.");
            Contract.Requires(publisher != null, "publisher is null.");
            Contract.Requires(arguments != null, "arguments is null.");

            EventData eventData = new EventData(@event, publisher, arguments);

            this.PublishInternal(eventData);
        }

        /// <summary>
        /// Publishes an event.
        /// </summary>
        /// <param name="eventData">The <see cref="T:EventData"/> instance containing the event data.</param>
        public void Publish(EventData eventData)
        {
            Contract.Requires(eventData != null, "eventData is null");

            this.PublishInternal(eventData);
        }

        /// <summary>
        /// Subscribes the event.
        /// </summary>
        /// <param name="event">The event token.</param>
        /// <param name="subscriber">The subscriber token.</param>
        /// <param name="handler">The handler.</param>
        public void Subscribe(object @event, object subscriber, Action<EventData> handler)
        {
            Contract.Requires(@event != null, "event is null.");
            Contract.Requires(subscriber != null, "subscriber is null.");
            Contract.Requires(handler != null, "handler is null.");

            this.EnsureNotDisposed();

            List<EventSubscriptionInfo> subscribers;

            Contract.Assume(this.subscriptions != null, "subscriptions is null");
            Contract.Assume(this.sync != null, "sync is null");

            try
            {
                this.sync.EnterWriteLock();

                if (!this.subscriptions.TryGetValue(@event, out subscribers))
                {
                    subscribers = new List<EventSubscriptionInfo>();
                    this.subscriptions.Add(@event, subscribers);
                }

                Contract.Assume(subscribers != null, "subscribers is null");

                subscribers.Add(new EventSubscriptionInfo(subscriber, handler));
            }
            finally
            {
                this.sync.ExitWriteLock();
            }
        }

        /// <summary>
        /// Unsubscribes the specified event.
        /// </summary>
        /// <param name="event">The event token.</param>
        /// <param name="subscriber">The subscriber token.</param>
        /// <param name="handler">The event handler.</param>
        public void Unsubscribe(object @event, object subscriber, Action<EventData> handler)
        {
            Contract.Requires(@event != null, "event is null.");
            Contract.Requires(subscriber != null, "subscriber is null.");
            Contract.Requires(handler != null, "handler is null.");

            this.EnsureNotDisposed();

            List<EventSubscriptionInfo> subscribers;

            Contract.Assume(this.sync != null, "sync is null");
            Contract.Assume(this.subscriptions != null, "subscriptions is null");

            this.sync.EnterWriteLock();

            try
            {
                if (this.subscriptions.TryGetValue(@event, out subscribers))
                {
                    Contract.Assume(subscribers != null, "subscribers is null.");

                    subscribers
                        .FindAll(s => (s.Subscriber == subscriber) && (s.Handler == handler))
                        .ForEach(s => subscribers.Remove(s));
                }
            }
            finally
            {
                this.sync.ExitWriteLock();
            }
        }

        /// <summary>
        /// Cancels all subscriptions with the specified subscriber token.
        /// </summary>
        /// <param name="subscriber">The subscriber token.</param>
        public void Unsubscribe(object subscriber)
        {
            Contract.Requires(subscriber != null, "subscriber is null.");

            this.EnsureNotDisposed();

            Contract.Assume(this.sync != null, "sync is null");
            Contract.Assume(this.subscriptions != null, "subscriptions is null");

            try
            {
                this.sync.EnterWriteLock();

                foreach (var eventSubscription in this.subscriptions)
                {
                    Contract.Assume(eventSubscription.Value != null, "eventSubscription.Value is null.");

                    eventSubscription.Value
                        .FindAll(s => s.Subscriber == subscriber)
                        .ForEach(s => eventSubscription.Value.Remove(s));
                }
            }
            finally
            {
                this.sync.ExitWriteLock();
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.EnsureNotDisposed();

                Contract.Assume(this.sync != null, "sync is null");

                this.sync.Dispose();

                this.disposed = true;
            }
        }

        /// <summary>
        /// Publishes an event.
        /// </summary>
        /// <param name="eventData">The <see cref="T:EventData"/> instance containing the event data.</param>
        protected virtual void PublishInternal(EventData eventData)
        {
            Contract.Assume(eventData != null, "eventData is null");
            Contract.Assume(this.subscriptions != null, "subscriptions is null");
            Contract.Assume(this.sync != null, "sync is null");

            List<EventSubscriptionInfo> subscribers;

            this.sync.EnterReadLock();

            try
            {
                if (this.subscriptions.TryGetValue(eventData.Event, out subscribers))
                {
                    Contract.Assume(subscribers != null, "subscribers is null");

                    subscribers.ForEach(s => s.Handler(eventData));
                }
            }
            finally
            {
                this.sync.ExitReadLock();
            }
        }

        /// <summary>
        /// Ensures that this instance has not been disposed.
        /// </summary>
        /// <exception cref="T:ObjectDisposedException">
        /// When this instance has already been disposed.
        /// </exception>
        private void EnsureNotDisposed()
        {
            if (this.disposed)
            {
                new ObjectDisposedException("EventBroker");
            }
        }

        /// <summary>
        /// Subscription information.
        /// </summary>
        private struct EventSubscriptionInfo
        {
            /// <summary>
            /// The subscriber token.
            /// </summary>
            public object Subscriber;

            /// <summary>
            /// The event handler.
            /// </summary>
            public Action<EventData> Handler;

            /// <summary>
            /// Initializes a new instance of the <see cref="EventSubscriptionInfo"/> struct.
            /// </summary>
            /// <param name="subscriber">The subscriber token.</param>
            /// <param name="handler">The event handler.</param>
            public EventSubscriptionInfo(object subscriber, Action<EventData> handler)
            {
                this.Subscriber = subscriber;
                this.Handler = handler;
            }

            /// <summary>
            /// Implements the operator ==.
            /// </summary>
            /// <param name="left">The left operand.</param>
            /// <param name="right">The right operand.</param>
            /// <returns>The result of the operator.</returns>
            public static bool operator ==(EventSubscriptionInfo left, EventSubscriptionInfo right)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Implements the operator !=.
            /// </summary>
            /// <param name="left">The left operand.</param>
            /// <param name="right">The right operand.</param>
            /// <returns>The result of the operator.</returns>
            public static bool operator !=(EventSubscriptionInfo left, EventSubscriptionInfo right)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
            /// </summary>
            /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
            /// <returns>
            /// <see langword="true"/> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <see langword="false"/>.
            /// </returns>
            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}
