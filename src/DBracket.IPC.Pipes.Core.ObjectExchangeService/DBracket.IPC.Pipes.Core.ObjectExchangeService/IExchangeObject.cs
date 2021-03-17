using System;
using System.Collections.Generic;
using System.Text;

namespace DBracket.IPC.Pipes.Core.PairedObjectExchange
{
    /// <summary>
    /// Interface of the object whose data should be exchanged.
    /// </summary>
    public interface IExchangeObject
    {
        /// <summary>
        /// Name of the object. Used to identify, when multiple objects are added to the Exchangehandler
        /// </summary>
        string objectName { get; set; }

        /// <summary>
        /// Raise to send object data to the other application
        /// </summary>
        event ObjectChangedHandler ObjectChanged;
    }
}

public delegate void ObjectChangedHandler(string objectName);
