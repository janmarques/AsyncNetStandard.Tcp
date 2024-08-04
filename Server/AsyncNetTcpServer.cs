using AsyncNetStandard.Core.Events;
using AsyncNetStandard.Core.Extensions;
using AsyncNetStandard.Tcp.Connection;
using AsyncNetStandard.Tcp.Connection.Events;
using AsyncNetStandard.Tcp.Defragmentation;
using AsyncNetStandard.Tcp.Extensions;
using AsyncNetStandard.Tcp.Remote;
using AsyncNetStandard.Tcp.Remote.Events;
using AsyncNetStandard.Tcp.Server.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;


namespace AsyncNetStandard.Tcp.Server
{
    /// <summary>An implementation of asynchronous TCP server</summary>
    public class AsyncNetTcpServer : IAsyncNetTcpServer
    {
        private readonly ConcurrentDictionary<IPEndPoint, RemoteTcpPeer> connectedPeersDictionary = new ConcurrentDictionary<IPEndPoint, RemoteTcpPeer>();

        /// <summary>
        /// Constructs TCP server that runs on particular port and has default configuration
        /// </summary>
        /// <param name="port">A port that TCP server will run on</param>
        public AsyncNetTcpServer(int port)
          : this(new AsyncNetTcpServerConfig() { Port = port })
        {
        }

        /// <summary>Constructs TCP server with custom configuration</summary>
        /// <param name="config">TCP server configuration</param>
        public AsyncNetTcpServer(AsyncNetTcpServerConfig config)
        {
            this.Config = new AsyncNetTcpServerConfig()
            {
                ProtocolFrameDefragmenterFactory = config.ProtocolFrameDefragmenterFactory,
                ConnectionTimeout = config.ConnectionTimeout,
                MaxSendQueuePerPeerSize = config.MaxSendQueuePerPeerSize,
                IPAddress = config.IPAddress,
                Port = config.Port,
                ConfigureTcpListenerCallback = config.ConfigureTcpListenerCallback,
                UseSsl = config.UseSsl,
                X509Certificate = config.X509Certificate,
                RemoteCertificateValidationCallback = config.RemoteCertificateValidationCallback,
                EncryptionPolicy = config.EncryptionPolicy,
                ClientCertificateRequiredCallback = config.ClientCertificateRequiredCallback,
                CheckCertificateRevocationCallback = config.CheckCertificateRevocationCallback,
                EnabledProtocols = config.EnabledProtocols
            };
        }

        /// <summary>Fires when server started running</summary>
        public event EventHandler<TcpServerStartedEventArgs> ServerStarted;

        /// <summary>Fires when server stopped running</summary>
        public event EventHandler<TcpServerStoppedEventArgs> ServerStopped;

        /// <summary>
        /// Fires when TCP frame arrived from particular client/peer
        /// </summary>
        public event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

        /// <summary>Fires when there was a problem with the server</summary>
        public event EventHandler<TcpServerExceptionEventArgs> ServerExceptionOccured;

        /// <summary>
        /// Fires when there was an error while handling particular client/peer
        /// </summary>
        public event EventHandler<RemoteTcpPeerExceptionEventArgs> RemoteTcpPeerExceptionOccured;

        /// <summary>
        /// Fires when unhandled error occured - e.g. when event subscriber throws an exception
        /// </summary>
        public event EventHandler<ExceptionEventArgs> UnhandledExceptionOccured;

        /// <summary>Fires when new client/peer connects to the server</summary>
        public event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

        /// <summary>
        /// Fires when connection closes for particular client/peer
        /// </summary>
        public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

        /// <summary>A list of connected peers / client</summary>
        public IEnumerable<IRemoteTcpPeer> ConnectedPeers
        {
            get => (IEnumerable<IRemoteTcpPeer>)this.connectedPeersDictionary.Values;
        }

        /// <summary>
        /// Asynchronously starts the server that will run indefinitely
        /// </summary>
        /// <returns><see cref="T:System.Threading.Tasks.Task" /></returns>
        public virtual Task StartAsync() => this.StartAsync(CancellationToken.None);

        /// <summary>
        /// Asynchronously starts the server that will run until <paramref name="cancellationToken" /> is cancelled
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns><see cref="T:System.Threading.Tasks.Task" /></returns>
        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            AsyncNetTcpServer asyncNetTcpServer = this;
            TcpListener tcpListener = asyncNetTcpServer.CreateTcpListener();
            Action<TcpListener> listenerCallback = asyncNetTcpServer.Config.ConfigureTcpListenerCallback;
            if (listenerCallback != null)
                listenerCallback(tcpListener);
            try
            {
                tcpListener.Start();
            }
            catch (Exception ex)
            {
                TcpServerExceptionEventArgs e = new TcpServerExceptionEventArgs(ex);
                asyncNetTcpServer.OnServerExceptionOccured(e);
                return;
            }
            try
            {
                // ISSUE: reference to a compiler-generated method
                await Task.WhenAll(asyncNetTcpServer.ListenAsync(tcpListener, cancellationToken), Task.Run(new Action(() => asyncNetTcpServer.StartAsync()))).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                TcpServerExceptionEventArgs e = new TcpServerExceptionEventArgs(ex);
                asyncNetTcpServer.OnServerExceptionOccured(e);
            }
            finally
            {
                try
                {
                    tcpListener.Stop();
                }
                catch (Exception ex)
                {
                    TcpServerExceptionEventArgs e = new TcpServerExceptionEventArgs(ex);
                    asyncNetTcpServer.OnServerExceptionOccured(e);
                }
                asyncNetTcpServer.OnServerStopped(new TcpServerStoppedEventArgs());
            }
        }

        /// <summary>
        /// Sends the data asynchronously to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        /// <returns><see cref="T:System.Threading.Tasks.Task" /></returns>
        public virtual async Task BroadcastAsync(byte[] data, IEnumerable<IRemoteTcpPeer> peers)
        {
            foreach (IRemoteTcpPeer peer in peers)
            {
                int num = await peer.SendAsync(data).ConfigureAwait(false) ? 1 : 0;
            }
        }

        /// <summary>
        /// Sends the data asynchronously to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        /// <returns><see cref="T:System.Threading.Tasks.Task" /></returns>
        public virtual async Task BroadcastAsync(
          byte[] buffer,
          int offset,
          int count,
          IEnumerable<IRemoteTcpPeer> peers)
        {
            foreach (IRemoteTcpPeer peer in peers)
            {
                int num = await peer.SendAsync(buffer, offset, count).ConfigureAwait(false) ? 1 : 0;
            }
        }

        /// <summary>
        /// Sends the data asynchronously to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="T:System.Threading.Tasks.Task" /></returns>
        public virtual async Task BroadcastAsync(
          byte[] data,
          IEnumerable<IRemoteTcpPeer> peers,
          CancellationToken cancellationToken)
        {
            foreach (IRemoteTcpPeer peer in peers)
            {
                int num = await peer.SendAsync(data, cancellationToken).ConfigureAwait(false) ? 1 : 0;
            }
        }

        /// <summary>
        /// Sends the data asynchronously to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to broadcast</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="T:System.Threading.Tasks.Task" /></returns>
        public virtual async Task BroadcastAsync(
          byte[] buffer,
          int offset,
          int count,
          IEnumerable<IRemoteTcpPeer> peers,
          CancellationToken cancellationToken)
        {
            foreach (IRemoteTcpPeer peer in peers)
            {
                int num = await peer.SendAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false) ? 1 : 0;
            }
        }

        /// <summary>
        /// Adds data to the broadcast queue. It will be sent to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        /// <returns><see cref="T:System.Threading.Tasks.Task" /></returns>
        public virtual async Task AddToBroadcastQueue(byte[] data, IEnumerable<IRemoteTcpPeer> peers)
        {
            foreach (IRemoteTcpPeer peer in peers)
            {
                int num = await peer.AddToSendQueueAsync(data).ConfigureAwait(false) ? 1 : 0;
            }
        }

        /// <summary>
        /// Adds data to the broadcast queue. It will be sent to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to send</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        /// <returns><see cref="T:System.Threading.Tasks.Task" /></returns>
        public virtual async Task AddToBroadcastQueue(
          byte[] buffer,
          int offset,
          int count,
          IEnumerable<IRemoteTcpPeer> peers)
        {
            foreach (IRemoteTcpPeer peer in peers)
            {
                int num = await peer.AddToSendQueueAsync(buffer, offset, count).ConfigureAwait(false) ? 1 : 0;
            }
        }

        /// <summary>
        /// Adds data to the broadcast queue. It will be sent to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="T:System.Threading.Tasks.Task" /></returns>
        public virtual async Task AddToBroadcastQueue(
          byte[] data,
          IEnumerable<IRemoteTcpPeer> peers,
          CancellationToken cancellationToken)
        {
            foreach (IRemoteTcpPeer peer in peers)
            {
                int num = await peer.AddToSendQueueAsync(data, cancellationToken).ConfigureAwait(false) ? 1 : 0;
            }
        }

        /// <summary>
        /// Adds data to the broadcast queue. It will be sent to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to broadcast</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
        /// <returns><see cref="T:System.Threading.Tasks.Task" /></returns>
        public virtual async Task AddToBroadcastQueue(
          byte[] buffer,
          int offset,
          int count,
          IEnumerable<IRemoteTcpPeer> peers,
          CancellationToken cancellationToken)
        {
            foreach (IRemoteTcpPeer peer in peers)
            {
                int num = await peer.AddToSendQueueAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false) ? 1 : 0;
            }
        }

        /// <summary>
        /// Sends the data to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="data">Data to broadcast</param>
        /// <param name="peers">Connected peer list</param>
        public virtual void Broadcast(byte[] data, IEnumerable<IRemoteTcpPeer> peers)
        {
            foreach (IRemoteTcpPeer peer in peers)
                peer.Post(data);
        }

        /// <summary>
        /// Sends the data to all connected peers / clients from <paramref name="peers" /> list
        /// </summary>
        /// <param name="buffer">Buffer containing data to broadcast</param>
        /// <param name="offset">Data offset in <paramref name="buffer" /></param>
        /// <param name="count">Numbers of bytes to send</param>
        /// <param name="peers">Connected peer list</param>
        public virtual void Broadcast(
          byte[] buffer,
          int offset,
          int count,
          IEnumerable<IRemoteTcpPeer> peers)
        {
            foreach (IRemoteTcpPeer peer in peers)
                peer.Post(buffer, offset, count);
        }

        protected virtual AsyncNetTcpServerConfig Config { get; set; }

        protected virtual TcpListener CreateTcpListener()
        {
            return new TcpListener(new IPEndPoint(this.Config.IPAddress, this.Config.Port));
        }

        protected virtual async Task ListenAsync(TcpListener tcpListener, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    this.HandleNewTcpClientAsync(await this.AcceptTcpClient(tcpListener, token).ConfigureAwait(false), token);
                }
                catch (OperationCanceledException ex)
                {
                    break;
                }
            }
        }

        protected virtual Task<TcpClient> AcceptTcpClient(
          TcpListener tcpListener,
          CancellationToken token)
        {
            return tcpListener.AcceptTcpClientWithCancellationTokenAsync(token);
        }

        protected virtual async void HandleNewTcpClientAsync(
          TcpClient tcpClient,
          CancellationToken token)
        {
            try
            {
                await this.HandleNewTcpClientTask(tcpClient, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.OnServerExceptionOccured(new TcpServerExceptionEventArgs(ex));
            }
        }

        protected virtual async Task HandleNewTcpClientTask(
          TcpClient tcpClient,
          CancellationToken token)
        {
            using (tcpClient)
            {
                CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue = this.CreateSendQueueActionBlock(linkedCts.Token);
                SslStream sslStream = (SslStream)null;
                ConfiguredTaskAwaitable configuredTaskAwaitable;
                RemoteTcpPeer remoteTcpPeer;
                try
                {
                    if (this.Config.UseSsl && this.Config.X509Certificate != null)
                    {
                        sslStream = this.CreateSslStream(tcpClient);
                        configuredTaskAwaitable = this.AuthenticateSslStream(tcpClient, sslStream, linkedCts.Token).ConfigureAwait(false);
                        await configuredTaskAwaitable;
                        remoteTcpPeer = this.CreateRemoteTcpPeer(tcpClient, sslStream, sendQueue, linkedCts);
                    }
                    else
                        remoteTcpPeer = this.CreateRemoteTcpPeer(tcpClient, sendQueue, linkedCts);
                }
                catch (AuthenticationException ex)
                {
                    this.OnServerExceptionOccured(new TcpServerExceptionEventArgs((Exception)ex));
                    sendQueue.Complete();
                    sslStream?.Dispose();
                    linkedCts.Dispose();
                    return;
                }
                catch (Exception ex)
                {
                    this.OnServerExceptionOccured(new TcpServerExceptionEventArgs(ex));
                    sendQueue.Complete();
                    linkedCts.Dispose();
                    return;
                }
                using (remoteTcpPeer)
                {
                    this.AddRemoteTcpPeerToConnectedList(remoteTcpPeer);
                    this.OnConnectionEstablished(new ConnectionEstablishedEventArgs((IRemoteTcpPeer)remoteTcpPeer));
                    configuredTaskAwaitable = this.HandleRemotePeerAsync(remoteTcpPeer, linkedCts.Token).ConfigureAwait(false);
                    await configuredTaskAwaitable;
                    sendQueue.Complete();
                    sslStream?.Dispose();
                }
                linkedCts = (CancellationTokenSource)null;
                sendQueue = (ActionBlock<RemoteTcpPeerOutgoingMessage>)null;
                sslStream = (SslStream)null;
            }
        }

        protected virtual void AddRemoteTcpPeerToConnectedList(RemoteTcpPeer remoteTcpPeer)
        {
            this.RemoveRemoteTcpPeerFromConnectedList(remoteTcpPeer.IPEndPoint);
            this.connectedPeersDictionary.TryAdd(remoteTcpPeer.IPEndPoint, remoteTcpPeer);
        }

        protected virtual void RemoveRemoteTcpPeerFromConnectedList(IPEndPoint ipEndPoint)
        {
            this.connectedPeersDictionary.TryRemove(ipEndPoint, out RemoteTcpPeer _);
        }

        protected virtual ActionBlock<RemoteTcpPeerOutgoingMessage> CreateSendQueueActionBlock(
          CancellationToken token)
        {
            Func<RemoteTcpPeerOutgoingMessage, Task> func = new Func<RemoteTcpPeerOutgoingMessage, Task>(this.SendToRemotePeerAsync);
            ExecutionDataflowBlockOptions dataflowBlockOptions = new ExecutionDataflowBlockOptions();
            ((DataflowBlockOptions)dataflowBlockOptions).EnsureOrdered = true;
            ((DataflowBlockOptions)dataflowBlockOptions).BoundedCapacity = this.Config.MaxSendQueuePerPeerSize;
            dataflowBlockOptions.MaxDegreeOfParallelism = 1;
            ((DataflowBlockOptions)dataflowBlockOptions).CancellationToken = token;
            return new ActionBlock<RemoteTcpPeerOutgoingMessage>(func, dataflowBlockOptions);
        }

        protected virtual SslStream CreateSslStream(TcpClient tcpClient)
        {
            return new SslStream((Stream)tcpClient.GetStream(), false, this.Config.RemoteCertificateValidationCallback, new LocalCertificateSelectionCallback(this.SelectDefaultLocalCertificate), this.Config.EncryptionPolicy);
        }

        protected virtual X509Certificate SelectDefaultLocalCertificate(
          object sender,
          string targetHost,
          X509CertificateCollection localCertificates,
          X509Certificate remoteCertificate,
          string[] acceptableIssuers)
        {
            return this.Config.X509Certificate;
        }

        protected virtual Task AuthenticateSslStream(
          TcpClient tcpClient,
          SslStream sslStream,
          CancellationToken token)
        {
            return SslStreamExtensions.AuthenticateAsServerWithCancellationAsync(sslStream, this.Config.X509Certificate, this.Config.ClientCertificateRequiredCallback(tcpClient), this.Config.EnabledProtocols, this.Config.CheckCertificateRevocationCallback(tcpClient), token);
        }

        protected virtual RemoteTcpPeer CreateRemoteTcpPeer(
          TcpClient tcpClient,
          ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue,
          CancellationTokenSource tokenSource)
        {
            return new RemoteTcpPeer(this.Config.ProtocolFrameDefragmenterFactory, tcpClient, sendQueue, tokenSource, new Action<RemoteTcpPeerExceptionEventArgs>(this.OnRemoteTcpPeerExceptionOccured));
        }

        protected virtual RemoteTcpPeer CreateRemoteTcpPeer(
          TcpClient tcpClient,
          SslStream sslStream,
          ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue,
          CancellationTokenSource tokenSource)
        {
            return new RemoteTcpPeer(this.Config.ProtocolFrameDefragmenterFactory, tcpClient, (Stream)sslStream, sendQueue, tokenSource, new Action<RemoteTcpPeerExceptionEventArgs>(this.OnRemoteTcpPeerExceptionOccured));
        }

        protected virtual async Task HandleRemotePeerAsync(
          RemoteTcpPeer remoteTcpPeer,
          CancellationToken cancellationToken)
        {
            try
            {
                await this.ReceiveFromRemotePeerAsync(remoteTcpPeer, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                remoteTcpPeer.ConnectionCloseReason = cancellationToken.IsCancellationRequested ? ConnectionCloseReason.LocalShutdown : ConnectionCloseReason.Timeout;
                remoteTcpPeer.ConnectionCloseException = (Exception)ex;
            }
            catch (Exception ex)
            {
                remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.ExceptionOccured;
                remoteTcpPeer.ConnectionCloseException = ex;
                this.OnUnhandledException(new ExceptionEventArgs(ex));
            }
            this.RemoveRemoteTcpPeerFromConnectedList(remoteTcpPeer.IPEndPoint);
            ConnectionClosedEventArgs e = new ConnectionClosedEventArgs((IRemoteTcpPeer)remoteTcpPeer)
            {
                ConnectionCloseException = remoteTcpPeer.ConnectionCloseException,
                ConnectionCloseReason = remoteTcpPeer.ConnectionCloseReason
            };
            this.OnConnectionClosed(remoteTcpPeer, e);
        }

        protected virtual async Task ReceiveFromRemotePeerAsync(
          RemoteTcpPeer remoteTcpPeer,
          CancellationToken cancellationToken)
        {
            ReadFrameResult readFrameResult = (ReadFrameResult)null;
            while (!cancellationToken.IsCancellationRequested)
            {
                using (CancellationTokenSource timeoutCts = this.Config.ConnectionTimeout == TimeSpan.Zero ? new CancellationTokenSource() : new CancellationTokenSource(this.Config.ConnectionTimeout))
                {
                    using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken))
                        readFrameResult = await remoteTcpPeer.ProtocolFrameDefragmenter.ReadFrameAsync((IRemoteTcpPeer)remoteTcpPeer, readFrameResult?.LeftOvers, linkedCts.Token).ConfigureAwait(false);
                }
                if (readFrameResult.ReadFrameStatus == ReadFrameStatus.StreamClosed)
                {
                    remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.RemoteShutdown;
                    return;
                }
                if (readFrameResult.ReadFrameStatus == ReadFrameStatus.FrameDropped)
                {
                    readFrameResult = (ReadFrameResult)null;
                }
                else
                {
                    TcpFrameArrivedEventArgs e = new TcpFrameArrivedEventArgs((IRemoteTcpPeer)remoteTcpPeer, readFrameResult.FrameData);
                    this.OnFrameArrived(remoteTcpPeer, e);
                }
            }
            remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.LocalShutdown;
        }

        protected virtual async Task SendToRemotePeerAsync(RemoteTcpPeerOutgoingMessage outgoingMessage)
        {
            try
            {
                ConfiguredTaskAwaitable configuredTaskAwaitable = StreamExtensions.WriteWithRealCancellationAsync(outgoingMessage.RemoteTcpPeer.TcpStream, outgoingMessage.Buffer.Memory, outgoingMessage.Buffer.Offset, outgoingMessage.Buffer.Count, outgoingMessage.CancellationToken).ConfigureAwait(false);
                await configuredTaskAwaitable;
                configuredTaskAwaitable = outgoingMessage.RemoteTcpPeer.TcpStream.FlushAsync(outgoingMessage.CancellationToken).ConfigureAwait(false);
                await configuredTaskAwaitable;
                outgoingMessage.SendTaskCompletionSource.TrySetResult(true);
            }
            catch (OperationCanceledException ex)
            {
                outgoingMessage.SendTaskCompletionSource.TrySetCanceled(ex.CancellationToken);
            }
            catch (Exception ex)
            {
                this.OnRemoteTcpPeerExceptionOccured(new RemoteTcpPeerExceptionEventArgs((IRemoteTcpPeer)outgoingMessage.RemoteTcpPeer, ex));
            }
        }

        protected virtual void OnConnectionEstablished(ConnectionEstablishedEventArgs e)
        {
            try
            {
                EventHandler<ConnectionEstablishedEventArgs> connectionEstablished = this.ConnectionEstablished;
                if (connectionEstablished == null)
                    return;
                connectionEstablished((object)this, e);
            }
            catch (Exception ex)
            {
                this.OnUnhandledException(new ExceptionEventArgs(ex));
            }
        }

        protected virtual void OnFrameArrived(RemoteTcpPeer remoteTcpPeer, TcpFrameArrivedEventArgs e)
        {
            try
            {
                remoteTcpPeer.OnFrameArrived(e);
            }
            catch (Exception ex)
            {
                this.OnUnhandledException(new ExceptionEventArgs(ex));
            }
            try
            {
                EventHandler<TcpFrameArrivedEventArgs> frameArrived = this.FrameArrived;
                if (frameArrived == null)
                    return;
                frameArrived((object)this, e);
            }
            catch (Exception ex)
            {
                this.OnUnhandledException(new ExceptionEventArgs(ex));
            }
        }

        protected virtual void OnConnectionClosed(
          RemoteTcpPeer remoteTcpPeer,
          ConnectionClosedEventArgs e)
        {
            try
            {
                remoteTcpPeer.OnConnectionClosed(e);
            }
            catch (Exception ex)
            {
                this.OnUnhandledException(new ExceptionEventArgs(ex));
            }
            try
            {
                EventHandler<ConnectionClosedEventArgs> connectionClosed = this.ConnectionClosed;
                if (connectionClosed == null)
                    return;
                connectionClosed((object)this, e);
            }
            catch (Exception ex)
            {
                this.OnUnhandledException(new ExceptionEventArgs(ex));
            }
        }

        protected virtual void OnServerStarted(TcpServerStartedEventArgs e)
        {
            try
            {
                EventHandler<TcpServerStartedEventArgs> serverStarted = this.ServerStarted;
                if (serverStarted == null)
                    return;
                serverStarted((object)this, e);
            }
            catch (Exception ex)
            {
                this.OnUnhandledException(new ExceptionEventArgs(ex));
            }
        }

        protected virtual void OnServerStopped(TcpServerStoppedEventArgs e)
        {
            try
            {
                EventHandler<TcpServerStoppedEventArgs> serverStopped = this.ServerStopped;
                if (serverStopped == null)
                    return;
                serverStopped((object)this, e);
            }
            catch (Exception ex)
            {
                this.OnUnhandledException(new ExceptionEventArgs(ex));
            }
        }

        protected virtual void OnServerExceptionOccured(TcpServerExceptionEventArgs e)
        {
            try
            {
                EventHandler<TcpServerExceptionEventArgs> exceptionOccured = this.ServerExceptionOccured;
                if (exceptionOccured == null)
                    return;
                exceptionOccured((object)this, e);
            }
            catch (Exception ex)
            {
                this.OnUnhandledException(new ExceptionEventArgs(ex));
            }
        }

        protected virtual void OnRemoteTcpPeerExceptionOccured(RemoteTcpPeerExceptionEventArgs e)
        {
            try
            {
                EventHandler<RemoteTcpPeerExceptionEventArgs> exceptionOccured = this.RemoteTcpPeerExceptionOccured;
                if (exceptionOccured == null)
                    return;
                exceptionOccured((object)this, e);
            }
            catch (Exception ex)
            {
                this.OnUnhandledException(new ExceptionEventArgs(ex));
            }
        }

        protected virtual void OnUnhandledException(ExceptionEventArgs e)
        {
            EventHandler<ExceptionEventArgs> exceptionOccured = this.UnhandledExceptionOccured;
            if (exceptionOccured == null)
                return;
            exceptionOccured((object)this, e);
        }
    }
}
