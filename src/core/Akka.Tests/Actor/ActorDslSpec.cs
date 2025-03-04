﻿//-----------------------------------------------------------------------
// <copyright file="ActorDslSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.TestKit;
using Xunit;

namespace Akka.Tests.Actor
{
    public class ActorDslSpec : AkkaSpec
    {
        [Fact]
        public async Task A_lightweight_creator_must_support_creating_regular_actors()
        {
            var a = Sys.ActorOf(Props.Create(() => new Act(c =>
                c.Receive<string>(msg => msg == "hello", (msg, ctx) => TestActor.Tell("hi")))));

            a.Tell("hello");
            await ExpectMsgAsync("hi");
        }

        [Fact]
        public async Task A_lightweight_creator_must_support_become_stacked()
        {
            var a = Sys.ActorOf(c => c.Become((msg, _) =>
            {
                var message = msg as string;
                if (message == null) return;

                if (message == "info")
                    TestActor.Tell("A");
                else if (message == "switch")
                    c.BecomeStacked((msg2, _) =>
                    {
                        var message2 = msg2 as string;
                        if (message2 == null) return;
                        
                        if (message2 == "info")
                            TestActor.Tell("B");
                        else if (message2 == "switch")
                            c.UnbecomeStacked();
                    });
                else if (message == "lobotomize")
                    c.UnbecomeStacked();
            }));

            a.Tell("info");
            await ExpectMsgAsync("A");

            a.Tell("switch");
            a.Tell("info");
            await ExpectMsgAsync("B");

            a.Tell("switch");
            a.Tell("info");
            await ExpectMsgAsync("A");
        }

        [Fact]
        public async Task A_lightweight_creator_must_support_actor_setup_and_teardown()
        {
            const string started = "started";
            const string stopped = "stopped";

            var a = Sys.ActorOf(c =>
            {
                c.OnPreStart = _ => TestActor.Tell(started);
                c.OnPostStop = _ => TestActor.Tell(stopped);
            });

            Sys.Stop(a);
            await ExpectMsgAsync(started);
            await ExpectMsgAsync(stopped);
        }

        [Fact(Skip = "TODO: requires event filters")]
        public void A_lightweight_creator_must_support_restart()
        {
            //TODO: requires event filters
        }

        [Fact(Skip = "TODO: requires event filters")]
        public void A_lightweight_creator_must_support_supervising()
        {
            //TODO: requires event filters
        }

        [Fact]
        public async Task A_lightweight_creator_must_support_nested_declarations()
        {
            var a = Sys.ActorOf(act =>
            {
                var b = act.ActorOf(act2 =>
                {
                    act2.OnPreStart = context => context.Parent.Tell("hello from " + context.Self.Path);
                }, "barney");
                act.ReceiveAny((x, _) => TestActor.Tell(x));
            }, "fred");

            await ExpectMsgAsync("hello from akka://" + Sys.Name + "/user/fred/barney");
            LastSender.ShouldBe(a);
        }

        [Fact(Skip = "TODO: requires proven and tested stash implementation")]
        public void A_lightweight_creator_must_support_stash()
        {
            //TODO: requires proven and tested stash implementation
        }

        [Fact]
        public async Task A_lightweight_creator_must_support_actor_base_method_calls()
        {
            var parent = Sys.ActorOf(act =>
            {
                var child = act.ActorOf(act2 =>
                {
                    act2.OnPostStop = _ => TestActor.Tell("stopping child");
                    act2.Receive("ping", (_, _) => TestActor.Tell("pong"));
                }, "child");
                act.OnPreRestart = (exc, msg, _) =>
                {
                    TestActor.Tell("restarting parent");
                    act.DefaultPreRestart(exc, msg);    //Will stop the children
                };
                act.Receive("crash",(_,_)=>{throw new Exception("Received <crash>");});
                act.ReceiveAny((x, _) => child.Tell(x));
            }, "parent");
            
            parent.Tell("ping");
            await ExpectMsgAsync("pong");

            parent.Tell("crash");
            await ExpectMsgAsync("restarting parent");
            await ExpectMsgAsync("stopping child");
        }

        [Fact]
        public async Task A_lightweight_creator_must_support_async_receives()
        {
            var parent = Sys.ActorOf(act =>
            {
                var completedTask = Task.FromResult(true);
                var child = act.ActorOf(act2 =>
                {
                    act2.ReceiveAsync<string>(m => m == "ping", (_, _) =>
                    {
                        TestActor.Tell("pong");
                        return completedTask;
                    });

                    act2.ReceiveAsync<string>((_, _) =>
                    {
                        TestActor.Tell("ping");
                        return completedTask;
                    }, msg => msg == "pong");

                    act2.ReceiveAsync<string>((_, _) =>
                    {
                        TestActor.Tell("hello");
                        return completedTask;
                    });
                });

                act.ReceiveAnyAsync((msg, _) => 
                {
                    child.Tell(msg);
                    return Task.FromResult(true);
                });
            });

            parent.Tell("ping");
            await ExpectMsgAsync("pong");

            parent.Tell("pong");
            await ExpectMsgAsync("ping");

            parent.Tell("hi");
            await ExpectMsgAsync("hello");
        }
    }
}

