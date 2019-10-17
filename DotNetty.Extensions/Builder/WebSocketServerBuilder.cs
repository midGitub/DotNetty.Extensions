﻿using DotNetty.Codecs.Http;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System.Threading.Tasks;

namespace DotNetty.Extensions
{
    class WebSocketServerBuilder :
        BaseGenericServerBuilder<IWebSocketServerBuilder, IWebSocketServer, IWebSocketConnection, string>,
        IWebSocketServerBuilder
    {
        public WebSocketServerBuilder(int port, string path, int idle)
            : base(port, idle)
        {
            _path = path;
        }
        private string _path { get; }
        public async override Task<IWebSocketServer> BuildAsync()
        {
            WebSocketServer tcpServer = new WebSocketServer(_port, _path, _event);

            var serverChannel = await new ServerBootstrap()
                .Group(new MultithreadEventLoopGroup(), new MultithreadEventLoopGroup())
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 8192)
                .Option(ChannelOption.TcpNodelay, true)
                .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    if (_idle != 0)
                    {
                        pipeline.AddLast(new IdleStateHandler(_idle, 0, 0));
                    }
                    pipeline.AddLast(new HttpServerCodec());
                    pipeline.AddLast(new HttpObjectAggregator(65536));
                    pipeline.AddLast(new CommonChannelHandler(tcpServer));
                })).BindAsync(_port);
            _event.OnServerStarted?.Invoke(tcpServer);
            tcpServer.SetChannel(serverChannel);

            return await Task.FromResult(tcpServer);
        }
    }
}