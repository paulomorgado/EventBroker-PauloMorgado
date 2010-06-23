//-----------------------------------------------------------------------
// <copyright file="EventBroker+EventInfo.cs"
//            project="PauloMorgado.Events.EventBroker"
//            assembly="PauloMorgado.Events.EventBroker"
//            solution="PMEventBroker"
//            company="Paulo Morgado">
//     Copyright (c) Paulo Morgado. All rights reserved.
// </copyright>
// <author>Paulo Morgado</author>
// <summary>Event information.</summary>
//-----------------------------------------------------------------------

namespace PauloMorgado.Events
{
    using System.Collections.Generic;

    /// <content>
    /// Event information.
    /// </content>
    public partial class EventBroker
    {
        /// <summary>
        /// Subscription information.
        /// </summary>
        private class EventInfo
        {
            /// <summary>
            /// The publisher.
            /// </summary>
            public object Publisher;

            /// <summary>
            /// The list of subscriptions.
            /// </summary>
            private List<EventSubscriptionInfo> subscriptions;

            /// <summary>
            /// Gets the list of subscriptions.
            /// </summary>
            /// <value>The list of subscriptions.</value>
            public List<EventSubscriptionInfo> Subscriptions
            {
                get
                {
                    if (this.subscriptions == null)
                    {
                        this.subscriptions = new List<EventSubscriptionInfo>();
                    }

                    return this.subscriptions;
                }
            }
        }
    }
}
