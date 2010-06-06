//-----------------------------------------------------------------------
// <copyright file="EventData.cs"
//            project="PauloMorgado.Events.EventBroker"
//            assembly="PauloMorgado.Events.EventBroker"
//            solution="PMEventBroker"
//            company="Paulo Morgado">
//     Copyright (c) Paulo Morgado. All rights reserved.
// </copyright>
// <author>Paulo Morgado</author>
// <summary>Contains event data.</summary>
//-----------------------------------------------------------------------

namespace PauloMorgado.Events
{
    using System;
    using System.Diagnostics.Contracts;

    /// <summary>
    /// Contains event data.
    /// </summary>
    public class EventData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventData"/> class.
        /// </summary>
        /// <param name="event">The event token.</param>
        /// <param name="publisher">The publisher of the event.</param>
        /// <param name="arguments">The <see cref="System.EventArgs"/> instance containing the event arguments.</param>
        public EventData(object @event, object publisher, EventArgs arguments)
        {
            Contract.Requires(@event != null, "event is null.");
            Contract.Requires(publisher != null, "publisher is null.");
            Contract.Requires(arguments != null, "arguments is null.");

            this.Event = @event;
            this.Publisher = publisher;
            this.Arguments = arguments;
        }

        /// <summary>
        /// Gets the publisher of the event.
        /// </summary>
        /// <value>The publisher of the event.</value>
        public object Publisher { get; private set; }

        /// <summary>
        /// Gets the event token.
        /// </summary>
        /// <value>The event token.</value>
        public object Event { get; private set; }

        /// <summary>
        /// Gets the event arguments.
        /// </summary>
        /// <value>The <see cref="System.EventArgs"/> instance containing the event data.</value>
        public EventArgs Arguments { get; private set; }
    }
}
