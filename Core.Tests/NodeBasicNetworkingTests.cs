﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Joins;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using MOUSE.Core;
using NUnit.Framework;
using Protocol.Generated;
using FluentAssertions;
using RakNetWrapper;
using TestDomain;
using Autofac.Integration.Mef;
using System.Reactive;
using System.Reactive.Linq;

namespace Core.Tests
{
    [TestFixture]
    public class NodeBasicNetworkingTests
    {
        private IContainer container;

        [SetUp]
        public void Init()
        {
            var builder = new ContainerBuilder();
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(Node))));
            builder.RegisterType<MOUSE.Core.MessageFactory>().As<IMessageFactory>();
            builder.RegisterType<RakPeerInterface>().As<INetPeer>();
            builder.RegisterType<Node>().As<INode>();
            container = builder.Build();
        }

        [Test]
        public void NodeShouldBeAbleToConnectToOtherNode()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
            var node1 = container.Resolve<INode>();
            var node2 = container.Resolve<INode>();

            node1.Start(true, endpoint);
            node2.Start(true);

            NodeProxy node1ProxyInNode2 = null;
            NodeProxy node2ProxyInNode1 = null;
            int node1OnConnectCalls = 0;
            int node2OnConnectCalls = 0;
            
            node1.OnNodeConnected.Subscribe((proxy) =>
                                                {
                                                    node2ProxyInNode1 = proxy;
                                                    node1OnConnectCalls++;
                                                });

            node2.OnNodeConnected.Subscribe((proxy) =>
                                                {
                                                    node1ProxyInNode2 = proxy;
                                                    node2OnConnectCalls++;
                                                });


            Task<NodeProxy> connectTask = node2.Connect(endpoint);

            connectTask.IsCompleted.Should().BeFalse();

            Stopwatch timer = Stopwatch.StartNew();
            while ((node1ProxyInNode2 == null || node2ProxyInNode1 == null)
                  && timer.Elapsed < TimeSpan.FromSeconds(3))
            {
                node1.Update();
                node2.Update();
            }

            node1.Stop();
            node2.Stop();

            node1OnConnectCalls.Should().Be(1);
            node2OnConnectCalls.Should().Be(1);

            node1ProxyInNode2.Should().NotBeNull();
            node2ProxyInNode1.Should().NotBeNull();

            connectTask.IsCompleted.Should().BeTrue();
            connectTask.Result.Should().Be(node1ProxyInNode2);
        }


        [Test]
        public void ShouldImmediatelyReturnSameProxyIfAlreadyConnected()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
            var node1 = container.Resolve<INode>();
            var node2 = container.Resolve<INode>();

            node1.Start(true, endpoint);
            node2.Start(true);

            NodeProxy node1ProxyInNode2 = null;
            NodeProxy node2ProxyInNode1 = null;

            node1.OnNodeConnected.Subscribe((proxy) => node2ProxyInNode1 = proxy);
            node2.OnNodeConnected.Subscribe((proxy) => node1ProxyInNode2 = proxy);

            Task<NodeProxy> connectTask = node2.Connect(endpoint);

            Stopwatch timer = Stopwatch.StartNew();
            while ((node1ProxyInNode2 == null || node2ProxyInNode1 == null)
                  && timer.Elapsed < TimeSpan.FromSeconds(5))
            {
                node1.Update();
                node2.Update();
            }

            //Assert
            Task<NodeProxy> connectTask2 = node2.Connect(endpoint);

            connectTask2.IsCompleted.Should().BeTrue();
            connectTask2.Result.Should().Be(node1ProxyInNode2);

            node1.Stop();
            node2.Stop();

            
        }

        [Test]
        public void ShouldNotifyOnDisconnect()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
            var node1 = container.Resolve<INode>();
            var node2 = container.Resolve<INode>();

            node1.Start(true, endpoint);
            node2.Start(true);

            NodeProxy node1ProxyInNode2 = null;
            NodeProxy node2ProxyInNode1 = null;

            NodeProxy disconectedProxy = null;
            int disconnectCalls = 0;

            node1.OnNodeConnected.Subscribe((proxy) => node2ProxyInNode1 = proxy);
            node2.OnNodeConnected.Subscribe((proxy) => node1ProxyInNode2 = proxy);

            node2.OnNodeDisconnected.Subscribe((proxy) =>
                                               {
                                                   disconectedProxy = proxy;
                                                   disconnectCalls++;   
                                               });

            Task<NodeProxy> connectTask = node2.Connect(endpoint);

            Stopwatch timer = Stopwatch.StartNew();
            while ((node1ProxyInNode2 == null || node2ProxyInNode1 == null)
                  && timer.Elapsed < TimeSpan.FromSeconds(3))
            {
                node1.Update();
                node2.Update();
            }

            node1.Stop();

            timer = Stopwatch.StartNew();
            while (disconnectCalls == 0 && timer.Elapsed < TimeSpan.FromSeconds(3))
                node2.Update();

            disconectedProxy.Should().Be(node1ProxyInNode2);
            disconnectCalls.Should().Be(1);
            node2.Stop();
        }
    }
}