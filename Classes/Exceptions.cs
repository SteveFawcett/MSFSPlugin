using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSFSPlugin.Classes
{
    /// <summary>  
        /// Exception thrown when an invalid request for simulation data is made.  
        /// </summary>  
    public class InvalidSimDataRequestException : Exception
        {
            /// <summary>  
            /// Initializes a new instance of the <see cref="InvalidSimDataRequestException"/> class.  
            /// </summary>  
            public InvalidSimDataRequestException()
            {
            }

            /// <summary>  
            /// Initializes a new instance of the <see cref="InvalidSimDataRequestException"/> class with a specified error message.  
            /// </summary>  
            /// <param name="message">The message that describes the error.</param>  
            public InvalidSimDataRequestException(string message)
                : base(message)
            {
            }

            /// <summary>  
            /// Initializes a new instance of the <see cref="InvalidSimDataRequestException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.  
            /// </summary>  
            /// <param name="message">The message that describes the error.</param>  
            /// <param name="innerException">The exception that is the cause of the current exception.</param>  
            public InvalidSimDataRequestException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

    /// <summary>
    /// Exception thrown when the simulator is not connected.
    /// </summary>
    public class SimulatorNotConnectedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatorNotConnectedException"/> class.
        /// </summary>
        public SimulatorNotConnectedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatorNotConnectedException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SimulatorNotConnectedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatorNotConnectedException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public SimulatorNotConnectedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
