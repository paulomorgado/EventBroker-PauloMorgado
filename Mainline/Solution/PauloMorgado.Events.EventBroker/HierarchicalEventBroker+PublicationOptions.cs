//-----------------------------------------------------------------------
// <copyright file="HierarchicalEventBroker+PublicationOptions.cs"
//            project="PauloMorgado.Events.EventBroker"
//            assembly="PauloMorgado.Events.EventBroker"
//            solution="PMEventBroker"
//            company="Paulo Morgado">
//     Copyright (c) Paulo Morgado. All rights reserved.
// </copyright>
// <author>Paulo Morgado</author>
// <summary>Specifies the event publication options.</summary>
//-----------------------------------------------------------------------

namespace PauloMorgado.Events
{

    /// <content>
    /// Specifies the event publication options.
    /// </content>
    public partial class HierarchicalEventBroker
    {
        /// <summary>
        /// Specifies the event publication options.
        /// </summary>
        public enum PublicationOptions : int
        {
            /// <summary>
            /// Events are published from the current <see cref="T:HierarchicalEventBroker"/> to its descendents.
            /// </summary>
            TopDown,

            /// <summary>
            /// Events are published from the current <see cref="T:HierarchicalEventBroker"/> to its ascendents.
            /// </summary>
            BottomUp,

            /// <summary>
            /// Events are published from the current <see cref="T:HierarchicalEventBroker"/> to both its ascendents and descendents.
            /// </summary>
            All
        }
    }
}
