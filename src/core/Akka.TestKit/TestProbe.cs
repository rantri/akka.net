﻿//-----------------------------------------------------------------------
// <copyright file="TestProbe.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Util;

namespace Akka.TestKit
{
    /// <summary>
    /// TestKit-based probe which allows sending, reception and reply.
    /// Use <see cref="TestKitBase.CreateTestProbe(string)" /> inside your test 
    /// to create new instances.
    /// </summary>
    public class TestProbe : TestKitBase, INoImplicitSender, IInternalActorRef
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        /// <param name="assertions">TBD</param>
        /// <param name="testProbeName">TBD</param>
        public TestProbe(ActorSystem system, ITestKitAssertions assertions, string testProbeName=null)
            : base(assertions, system, testProbeName)
        {
        }

        /// <summary>Gets the reference of this probe.</summary>
        public IActorRef Ref { get { return TestActor; } }

        /// <summary>Gets the sender of the last message</summary>
        public IActorRef Sender { get { return LastSender; } }

        /// <summary>
        /// Send message to an actor while using the probe as the sender.
        /// Replies will be available for inspection with all of TestKit's assertion
        /// methods.
        /// </summary>
        /// <param name="actor">The actor.</param>
        /// <param name="message">The message.</param>
        public void Send(IActorRef actor, object message)
        {
            actor.Tell(message, TestActor);
        }


        /// <summary>
        /// Forwards a message to the specified actor. As sender the sender of the last message is used.
        /// </summary>
        /// <param name="actor">The actor to forward to.</param>
        /// <param name="message">The message.</param>
        public void Forward(IActorRef actor, object message)
        {
            actor.Tell(message, Sender);
        }

        /// <summary>
        /// Forwards the last received message to the specified actor as if the 
        /// <see cref="TestKitBase.LastMessage"/> was sent directly to the actor in the first place.
        /// </summary>
        /// <param name="actor">The actor to forward to.</param>
        public void Forward(IActorRef actor)
        {
            actor.Tell(LastMessage, Sender);
        }


        /// <summary>
        /// Send message to the sender of the last received message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Reply(object message)
        {
            Sender.Tell(message,TestActor);
        }


        /// <summary>
        /// N/A
        /// </summary>
        /// <param name="name">N/A</param>
        /// <exception cref="NotSupportedException">
        /// This exception is thrown since a <see cref="TestProbe"/> cannot be created from a <see cref="TestProbe"/>.
        /// </exception>
        /// <returns>N/A</returns>
        [Obsolete("Cannot create a TestProbe from a TestProbe", true)]
#pragma warning disable CS0809
        public override TestProbe CreateTestProbe(string name=null)
#pragma warning restore CS0809
        {
            throw new NotSupportedException("Cannot create a TestProbe from a TestProbe");
        }

        int IComparable<IActorRef>.CompareTo(IActorRef other)
        {
            return TestActor.CompareTo(other);
        }

        bool IEquatable<IActorRef>.Equals(IActorRef other)
        {
            return TestActor.Equals(other);
        }

        ActorPath IActorRef.Path { get { return TestActor.Path; } }

        void ICanTell.Tell(object message, IActorRef sender)
        {
            TestActor.Tell(message, sender);
        }

        ISurrogate ISurrogated.ToSurrogate(ActorSystem system)
        {
            return TestActor.ToSurrogate(system);
        }

        bool IActorRefScope.IsLocal { get { return ((IInternalActorRef) TestActor).IsLocal; } }

        IInternalActorRef IInternalActorRef.Parent { get { return ((IInternalActorRef)TestActor).Parent; } }

        IActorRefProvider IInternalActorRef.Provider { get { return ((IInternalActorRef)TestActor).Provider; } }

#pragma warning disable CS0618 // Type or member is obsolete
        bool IInternalActorRef.IsTerminated { get { return ((IInternalActorRef)TestActor).IsTerminated; } }
#pragma warning restore CS0618 // Type or member is obsolete

        IActorRef IInternalActorRef.GetChild(IReadOnlyList<string> name)
        {
            return ((IInternalActorRef)TestActor).GetChild(name);
        }

        void IInternalActorRef.Resume(Exception causedByFailure)
        {
            ((IInternalActorRef)TestActor).Resume(causedByFailure);
        }

        void IInternalActorRef.Start()
        {
            ((IInternalActorRef)TestActor).Start();
        }

        void IInternalActorRef.Stop()
        {
            ((IInternalActorRef)TestActor).Stop();
        }

        void IInternalActorRef.Restart(Exception cause)
        {
            ((IInternalActorRef)TestActor).Restart(cause);
        }

        void IInternalActorRef.Suspend()
        {
            ((IInternalActorRef)TestActor).Suspend();
        }

        /// <summary>
        /// Sends a system message to the test probe
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="sender">NOT USED.</param>
        public void SendSystemMessage(ISystemMessage message, IActorRef sender)
        {
            ((IInternalActorRef)TestActor).SendSystemMessage(message);
        }

        /// <summary>
        /// Spawns an actor as a child of this test actor, and returns the child's ActorRef.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="supervisorStrategy"></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the operation</param>
        /// <returns></returns>
        public IActorRef ChildActorOf<T>(
            string name,
            SupervisorStrategy supervisorStrategy,
            CancellationToken cancellationToken = default)
            where T : ActorBase
            => ChildActorOfAsync(Props.Create<T>(), name, supervisorStrategy, cancellationToken)
                .GetAwaiter().GetResult();
        
        /// <summary>
        /// Spawns an actor as a child of this test actor, and returns the child's ActorRef.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="supervisorStrategy"></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the operation</param>
        /// <returns></returns>
        public async Task<IActorRef> ChildActorOfAsync<T>(
            string name,
            SupervisorStrategy supervisorStrategy,
            CancellationToken cancellationToken = default)
            where T : ActorBase
            => await ChildActorOfAsync(Props.Create<T>(), name, supervisorStrategy, cancellationToken)
                ;

        
        /// <summary>
        /// Spawns an actor as a child of this test actor, and returns the child's ActorRef.
        /// </summary>
        /// <param name="supervisorStrategy"></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the operation</param>
        /// <returns></returns>
        public IActorRef ChildActorOf<T>(
            SupervisorStrategy supervisorStrategy, CancellationToken cancellationToken = default)
            where T : ActorBase
            => ChildActorOfAsync(Props.Create<T>(), supervisorStrategy, cancellationToken)
                .GetAwaiter().GetResult();
        
        /// <summary>
        /// Spawns an actor as a child of this test actor, and returns the child's ActorRef.
        /// </summary>
        /// <param name="supervisorStrategy"></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the operation</param>
        /// <returns></returns>
        public async Task<IActorRef> ChildActorOfAsync<T>(
            SupervisorStrategy supervisorStrategy, CancellationToken cancellationToken = default)
            where T : ActorBase
            => await ChildActorOfAsync(Props.Create<T>(), supervisorStrategy, cancellationToken)
                ;
        
        /// <summary>
        /// Spawns an actor as a child of this test actor, and returns the child's ActorRef.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the operation</param>
        /// <returns></returns>
        public IActorRef ChildActorOf<T>(string name, CancellationToken cancellationToken = default)
            where T : ActorBase
            => ChildActorOfAsync(Props.Create<T>(), name, cancellationToken)
                .GetAwaiter().GetResult();
        
        /// <summary>
        /// Spawns an actor as a child of this test actor, and returns the child's ActorRef.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the operation</param>
        /// <returns></returns>
        public async Task<IActorRef> ChildActorOfAsync<T>(string name, CancellationToken cancellationToken = default)
            where T : ActorBase
            => await ChildActorOfAsync(Props.Create<T>(), name, cancellationToken)
                ;
        
        /// <summary>
        /// Spawns an actor as a child of this test actor, and returns the child's ActorRef.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the operation</param>
        /// <returns></returns>
        public IActorRef ChildActorOf<T>(CancellationToken cancellationToken = default)
            where T : ActorBase
            => ChildActorOfAsync(Props.Create<T>(), cancellationToken)
                .GetAwaiter().GetResult();
        
        /// <summary>
        /// Spawns an actor as a child of this test actor, and returns the child's ActorRef.
        /// </summary>
        /// <param name="cancellationToken"><see cref="CancellationToken"/> to cancel the operation</param>
        /// <returns></returns>
        public async Task<IActorRef> ChildActorOfAsync<T>(CancellationToken cancellationToken = default)
            where T : ActorBase
            => await ChildActorOfAsync(Props.Create<T>(), cancellationToken)
                ;
        
        /// <summary>
        /// Sends a system message to the test probe
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendSystemMessage(ISystemMessage message)
        {
            ((IInternalActorRef)TestActor).SendSystemMessage(message);
        }

   
        public int CompareTo(object obj)
        {
            return TestActor.CompareTo(obj);
        }

       
        public override bool Equals(object obj)
        {
            return TestActor.Equals(obj);
        }

       
        public override int GetHashCode()
        {
            return TestActor.GetHashCode();
        }

       
        public override string ToString()
        {
            return $"TestProbe({TestActor})";
        }
    }
}
