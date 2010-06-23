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
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Threading;

    /// <summary>
    /// An event broker that delivers published events to he interested subscribers.
    /// </summary>
    public partial class EventBroker : IDisposable
    {
        /// <summary>
        /// The trace source.
        /// </summary>
        private readonly TraceSource traceSource = new TraceSource("PauloMorgado.Events.EventBroker", SourceLevels.Error);

        /// <summary>
        /// Synchronizes access to the <see cref="F:subscriptions" />.
        /// </summary>
        private readonly ReaderWriterLockSlim sync = new ReaderWriterLockSlim();

        /// <summary>
        /// The list of subscriptions.
        /// </summary>
        private Dictionary<object, EventInfo> events = new Dictionary<object, EventInfo>();

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
        /// Gets the trace source.
        /// </summary>
        /// <value>The trace source.</value>
        protected virtual TraceSource TraceSource
        {
            get { return this.traceSource; }
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

            Contract.Assume(this.events != null, "events is null");
            Contract.Assume(this.sync != null, "sync is null");

            try
            {
                this.sync.EnterWriteLock();

                EventInfo eventInfo = GetEventInfo(@event);

                Contract.Assert(eventInfo.Subscriptions != null, "eventInfo.Subscribers is null");

                eventInfo.Subscriptions.Add(new EventSubscriptionInfo(subscriber, handler));
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

            Contract.Assume(this.sync != null, "sync is null");
            Contract.Assume(this.events != null, "subscriptions is null");

            this.sync.EnterWriteLock();

            try
            {
                EventInfo eventInfo;

                if (this.events.TryGetValue(@event, out eventInfo))
                {
                    Contract.Assume(eventInfo != null, "eventInfo is null.");

                    List<EventSubscriptionInfo> subsciptions = eventInfo.Subscriptions;

                    Contract.Assert(subsciptions != null, "subscribers is null.");

                    subsciptions
                        .FindAll(s => (s.Subscriber == subscriber) && (s.Handler == handler))
                        .ForEach(s => subsciptions.Remove(s));
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
            Contract.Assume(this.events != null, "subscriptions is null");

            try
            {
                this.sync.EnterWriteLock();

                foreach (var eventItem in this.events)
                {
                    Contract.Assume(eventItem.Key != null, "eventItem.Key is null.");
                    Contract.Assume(eventItem.Value != null, "eventItem.Value is null.");
                    Contract.Assert(eventItem.Value.Subscriptions != null, "eventItem.Value.Subscribers is null.");

                    eventItem.Value.Subscriptions
                        .FindAll(s => s.Subscriber == subscriber)
                        .ForEach(s => eventItem.Value.Subscriptions.Remove(s));
                }
            }
            finally
            {
                this.sync.ExitWriteLock();
            }
        }

        /// <summary>
        /// Acquires exclusive publication rigths to the supplied <paramref name="event"/> for the specified <paramref name="publisher"/>.
        /// </summary>
        /// <param name="event">The event token.</param>
        /// <param name="publisher">The publisher of the event.</param>
        /// <exception cref="InvalidOperationException">
        /// Exclusive publication has been aquired by another publisher.
        /// </exception>
        public void AcquireExclusive(object @event, object publisher)
        {
            if (!this.TryAcquireExclusiveInternal(@event, publisher))
            {
                throw new InvalidOperationException("Exclusive publication has been aquired by another publisher.");
            }
        }

        /// <summary>
        /// Acquires exclusive publication rigths to the supplied <paramref name="event"/> for the specified <paramref name="publisher"/>.
        /// </summary>
        /// <param name="event">The event token.</param>
        /// <param name="publisher">The publisher of the event.</param>
        /// <returns><see langword="true" /> if exclusive publication hadn't been acquired by another publisher; otherwise <see langword="false" />.</returns>
        public bool TryAcquireExclusive(object @event, object publisher)
        {
            Contract.Requires(@event != null, "event is null.");
            Contract.Requires(publisher != null, "publisher is null.");

            return this.TryAcquireExclusiveInternal(@event, publisher);
        }

        /// <summary>
        /// Releases exclusive publication rigths to the supplied <paramref name="event"/> for the specified <paramref name="publisher"/>.
        /// </summary>
        /// <param name="event">The event token.</param>
        /// <param name="publisher">The publisher of the event.</param>
        /// <exception cref="InvalidOperationException">
        /// Exclusive publication has been aquired by another publisher.
        /// </exception>
        public void ReleaseExclusive(object @event, object publisher)
        {
            if (!this.TryReleaseExclusiveInternal(@event, publisher))
            {
                throw new InvalidOperationException("Exclusive publication has been aquired by another publisher.");
            }
        }

        /// <summary>
        /// Releases exclusive publication rigths to the supplied <paramref name="event"/> for the specified <paramref name="publisher"/>.
        /// </summary>
        /// <param name="event">The event token.</param>
        /// <param name="publisher">The publisher of the event.</param>
        /// <returns><see langword="true" /> if exclusive publication has been aquired by another publisher; otherwise <see langword="false" />.</returns>
        public bool TryReleaseExclusive(object @event, object publisher)
        {
            Contract.Requires(@event != null, "event is null.");
            Contract.Requires(publisher != null, "publisher is null.");

            return this.TryReleaseExclusiveInternal(@event, publisher);
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
            Contract.Assume(this.sync != null, "sync is null");
            Contract.Assume(this.TraceSource != null, "TraceSource is null");

            this.TraceSource.TraceEvent(TraceEventType.Information, 0, "Publishing event: {0}", eventData.Event);

            this.EnsureNotDisposed();

            this.sync.EnterReadLock();

            try
            {
                Contract.Assume(this.events != null, "events is null");

                EventInfo eventInfo;

                if (this.events.TryGetValue(eventData.Event, out eventInfo))
                {
                    Contract.Assume(eventInfo != null, "eventInfo is null");
                    Contract.Assert(eventInfo.Subscriptions != null, "eventInfo.Subscribers is null");

                    this.CheckAllowedPublisher(eventInfo, eventData);

                    foreach (var subscription in eventInfo.Subscriptions)
                    {
                        CallEventSubscriptionHandler(subscription, eventData);
                    }
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

        private void CheckAllowedPublisher(EventInfo eventInfo, EventData eventData)
        {
            Contract.Assume(eventInfo != null, "eventInfo is null");
            Contract.Assume(eventData != null, "eventData is null");

            if ((eventInfo.Publisher != null) && !eventInfo.Publisher.Equals(eventData.Publisher))
            {
                Contract.Assume(this.TraceSource != null, "TraceSource is null");

                this.TraceSource.TraceEvent(
                   TraceEventType.Error,
                   0,
                   "Invalid event publisher. Registered publisher: {0} Event publisher: {1}",
                   eventInfo.Publisher,
                   eventData.Publisher);

                throw new InvalidOperationException("Invalid event publisher.");
            }
        }

        /// <summary>
        /// Calls the event subscription handler.
        /// </summary>
        /// <param name="subscription">The event subscription.</param>
        /// <param name="eventData">The <see cref="T:EventData"></see> instance containing the event data.</param>
        private void CallEventSubscriptionHandler(EventSubscriptionInfo subscription, EventData eventData)
        {
            Contract.Assume(subscription.Handler != null, "subscription.Handler is null");
            Contract.Assume(eventData != null, "eventData is null");

            try
            {
                subscription.Handler(eventData);
            }
            catch (Exception ex)
            {
                this.TraceSource.TraceEvent(
                    TraceEventType.Error,
                    0,
                    "Error calling event subscription handler. Subscriber: {0} Error: {1}",
                    subscription.Subscriber,
                    ex);
            }
        }

        /// <summary>
        /// Gets the event information (from the list of events) corresponding to the supplied <paramref name="event"/>.
        /// </summary>
        /// <param name="event">The @event.</param>
        /// <returns>the event information corresponding to the supplied <paramref name="event"/>.</returns>
        /// <remarks>If the requested event information doesn't exit, it's created and added to the list.</remarks>
        private EventInfo GetEventInfo(object @event)
        {
            Contract.Ensures(Contract.Result<EventInfo>() != null);
            Contract.Assume(@event != null, "event is null.");
            Contract.Assume(this.events != null, "events is null");

            EventInfo eventInfo;
            if (!this.events.TryGetValue(@event, out eventInfo))
            {
                eventInfo = new EventInfo();
                this.events.Add(@event, eventInfo);
            }

            Contract.Assume(eventInfo != null, "eventInfo is null");

            return eventInfo;
        }

        /// <summary>
        /// Acquires exclusive publication rigths to the supplied <paramref name="event"/> for the specified <paramref name="publisher"/>.
        /// </summary>
        /// <param name="event">The event token.</param>
        /// <param name="publisher">The publisher of the event.</param>
        /// <returns><see langword="true" /> if exclusive publication hadn't been acquired by another publisher; otherwise <see langword="false" />.</returns>
        private bool TryAcquireExclusiveInternal(object @event, object publisher)
        {
            Contract.Assume(@event != null, "event is null.");
            Contract.Assume(publisher != null, "publisher is null.");

            EventInfo eventInfo = GetEventInfo(@event);

            if ((eventInfo.Publisher != null) && !eventInfo.Publisher.Equals(publisher))
            {
                return false;
            }

            eventInfo.Publisher = publisher;

            return true;
        }

        private bool TryReleaseExclusiveInternal(object @event, object publisher)
        {
            Contract.Assume(@event != null, "event is null.");
            Contract.Assume(publisher != null, "publisher is null.");

            EventInfo eventInfo = GetEventInfo(@event);

            if ((eventInfo.Publisher != null) && eventInfo.Publisher.Equals(publisher))
            {
                eventInfo.Publisher = null;

                return true;
            }

            return false;
        }
    }
}
