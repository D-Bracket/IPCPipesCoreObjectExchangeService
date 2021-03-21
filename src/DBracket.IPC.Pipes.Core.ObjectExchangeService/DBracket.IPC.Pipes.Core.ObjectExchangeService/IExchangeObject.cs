using System;
using System.Collections.Generic;
using System.Text;

namespace DBracket.IPC.Pipes.Core.ObjectExchangeService
{
    /// <summary>
    /// Interface of the object whose data should be exchanged.
    /// </summary>
    public interface IExchangeObject
    {
        /// <summary>
        /// Raise to send object data to the other application
        /// </summary>
        event ObjectChangedHandler ObjectChanged;
    }
    public delegate void ObjectChangedHandler();
}