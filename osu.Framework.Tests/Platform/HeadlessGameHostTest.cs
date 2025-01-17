﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Framework.Tests.IO;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class HeadlessGameHostTest
    {
        [Test]
        public void TestGameHostExceptionDuringSetupHost()
        {
            using (var host = new ExceptionDuringSetupGameHost(nameof(TestGameHostExceptionDuringSetupHost)))
            {
                Assert.Throws<InvalidOperationException>(() => host.Run(new TestGame()));

                Assert.AreEqual(ExecutionState.Idle, host.ExecutionState);
            }
        }

        /// <summary>
        /// <see cref="GameHost.PerformExit"/> is virtual, but there are some cases where exit is mandatory.
        /// This aims to test that shutdown from an exception firing (ie. the `finally` portion of <see cref="GameHost.Run"/>)
        /// fires correctly even if the base call of <see cref="GameHost.PerformExit"/> is omitted.
        /// </summary>
        [Test]
        public void TestGameHostExceptionDuringAsynchronousChildLoad()
        {
            using (var host = new TestRunHeadlessGameHostWithOverriddenExit(nameof(TestGameHostExceptionDuringAsynchronousChildLoad)))
            {
                Assert.Throws<InvalidOperationException>(() => host.Run(new ExceptionDuringAsynchronousLoadTestGame()));

                Assert.AreEqual(ExecutionState.Stopped, host.ExecutionState);
            }
        }

        [Test]
        public void TestGameHostDisposalWhenNeverRun()
        {
            using (new TestRunHeadlessGameHost(nameof(TestGameHostDisposalWhenNeverRun), true))
            {
                // never call host.Run()
            }
        }

        [Test]
        public void TestIpc()
        {
            using (var server = new BackgroundGameHeadlessGameHost(@"server", true))
            using (var client = new HeadlessGameHost(@"client", true))
            {
                Assert.IsTrue(server.IsPrimaryInstance, @"Server wasn't able to bind");
                Assert.IsFalse(client.IsPrimaryInstance, @"Client was able to bind when it shouldn't have been able to");

                var serverChannel = new IpcChannel<Foobar>(server);
                var clientChannel = new IpcChannel<Foobar>(client);

                void waitAction()
                {
                    using (var received = new ManualResetEventSlim(false))
                    {
                        serverChannel.MessageReceived += message =>
                        {
                            Assert.AreEqual("example", message.Bar);
                            // ReSharper disable once AccessToDisposedClosure
                            received.Set();
                        };

                        clientChannel.SendMessageAsync(new Foobar { Bar = "example" }).Wait();

                        received.Wait();
                    }
                }

                Assert.IsTrue(Task.Run(waitAction).Wait(10000), @"Message was not received in a timely fashion");
            }
        }

        private class Foobar
        {
            public string Bar;
        }

        public class ExceptionDuringSetupGameHost : TestRunHeadlessGameHost
        {
            public ExceptionDuringSetupGameHost(string gameName)
                : base(gameName)
            {
            }

            protected override void SetupForRun()
            {
                base.SetupForRun();
                throw new InvalidOperationException();
            }
        }

        public class TestRunHeadlessGameHostWithOverriddenExit : TestRunHeadlessGameHost
        {
            public TestRunHeadlessGameHostWithOverriddenExit(string gameName)
                : base(gameName)
            {
            }

            protected override void PerformExit(bool immediately)
            {
                // matches TestGameHost behaviour for testing purposes
            }
        }

        internal class ExceptionDuringAsynchronousLoadTestGame : TestGame
        {
            [BackgroundDependencyLoader]
            private void load()
            {
                throw new InvalidOperationException();
            }
        }
    }
}
