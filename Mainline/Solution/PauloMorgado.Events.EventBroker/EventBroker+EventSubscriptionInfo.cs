//-----------------------------------------------------------------------
// <copyright file="EventBroker+EventSubscriptionInfo.cs"
//            project="PauloMorgado.Events.EventBroker"
//            assembly="PauloMorgado.Events.EventBroker"
//            solution="PMEventBroker"
//            company="Paulo Morgado">
//     Copyright (c) Paulo Morgado. All rights reserved.
// </copyright>
// <author>Paulo Morgado</author>
// <summary>Subscription information.</summary>
//-----------------------------------------------------------------------

namespace PauloMorgado.Events
{
    using System;
    using System.Diagnostics.Contracts;

    /// <content>
    /// Subscription information.
    /// </content>
    public partial class EventBroker
    {
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
                Contract.Assume(subscriber != null, "subscriber is null.");
                Contract.Assume(handler != null, "handler is null.");

                this.Subscriber = subscriber;
                this.Handler = handler;
            }
        }
    }
}
