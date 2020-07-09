﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Transport.Libuv
{
    using System;
    using System.Threading.Tasks;
    using DotNetty.Common.Concurrency;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Libuv.Native;

    public sealed class EventLoopGroup : MultithreadEventLoopGroup<EventLoopGroup, EventLoop>
    {
        private static readonly int DefaultEventLoopCount;
        private static readonly Func<EventLoopGroup, EventLoop> DefaultEventLoopFactory;

        static EventLoopGroup()
        {
            DefaultEventLoopCount = Environment.ProcessorCount;
            DefaultEventLoopFactory = group => new EventLoop(group);
        }


        public EventLoopGroup()
            : this(0)
        {
        }

        public EventLoopGroup(int nThreads)
            : base(0u >= (uint)nThreads ? DefaultEventLoopCount : nThreads, EventLoopChooserFactory<EventLoop>.Instance, DefaultEventLoopFactory)
        {
        }

        public EventLoopGroup(int nThreads, TimeSpan breakoutInterval)
            : this(nThreads, DefaultThreadFactory<EventLoop>.Instance, RejectedExecutionHandlers.Reject(), breakoutInterval)
        {
        }

        public EventLoopGroup(int nThreads, IRejectedExecutionHandler rejectedHandler)
            : this(nThreads, rejectedHandler, LoopExecutor.DefaultBreakoutInterval)
        {
        }

        public EventLoopGroup(int nThreads, IRejectedExecutionHandler rejectedHandler, TimeSpan breakoutInterval)
            : this(nThreads, DefaultThreadFactory<EventLoop>.Instance, rejectedHandler, breakoutInterval)
        {
        }

        public EventLoopGroup(int nThreads, IThreadFactory threadFactory, TimeSpan breakoutInterval)
            : this(nThreads, threadFactory, RejectedExecutionHandlers.Reject(), breakoutInterval)
        {
        }

        public EventLoopGroup(int nThreads, IThreadFactory threadFactory, IRejectedExecutionHandler rejectedHandler, TimeSpan breakoutInterval)
            : base(0u >= (uint)nThreads ? DefaultEventLoopCount : nThreads,
                  EventLoopChooserFactory<EventLoop>.Instance,
                  group => new EventLoop(group, threadFactory, rejectedHandler, breakoutInterval))
        {
        }

        public override Task RegisterAsync(IChannel channel)
        {
            var nativeChannel = channel as INativeChannel;
            if (nativeChannel is null)
            {
                ThrowHelper.ThrowArgumentException_RegChannel();
            }

            // The handle loop must be the same as the loop of the
            // handle was created from.
            NativeHandle handle = nativeChannel.GetHandle();
            IntPtr loopHandle = handle.LoopHandle();
            var eventLoops = GetItems();
            for (int i = 0; i < eventLoops.Count; i++)
            {
                var eventLoop = eventLoops[i];
                if (eventLoop.UnsafeLoop.Handle == loopHandle)
                {
                    return eventLoop.RegisterAsync(nativeChannel);
                }
            }

            return ThrowHelper.ThrowInvalidOperationException(loopHandle);
        }
    }
}