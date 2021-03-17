using DBracket.IPC.Pipes.Core.PairedObjectExchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBracket.IPC.Pipes.Core.ObjectExchangeService.Example
{
    class DataContainer : IExchangeObject
    {
        public DataContainer()
        {
            objectName = "object1";
        }

        /// <summary>
        /// Example string to exchange
        /// </summary>
        public string TestDataString
        {
            get
            {
                return _TestDataString;
            }
            set
            {
                _TestDataString = value;
                ObjectChanged?.Invoke(objectName);
            }
        }
        private string _TestDataString;

        /// <summary>
        /// Example int to exchange
        /// </summary>
        public int TestDataInt
        {
            get
            {
                return _TestDataInt;
            }
            set
            {
                _TestDataInt = value;
                ObjectChanged?.Invoke(objectName);
            }
        }
        private int _TestDataInt;

        /// <summary>
        /// Name of the object. Used to identify, when multiple objects are added to the Exchangehandler
        /// </summary>
        public string objectName { get; set; }

        /// <summary>
        /// Raise to send object data to the other application
        /// </summary>
        public event ObjectChangedHandler ObjectChanged;
    }
}
