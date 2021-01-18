using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;

namespace GerberVS
{
    /// <summary>
    /// GerberVS.GerberDLLException class.
    /// </summary>
    [Serializable]
    public class GerberDLLException : System.Exception
    {
        private static string baseMessage = "GerberVS DLL error.";
        /// <summary>
        /// Initialses a new instance of GerberVS.GerberDLLException class.
        /// </summary>
        public GerberDLLException()
            : base()
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.GerberDLLException class with a specified error message.
        /// </summary>
        /// <param name="message">error message.</param>
        public GerberDLLException(string message)
            : base(baseMessage + Environment.NewLine + message)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.GerberDLLException class with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">inner exception.</param>
        public GerberDLLException(string message, Exception innerException)
            : base(baseMessage + Environment.NewLine + message, innerException)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.GerberDLLException class with a specified serialization information and streaming context.
        /// </summary>
        /// <param name="info">serialization info.</param>
        /// <param name="context">streaming context.</param>
        protected GerberDLLException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    /// <summary>
    /// GerberVS.GerberFileException class.
    /// </summary>
    [Serializable]
    public class GerberFileException : System.Exception
    {
        private static string baseMessage = "File process error.";
        /// <summary>
        /// Initialses a new instance of GerberVS.GerberFileException class.
        /// </summary>
        public GerberFileException()
            : base(baseMessage)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.GerberFileException class with a specified error message.
        /// </summary>
        /// <param name="message">exception message</param>
        public GerberFileException(string message)
            : base(baseMessage + Environment.NewLine + message)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.GerberFileException class with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">inner exception</param>
        public GerberFileException(string message, Exception innerException)
            : base(baseMessage + Environment.NewLine + message, innerException)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.GerberFileException class with a specified serialization information and streaming context.
        /// </summary>
        /// <param name="info">serialization info.</param>
        /// <param name="context">streaming context.</param>
        protected GerberFileException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    /// <summary>
    /// GerberVS.GerberApertureException class.
    /// </summary>
    [Serializable]
    public class GerberApertureException : System.Exception
    {
        private static string baseMessage = "Aperture undefined.";
        /// <summary>
        /// Initialses a new instance of GerberVS.GerberApertureException class.
        /// </summary>
        public GerberApertureException()
            : base(baseMessage)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.GerberApertureException class with a specified error message.
        /// </summary>
        /// <param name="message">exception message</param>
        public GerberApertureException(string message)
            : base(baseMessage + Environment.NewLine + message)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.GerberApertureException class with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">inner exception</param>
        public GerberApertureException(string message, Exception innerException)
            : base(baseMessage + Environment.NewLine + message, innerException)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.GerberApertureException class with a specified serialization information and streaming context.
        /// </summary>
        /// <param name="info">serialization info.</param>
        /// <param name="context">streaming context.</param>
        protected GerberApertureException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    // <summary>
    /// GerberVS.GerberExportException class.
    /// </summary>
    [Serializable]
    public class GerberExportException : System.Exception
    {
        private static string baseMessage = "File expoert failed.";
        /// <summary>
        /// Initialses a new instance of GerberVS.GerberExportException class.
        /// </summary>
        public GerberExportException()
            : base(baseMessage)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.GerberExportException class with a specified error message.
        /// </summary>
        /// <param name="message">exception message</param>
        public GerberExportException(string message)
            : base(baseMessage + Environment.NewLine + message)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.GerberExportException class with a specified error message and a reference to the inner exception.
        /// </summary>
        /// <param name="message">error message</param>
        /// <param name="innerException">inner exception</param>
        public GerberExportException(string message, Exception innerException)
            : base(baseMessage + Environment.NewLine + message, innerException)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.GerberExportException class with a specified serialization information and streaming context.
        /// </summary>
        /// <param name="info">serialization info.</param>
        /// <param name="context">streaming context.</param>
        protected GerberExportException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }


    [Serializable]
    public class MacroStackOverflowException : System.Exception
    {
        private static string baseMessage = "Macro stack error.";
        /// <summary>
        /// Initialses a new instance of GerberVS.MacroStackOverflowException class.
        /// </summary>
        public MacroStackOverflowException()
            : base(baseMessage)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.MacroStackOverflowException class with a specified error message.
        /// </summary>
        /// <param name="message">exception message</param>
        public MacroStackOverflowException(string message)
            : base(baseMessage + Environment.NewLine + message)
        { }
    }

    [Serializable]
    public class GerberImageException : System.Exception
    {
        private static string baseMessage = "Gerber image error.";
        /// <summary>
        /// Initialses a new instance of GerberVS.MacroStackOverflowException class.
        /// </summary>
        public GerberImageException()
            : base(baseMessage)
        { }

        /// <summary>
        /// Initialses a new instance of GerberVS.MacroStackOverflowException class with a specified error message.
        /// </summary>
        /// <param name="message">exception message</param>
        public GerberImageException(string message)
            : base(baseMessage + Environment.NewLine + message)
        { }
    }
}
