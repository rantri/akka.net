﻿//-----------------------------------------------------------------------
// <copyright file="Debug.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Akka.Event
{
    /// <summary>
    /// This class represents a Debug log event.
    /// </summary>
    public class Debug : LogEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Debug" /> class.
        /// </summary>
        /// <param name="logSource">The source that generated the log event.</param>
        /// <param name="logClass">The type of logger used to log the event.</param>
        /// <param name="message">The message that is being logged.</param>
        public Debug(string logSource, Type logClass, object message) 
            : this(null, logSource, logClass, message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Debug" /> class.
        /// </summary>
        /// <param name="cause">The exception that generated the log event.</param>
        /// <param name="logSource">The source that generated the log event.</param>
        /// <param name="logClass">The type of logger used to log the event.</param>
        /// <param name="message">The message that is being logged.</param>
        public Debug(Exception cause, string logSource, Type logClass, object message)
        {
            LogSource = logSource;
            LogClass = logClass;
            Message = message;
            Cause = cause;
        }

        /// <summary>
        /// Retrieves the <see cref="Akka.Event.LogLevel" /> used to classify this event.
        /// </summary>
        /// <returns>
        /// The <see cref="Akka.Event.LogLevel" /> used to classify this event.
        /// </returns>
        public override LogLevel LogLevel()
        {
            return Event.LogLevel.DebugLevel;
        }
    }
}
