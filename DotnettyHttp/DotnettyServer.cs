﻿using dotnet_etcd;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Codecs.Http;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotnettyHttp
{
    public class DotnettyServer:IDisposable
    {
        private IEventLoopGroup group, workGroup;
        private ServerBootstrap serverBootstrap;
        public IChannel BootstrapChannel { get; private set; }

        public static EtcdClient EtcdClient { get; private set; }


        private static DotnettyServer httpServer;
        private static readonly object _readoLook = new object();


        private DotnettyServer(string etcdHost, int etcdPort)
        {
            EtcdClient = new EtcdClient(etcdHost, etcdPort);
            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => level==Microsoft.Extensions.Logging.LogLevel.Information, false));
            var dispatcher = new DispatcherEventLoopGroup();
            group = dispatcher;
            workGroup = new WorkerEventLoopGroup(dispatcher);
            serverBootstrap = new ServerBootstrap()
                            .Group(group, workGroup)
                            .Channel<TcpServerChannel>()//TcpServerSocketChannel
                            .Option(ChannelOption.SoBacklog, 8192)
                            .Handler(new LoggingHandler(LogLevel.INFO))
                            .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>//ActionChannelInitializer
                            {
                                IChannelPipeline pipeline = channel.Pipeline;

                                //pipeline.AddLast(new DelimiterBasedFrameDecoder(8192,new[]{
                                //    Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes("&sup;")),
                                //    Unpooled.WrappedBuffer(new[] { (byte)'\r', (byte)'\n' }),
                                //    Unpooled.WrappedBuffer(new[] { (byte)'\n' }),
                                //}));
                                pipeline.AddLast(new DynamicHandler(Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes("&sup;"))));
                                //pipeline.AddLast(new TestHandler());
                                ////pipeline.AddLast(new HttpRequestDecoder(4096, 8192, 8192, false));
                                //pipeline.AddLast(new HttpResponseEncoder());
                                //pipeline.AddLast(new StringEncoder());
                                ////pipeline.AddLast(new StringDecoder());
                                //pipeline.AddLast(new HttpObjectAggregator(1048576));

                                ////pipeline.AddLast(new HttpContentCompressor());//压缩
                                //pipeline.AddLast(new HttpServerHandler(etcdClient));
                            }));
        }
        /// <summary>
        /// 初始化当前对象
        /// </summary>
        /// <returns></returns>
        public static DotnettyServer InitializeCreate(string etcdHost, int etcdPort)
        {
            if (httpServer==null)
            {
                lock (_readoLook)
                {
                    if (httpServer == null)
                    {
                        httpServer =new DotnettyServer(etcdHost, etcdPort);
                    }
                }
            }
            return httpServer;
        }
        public DotnettyServer RunServer(int inetPort)
        {
            httpServer.BootstrapChannel = httpServer.serverBootstrap.BindAsync(IPAddress.IPv6Any, inetPort).Result;
            return httpServer;
        }
        public void Dispose()
        {
            workGroup.ShutdownGracefullyAsync();
            group.ShutdownGracefullyAsync();
        }
    }
}
