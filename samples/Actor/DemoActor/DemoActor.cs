// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// ------------------------------------------------------------

namespace DaprDemoTwin
{
    using System;
    using System.Threading.Tasks;
    using Dapr.Actors;
    using Dapr.Actors.Runtime;
    using Dapr.DigitalTwins;
    using IDemoTwinInterface;

    public class DemoTwin : DigitalTwin, IDemoTwin, IRemindable
    {
        public DemoTwin(TwinId twinId)
            : base(twinId)
        {
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override Task OnActivateAsync()
        {
            var children = this.GetChildren();
            ...
            
            return Task.CompletedTask;
        }
    }
}
