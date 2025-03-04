﻿//-----------------------------------------------------------------------
// <copyright file="RepointableActorRefSpecs.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using Xunit;

namespace Akka.Tests.Actor
{
    public class RepointableActorRefSpecs : AkkaSpec
    {
        public class Bug2182Actor : ReceiveActor, IWithUnboundedStash
        {
            public Bug2182Actor()
            {
                Receive<string>(str => str.Equals("init"), _ => Become(Initialize));
                ReceiveAny(_ => Stash.Stash());
                Self.Tell("init");
            }

            private void Initialize()
            {
                Self.Tell("init2");
                Receive<string>(str => str.Equals("init2"), _ =>
                {
                    Become(Set);
                    Stash.UnstashAll();
                });
                ReceiveAny(_ => Stash.Stash());
            }

            private void Set()
            {
                ReceiveAny(o => Sender.Tell(o));
            }

            public IStash Stash { get; set; }
        }

        /// <summary>
        /// Fixes https://github.com/akkadotnet/akka.net/pull/2182
        /// </summary>
        [Fact]
        public async Task Fix2128_RepointableActorRef_multiple_enumerations()
        {
            var actor = Sys.ActorOf(Props.Create(() => new Bug2182Actor()).WithDispatcher("akka.test.calling-thread-dispatcher"), "buggy");
            actor.Tell("foo");
            await ExpectMsgAsync("foo");
        }
    }
}

