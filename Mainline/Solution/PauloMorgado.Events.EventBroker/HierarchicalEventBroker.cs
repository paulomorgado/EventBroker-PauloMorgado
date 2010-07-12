//-----------------------------------------------------------------------
// <copyright file="HierarchicalEventBroker.cs"
//            project="PauloMorgado.Events.EventBroker"
//            assembly="PauloMorgado.Events.EventBroker"
//            solution="PMEventBroker"
//            company="Paulo Morgado">
//     Copyright (c) Paulo Morgado. All rights reserved.
// </copyright>
// <author>Paulo Morgado</author>
// <summary>An hierarchical implementation of an event broker.</summary>
//-----------------------------------------------------------------------

namespace PauloMorgado.Events
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Threading;

    /// <summary>
    /// An hierarchical implementation of an <see cref="T:EventBroker" />.
    /// </summary>
    public partial class HierarchicalEventBroker : EventBroker
    {
        /// <summary>
        /// Synchronizes access to the <see cref="F:subscriptions" />.
        /// </summary>
        private readonly ReaderWriterLockSlim sync = new ReaderWriterLockSlim();

        /// <summary>
        /// The list of child <see cref="HierarchicalEventBroker"/>s.
        /// </summary>
        private readonly List<HierarchicalEventBroker> children = new List<HierarchicalEventBroker>();

        /// <summary>
        /// The parent <see cref="HierarchicalEventBroker"/>.
        /// </summary>
        private readonly HierarchicalEventBroker parent;

        /// <summary>
        /// The default publication options.
        /// </summary>
        private readonly PublicationOptions defaultPublicationOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="HierarchicalEventBroker"/> class.
        /// </summary>
        public HierarchicalEventBroker()
            : this(PublicationOptions.TopDown)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HierarchicalEventBroker"/> class.
        /// </summary>
        /// <param name="defaultPublicationOptions">The default publication options.</param>
        public HierarchicalEventBroker(PublicationOptions defaultPublicationOptions)
            : this(null, defaultPublicationOptions, new TraceSource("PauloMorgado.Events.HierarchicalEventBroker", SourceLevels.Error))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HierarchicalEventBroker"/> class.
        /// </summary>
        /// <param name="parent">The parent <see cref="HierarchicalEventBroker"/>.</param>
        /// <param name="defaultPublicationOptions">The default publication options.</param>
        /// <param name="traceSource">The trace source.</param>
        private HierarchicalEventBroker(HierarchicalEventBroker parent, PublicationOptions defaultPublicationOptions, TraceSource traceSource)
            : base(traceSource)
        {
            Contract.Requires(traceSource != null, "traceSource is null.");

            this.parent = parent;
            this.defaultPublicationOptions = defaultPublicationOptions;
        }

        /// <summary>
        /// Creates a child <see cref="HierarchicalEventBroker"/>.
        /// </summary>
        /// <returns>The created child <see cref="HierarchicalEventBroker"/>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The object is returned and disposed on the Dispose(bool) method of this instance.")]
        public HierarchicalEventBroker CreateChild()
        {
            return this.CreateChild(this.defaultPublicationOptions);
        }

        /// <summary>
        /// Creates a child <see cref="HierarchicalEventBroker"/> overriding the current The default publication options.
        /// </summary>
        /// <param name="defaultPublicationOptions">The default publication options.</param>
        /// <returns>
        /// The created child <see cref="HierarchicalEventBroker"/>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The object is returned and disposed on the Dispose(bool) method of this instance.")]
        public HierarchicalEventBroker CreateChild(PublicationOptions defaultPublicationOptions)
        {
            HierarchicalEventBroker newChild = new HierarchicalEventBroker(this, defaultPublicationOptions, this.TraceSource);

            this.sync.EnterWriteLock();

            try
            {
                Contract.Assume(this.children != null, "children is null");

                this.children.Add(newChild);
            }
            finally
            {
                this.sync.ExitWriteLock();
            }

            return newChild;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.sync.Dispose();

                if (this.parent != null)
                {
                    Contract.Assume(this.parent.children != null, "parent.children is null");

                    this.parent.children.Remove(this);
                }

                if ((this.children != null) && (this.children.Count > 0))
                {
                    this.children.ForEach(c => c.Dispose(disposing));
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Publishes an event.
        /// </summary>
        /// <param name="eventData">The <see cref="T:EventData"/> instance containing the event data.</param>
        protected override void PublishInternal(EventData eventData)
        {
            //Contract.Requires(eventData != null, "eventData is null");

            base.PublishInternal(eventData);

            try
            {
                this.sync.EnterReadLock();

                if (((this.defaultPublicationOptions == PublicationOptions.All) || (this.defaultPublicationOptions == PublicationOptions.TopDown))
                    && ((this.children != null) && (this.children.Count > 0)))
                {
                    this.children.ForEach(c => c.PublishInternal(eventData));
                }

                if (((this.defaultPublicationOptions == PublicationOptions.All) || (this.defaultPublicationOptions == PublicationOptions.BottomUp))
                    && (this.parent != null))
                {
                    this.parent.PublishInternal(eventData);
                }
            }
            finally
            {
                this.sync.ExitReadLock();
            }
        }

        [ContractInvariantMethod]
        private void ObjectInvariant()
        {
            Contract.Invariant(this.sync != null, "sync is null");
        }
    }
}
