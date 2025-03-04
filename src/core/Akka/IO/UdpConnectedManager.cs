﻿//-----------------------------------------------------------------------
// <copyright file="UdpConnectedManager.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Annotations;

namespace Akka.IO
{
    using ByteBuffer = ArraySegment<byte>;

    /// <summary>
    /// INTERNAL API
    /// </summary>
    [InternalApi]
    class UdpConnectedManager : ActorBase
    {
        private readonly UdpConnectedExt _udpConn;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="udpConn">TBD</param>
        public UdpConnectedManager(UdpConnectedExt udpConn)
        {
            _udpConn = udpConn;
        }

        protected override bool Receive(object message)
        {
            switch (message)
            {
                case UdpConnected.Connect connect:
                    {
                        var commander = Sender; // NOTE: Aaronontheweb (9/1/2017) this should probably be the Handler...
                        Context.ActorOf(Props.Create(() => new UdpConnection(_udpConn, commander, connect)));
                        return true;
                    }
                default: throw new ArgumentException($"The supplied message type [{message.GetType()}] is invalid. Only Connect messages are supported.", nameof(message));
            }
        }

    }
}
