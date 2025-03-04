﻿//-----------------------------------------------------------------------
// <copyright file="InternalTestActorRef.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2024 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Dispatch;
using Akka.Event;
using Akka.Pattern;
using Akka.Util;
using Akka.Util.Internal;

namespace Akka.TestKit.Internal
{
    /// <summary>INTERNAL
    /// This special ActorRef is exclusively for use during unit testing in a single-threaded environment. Therefore, it
    /// overrides the dispatcher to CallingThreadDispatcher and sets the receiveTimeout to None. Otherwise,
    /// it acts just like a normal ActorRef. You may retrieve a reference to the underlying actor to test internal logic.
    /// <remarks>Note! Part of internal API. Breaking changes may occur without notice. Use at own risk.</remarks>
    /// </summary>
    public class InternalTestActorRef : LocalActorRef
    {
        private static readonly AtomicCounterLong _uniqueNameNumber = new(0);

        /// <summary>INTERNAL
        /// <remarks>Note! Part of internal API. Breaking changes may occur without notice. Use at own risk.</remarks>
        /// </summary>
        /// <returns>TBD</returns>
        public static string CreateUniqueName()
        {
            var number = _uniqueNameNumber.GetAndIncrement();
            return "$$" + number.Base64Encode();
            //Looking at the scala code, this might not be obvious that the name starts with $$
            //object TestActorRef (TestActorRef.scala) contain this code:
            //    private[testkit] def randomName: String = {
            //      val l = number.getAndIncrement()
            //      "$" + akka.util.Helpers.base64(l)
            //    }
            //So it adds one $. The second is added by akka.util.Helpers.base64(l) which by default
            //creates a StringBuilder and adds adds $. Hence, 2 $$
        }
        private InternalTestActorRef(ActorSystemImpl system, Props props, MessageDispatcher dispatcher, MailboxType mailboxType, IInternalActorRef supervisor, ActorPath path)
            : base(system, props, dispatcher, mailboxType, supervisor, path)
        {
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        /// <param name="self">TBD</param>
        /// <param name="props">TBD</param>
        /// <param name="dispatcher">TBD</param>
        /// <param name="supervisor">TBD</param>
        /// <returns>TBD</returns>
        protected override ActorCell NewActorCell(ActorSystemImpl system, IInternalActorRef self, Props props, MessageDispatcher dispatcher,
            IInternalActorRef supervisor)
        {
            return new TestActorCell((ActorSystemImpl)system, self, props, dispatcher, supervisor);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        protected TestActorCell GetTestActorCell()
        {
            return (TestActorCell)Cell;
        }


        /// <summary>
        /// Directly inject messages into actor receive behavior. Any exceptions
        /// thrown will be available to you, while still being able to use
        /// become/unbecome.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="sender">The sender.</param>
        public void Receive(object message, IActorRef sender = null)
        {
            var cell = Cell;
            sender = sender.IsNobody() ? cell.System.DeadLetters : sender;
            var envelope = new Envelope(message, sender);
            cell.UseThreadContext(() => cell.ReceiveMessageForTest(envelope));
        }

        public Task ReceiveAsync(object message, IActorRef sender = null)
        {
            var cell = (TestActorCell)Cell;
            sender = sender.IsNobody() ? cell.System.DeadLetters : sender;
            var envelope = new Envelope(message, sender);
            return cell.UseThreadContextAsync(() => cell.ReceiveMessageForTestAsync(envelope));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <exception cref="IllegalActorStateException">TBD</exception>
        public object UnderlyingActor
        {
            get
            {
                if (IsTerminated)
                    throw new IllegalActorStateException("Underlying actor is terminated");
                var actor = GetTestActorCell().Actor;
                if (actor == null)
                {
                    var timeout = TestKitExtension.For(System).DefaultTimeout;
                    actor = this.Ask(InternalGetActor.Instance, timeout).Result;
                }
                return actor;
            }
        }

        /// <summary>
        /// Registers this actor to be a death monitor of the provided ActorRef
        /// This means that this actor will get a Terminated()-message when the provided actor
        /// is permanently terminated.
        /// Returns the same ActorRef that is provided to it, to allow for cleaner invocations.
        /// </summary>
        /// <param name="subject">The subject to watch.</param>
        /// <returns>Returns the same ActorRef that is provided to it, to allow for cleaner invocations.</returns>
        public void Watch(IActorRef subject)
        {
            Cell.Watch(subject);
        }

        /// <summary>
        /// Deregisters this actor from being a death monitor of the provided ActorRef
        /// This means that this actor will not get a Terminated()-message when the provided actor
        /// is permanently terminated.
        /// Returns the same ActorRef that is provided to it, to allow for cleaner invocations.
        /// </summary>
        /// <returns>Returns the same ActorRef that is provided to it, to allow for cleaner invocations.</returns>
        /// <param name="subject">The subject to unwatch.</param>
        public void Unwatch(IActorRef subject)
        {
            Cell.Unwatch(subject);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <returns>TBD</returns>
        public override string ToString()
        {
            return "TestActor[" + Path + "]";
        }

        /// <summary>
        /// INTERNAL
        /// <remarks>Note! Part of internal API. Breaking changes may occur without notice. Use at own risk.</remarks>
        /// </summary>
        /// <param name="system">TBD</param>
        /// <param name="props">TBD</param>
        /// <param name="supervisor">TBD</param>
        /// <param name="name">TBD</param>
        /// <exception cref="IllegalStateException">TBD</exception>
        /// <returns>TBD</returns>
        public static InternalTestActorRef Create(ActorSystem system, Props props, IActorRef supervisor = null, string name = null)
        {
            if (name == null)
                name = CreateUniqueName();

            if (supervisor == null)
            {
                var systemImpl = (ActorSystemImpl)system;
                supervisor = systemImpl.Guardian;
            }


            if (props.Deploy.Dispatcher == Deploy.NoDispatcherGiven)
            {
                props = props.WithDispatcher(CallingThreadDispatcher.Id);
            }

            var dispatcher = system.Dispatchers.Lookup(props.Deploy.Dispatcher);

            if (supervisor is LocalActorRef supervisorLocal)
            {
                supervisorLocal.Cell.ReserveChild(name);
            }
            else
            {
                if (supervisor is RepointableActorRef supervisorRep)
                {
                    var repUnderlying = supervisorRep.Underlying;
                    if (repUnderlying is UnstartedCell)
                        throw new IllegalStateException("Cannot attach a TestActor to an unstarted top-level actor, ensure that it is started by sending a message and observing the reply");
                    if (repUnderlying is ActorCell cellUnderlying)
                    {
                        cellUnderlying.ReserveChild(name);
                    }
                    else
                    {
                        system.Log.Error("Trying to attach child {0} to unknown type of supervisor cell {1}, this is not going to end well", name, repUnderlying.GetType());
                    }
                }
            }

            var mailbox = system.Mailboxes.GetMailboxType(props, dispatcher.Configurator.Config);
            var testActorRef = new InternalTestActorRef((ActorSystemImpl)system, props, dispatcher, mailbox, (IInternalActorRef)supervisor, supervisor.Path / name);

            // we need to start ourselves since the creation of an actor has been split into initialization and starting
            testActorRef.Underlying.Start();
            return testActorRef;
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected class TestActorCell : ActorCell
        {
            private TestActorTaskScheduler _taskScheduler;
            
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="system">TBD</param>
            /// <param name="self">TBD</param>
            /// <param name="props">TBD</param>
            /// <param name="dispatcher">TBD</param>
            /// <param name="parent">TBD</param>
            public TestActorCell(ActorSystemImpl system, IInternalActorRef self, Props props, MessageDispatcher dispatcher, IInternalActorRef parent)
                : base(system, self, props, dispatcher, parent)
            {
            }

            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="envelope">TBD</param>
            protected internal override void AutoReceiveMessage(Envelope envelope)
            {
                if (envelope.Message is InternalGetActor)
                    Sender.Tell(Actor, Self);
                else
                    base.AutoReceiveMessage(envelope);
            }

            /// <inheritdoc />
            public override ActorTaskScheduler TaskScheduler
            {
                get
                {
                    var taskScheduler = Volatile.Read(ref _taskScheduler);

                    if (taskScheduler != null)
                        return taskScheduler;

                    taskScheduler = new TestActorTaskScheduler(this, TaskFailureHook);
                    return Interlocked.CompareExchange(ref _taskScheduler, taskScheduler, null) ?? taskScheduler;
                }
            }


            private readonly Dictionary<object, TaskCompletionSource<Done>> _testActorTasks = new();
        
            /// <summary>
            /// This is only intended to be called from TestKit's TestActorRef
            /// </summary>
            /// <param name="envelope">TBD</param>
            public Task ReceiveMessageForTestAsync(Envelope envelope)
            {
                var tcs = new TaskCompletionSource<Done>();
                _testActorTasks[envelope.Message] = tcs;
                ReceiveMessageForTest(envelope);
                return tcs.Task;
            }
        
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="actionAsync">TBD</param>
            public Task UseThreadContextAsync(Func<Task> actionAsync)
            {
                var tmp = InternalCurrentActorCellKeeper.Current;
                InternalCurrentActorCellKeeper.Current = this;
                try
                {
                    return actionAsync();
                }
                finally
                {
                    //ensure we set back the old context
                    InternalCurrentActorCellKeeper.Current = tmp;
                }
            }

            private void TaskFailureHook(object message, Exception exception)
            {
                if (!_testActorTasks.TryGetValue(message, out var tcs)) 
                    return;
                if (exception is { })
                    tcs.TrySetException(exception);
                else
                    tcs.TrySetResult(Done.Instance);
                _testActorTasks.Remove(message);
            }

            /// <summary>
            /// TBD
            /// </summary>
            public new object Actor { get { return base.Actor; } }
        }

        internal class TestActorTaskScheduler : ActorTaskScheduler, IAsyncResultInterceptor
        {
            private readonly TestActorCell _testActorCell;
            private readonly Action<object, Exception> _taskCallback;

            /// <inheritdoc />
            internal TestActorTaskScheduler(ActorCell testActorCell, Action<object, Exception> taskCallback) : base(testActorCell)
            {
                _taskCallback = taskCallback;
                _testActorCell = (TestActorCell) testActorCell;
            }

            /// <inheritdoc />
            protected override void OnBeforeTaskStarted()
            {
                ActorCellKeepingSynchronizationContext.AsyncCache = _testActorCell;
            }

            /// <inheritdoc />
            protected override void OnAfterTaskCompleted()
            {
                ActorCellKeepingSynchronizationContext.AsyncCache = null;
            }

            public void OnTaskCompleted(object message, Exception exception)
            {
                _taskCallback(message, exception);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        public class InternalGetActor : IAutoReceivedMessage, IPossiblyHarmful
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly InternalGetActor Instance = new();
            private InternalGetActor() { }
        }
    }
}
