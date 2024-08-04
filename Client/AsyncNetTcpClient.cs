using AsyncNetStandard.Core.Events;
using AsyncNetStandard.Core.Extensions;
using AsyncNetStandard.Tcp.Client.Events;
using AsyncNetStandard.Tcp.Connection;
using AsyncNetStandard.Tcp.Connection.Events;
using AsyncNetStandard.Tcp.Defragmentation;
using AsyncNetStandard.Tcp.Extensions;
using AsyncNetStandard.Tcp.Remote;
using AsyncNetStandard.Tcp.Remote.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;


namespace AsyncNetStandard.Tcp.Client
{
  /// <summary>An implementation of asynchronous TCP client</summary>
  public class AsyncNetTcpClient : IAsyncNetTcpClient
  {
    /// <summary>
    /// Constructs TCP client that connects to the particular server and has default configuration
    /// </summary>
    /// <param name="targetHostname">Server hostname</param>
    /// <param name="targetPort">Server port</param>
    public AsyncNetTcpClient(string targetHostname, int targetPort)
      : this(new AsyncNetTcpClientConfig()
      {
        TargetHostname = targetHostname,
        TargetPort = targetPort
      })
    {
    }

    /// <summary>Constructs TCP client with custom configuration</summary>
    /// <param name="config">TCP client configuration</param>
    public AsyncNetTcpClient(AsyncNetTcpClientConfig config)
    {
      this.Config = new AsyncNetTcpClientConfig()
      {
        ProtocolFrameDefragmenterFactory = config.ProtocolFrameDefragmenterFactory,
        TargetHostname = config.TargetHostname,
        TargetPort = config.TargetPort,
        ConnectionTimeout = config.ConnectionTimeout,
        MaxSendQueueSize = config.MaxSendQueueSize,
        ConfigureTcpClientCallback = config.ConfigureTcpClientCallback,
        FilterResolvedIpAddressListForConnectionCallback = config.FilterResolvedIpAddressListForConnectionCallback,
        UseSsl = config.UseSsl,
        X509ClientCertificates = config.X509ClientCertificates,
        RemoteCertificateValidationCallback = config.RemoteCertificateValidationCallback,
        LocalCertificateSelectionCallback = config.LocalCertificateSelectionCallback,
        EncryptionPolicy = config.EncryptionPolicy,
        CheckCertificateRevocation = config.CheckCertificateRevocation,
        EnabledProtocols = config.EnabledProtocols
      };
    }

    /// <summary>
    /// Fires when client started running, but it's not connected yet to the server
    /// </summary>
    public event EventHandler<TcpClientStartedEventArgs> ClientStarted;

    /// <summary>Fires when client stopped running</summary>
    public event EventHandler<TcpClientStoppedEventArgs> ClientStopped;

    /// <summary>Fires when there was a problem with the client</summary>
    public event EventHandler<TcpClientExceptionEventArgs> ClientExceptionOccured;

    /// <summary>
    /// Fires when there was a problem while handling communication with the server
    /// </summary>
    public event EventHandler<RemoteTcpPeerExceptionEventArgs> RemoteTcpPeerExceptionOccured;

    /// <summary>
    /// Fires when unhandled exception occured - e.g. when event subscriber throws an exception
    /// </summary>
    public event EventHandler<ExceptionEventArgs> UnhandledExceptionOccured;

    /// <summary>Fires when connection with the server is established</summary>
    public event EventHandler<ConnectionEstablishedEventArgs> ConnectionEstablished;

    /// <summary>Fires when TCP frame arrived from the server</summary>
    public event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

    /// <summary>Fires when connection with the server closes</summary>
    public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

    /// <summary>
    /// Asynchronously starts the client that run until connection with the server is closed
    /// </summary>
    /// <returns><see cref="T:System.Threading.Tasks.Task" /></returns>
    public virtual Task StartAsync() => this.StartAsync(CancellationToken.None);

    /// <summary>
    /// Asynchronously starts the client that run until connection with the server is closed or <paramref name="cancellationToken" /> is cancelled
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task" /></returns>
    public virtual async Task StartAsync(CancellationToken cancellationToken)
    {
      AsyncNetTcpClient asyncNetTcpClient = this;
      TcpClient tcpClient = asyncNetTcpClient.CreateTcpClient();
      Action<TcpClient> tcpClientCallback = asyncNetTcpClient.Config.ConfigureTcpClientCallback;
      if (tcpClientCallback != null)
        tcpClientCallback(tcpClient);
      asyncNetTcpClient.OnClientStarted(new TcpClientStartedEventArgs()
      {
        TargetHostname = asyncNetTcpClient.Config.TargetHostname,
        TargetPort = asyncNetTcpClient.Config.TargetPort
      });
      IPAddress[] addresses;
      try
      {
        addresses = await asyncNetTcpClient.GetHostAddresses(cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException ex)
      {
        asyncNetTcpClient.OnClientStopped(new TcpClientStoppedEventArgs()
        {
          ClientStoppedReason = ClientStoppedReason.InitiatingConnectionTimeout,
          Exception = (Exception) ex
        });
        tcpClient.Dispose();
        return;
      }
      catch (Exception ex)
      {
        asyncNetTcpClient.OnClientExceptionOccured(new TcpClientExceptionEventArgs(ex));
        asyncNetTcpClient.OnClientStopped(new TcpClientStoppedEventArgs()
        {
          ClientStoppedReason = ClientStoppedReason.RuntimeException,
          Exception = ex
        });
        tcpClient.Dispose();
        return;
      }
      try
      {
        await asyncNetTcpClient.ConnectAsync(tcpClient, addresses, cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException ex)
      {
        tcpClient.Dispose();
        asyncNetTcpClient.OnClientStopped(new TcpClientStoppedEventArgs()
        {
          ClientStoppedReason = ClientStoppedReason.InitiatingConnectionTimeout,
          Exception = (Exception) ex
        });
        return;
      }
      catch (Exception ex)
      {
        asyncNetTcpClient.OnClientExceptionOccured(new TcpClientExceptionEventArgs(ex));
        tcpClient.Dispose();
        asyncNetTcpClient.OnClientStopped(new TcpClientStoppedEventArgs()
        {
          ClientStoppedReason = ClientStoppedReason.RuntimeException,
          Exception = ex
        });
        return;
      }
      try
      {
        await asyncNetTcpClient.HandleTcpClientAsync(tcpClient, cancellationToken).ConfigureAwait(false);
      }
      catch (Exception ex)
      {
        TcpClientExceptionEventArgs e = new TcpClientExceptionEventArgs(ex);
        asyncNetTcpClient.OnClientExceptionOccured(e);
      }
    }

    protected virtual AsyncNetTcpClientConfig Config { get; set; }

    protected virtual TcpClient CreateTcpClient() => new TcpClient();

    protected virtual Task<IPAddress[]> GetHostAddresses(CancellationToken cancellationToken)
    {
      return DnsExtensions.GetHostAddressesWithCancellationTokenAsync(this.Config.TargetHostname, cancellationToken);
    }

    protected virtual IEnumerable<IPAddress> DefaultIpAddressFilter(IPAddress[] addresses)
    {
      return (IEnumerable<IPAddress>) addresses;
    }

    protected virtual async Task ConnectAsync(
      TcpClient tcpClient,
      IPAddress[] addresses,
      CancellationToken cancellationToken)
    {
      if (addresses != null || addresses.Length != 0)
        await tcpClient.ConnectWithCancellationTokenAsync((this.Config.FilterResolvedIpAddressListForConnectionCallback == null ? this.DefaultIpAddressFilter(addresses) : this.Config.FilterResolvedIpAddressListForConnectionCallback(addresses)).ToArray<IPAddress>(), this.Config.TargetPort, cancellationToken).ConfigureAwait(false);
      else
        await tcpClient.ConnectWithCancellationTokenAsync(this.Config.TargetHostname, this.Config.TargetPort, cancellationToken).ConfigureAwait(false);
    }

    protected virtual ActionBlock<RemoteTcpPeerOutgoingMessage> CreateSendQueueActionBlock(
      CancellationToken token)
    {
      Func<RemoteTcpPeerOutgoingMessage, Task> func = new Func<RemoteTcpPeerOutgoingMessage, Task>(this.SendToRemotePeerAsync);
      ExecutionDataflowBlockOptions dataflowBlockOptions = new ExecutionDataflowBlockOptions();
      ((DataflowBlockOptions) dataflowBlockOptions).EnsureOrdered = true;
      ((DataflowBlockOptions) dataflowBlockOptions).BoundedCapacity = this.Config.MaxSendQueueSize;
      dataflowBlockOptions.MaxDegreeOfParallelism = 1;
      ((DataflowBlockOptions) dataflowBlockOptions).CancellationToken = token;
      return new ActionBlock<RemoteTcpPeerOutgoingMessage>(func, dataflowBlockOptions);
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
        this.OnRemoteTcpPeerExceptionOccured(new RemoteTcpPeerExceptionEventArgs((IRemoteTcpPeer) outgoingMessage.RemoteTcpPeer, ex));
      }
    }

    protected virtual async Task HandleTcpClientAsync(TcpClient tcpClient, CancellationToken token)
    {
      AsyncNetTcpClient asyncNetTcpClient = this;
      using (tcpClient)
      {
        CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue = asyncNetTcpClient.CreateSendQueueActionBlock(linkedCts.Token);
        SslStream sslStream = (SslStream) null;
        RemoteTcpPeer remoteTcpPeer;
        ConfiguredTaskAwaitable configuredTaskAwaitable;
        try
        {
          if (asyncNetTcpClient.Config.UseSsl)
          {
            sslStream = asyncNetTcpClient.CreateSslStream(tcpClient);
            configuredTaskAwaitable = asyncNetTcpClient.AuthenticateSslStream(tcpClient, sslStream, linkedCts.Token).ConfigureAwait(false);
            await configuredTaskAwaitable;
            remoteTcpPeer = asyncNetTcpClient.CreateRemoteTcpPeer(tcpClient, sslStream, sendQueue, linkedCts);
          }
          else
            remoteTcpPeer = asyncNetTcpClient.CreateRemoteTcpPeer(tcpClient, sendQueue, linkedCts);
        }
        catch (AuthenticationException ex)
        {
          TcpClientExceptionEventArgs e = new TcpClientExceptionEventArgs((Exception) ex);
          asyncNetTcpClient.OnClientExceptionOccured(e);
          sendQueue.Complete();
          sslStream?.Dispose();
          asyncNetTcpClient.OnClientStopped(new TcpClientStoppedEventArgs()
          {
            ClientStoppedReason = ClientStoppedReason.RuntimeException,
            Exception = (Exception) ex
          });
          linkedCts.Dispose();
          return;
        }
        catch (Exception ex)
        {
          sendQueue.Complete();
          asyncNetTcpClient.OnClientStopped(new TcpClientStoppedEventArgs()
          {
            ClientStoppedReason = ClientStoppedReason.RuntimeException,
            Exception = ex
          });
          linkedCts.Dispose();
          return;
        }
        using (remoteTcpPeer)
        {
          ConnectionEstablishedEventArgs e = new ConnectionEstablishedEventArgs((IRemoteTcpPeer) remoteTcpPeer);
          asyncNetTcpClient.OnConnectionEstablished(e);
          configuredTaskAwaitable = asyncNetTcpClient.HandleRemotePeerAsync(remoteTcpPeer, linkedCts.Token).ConfigureAwait(false);
          await configuredTaskAwaitable;
          sendQueue.Complete();
          sslStream?.Dispose();
          asyncNetTcpClient.OnClientStopped(new TcpClientStoppedEventArgs()
          {
            ClientStoppedReason = ClientStoppedReason.Disconnected,
            ConnectionCloseReason = remoteTcpPeer.ConnectionCloseReason,
            Exception = remoteTcpPeer.ConnectionCloseException
          });
        }
        linkedCts = (CancellationTokenSource) null;
        sendQueue = (ActionBlock<RemoteTcpPeerOutgoingMessage>) null;
        remoteTcpPeer = (RemoteTcpPeer) null;
        sslStream = (SslStream) null;
      }
    }

    protected virtual SslStream CreateSslStream(TcpClient tcpClient)
    {
      LocalCertificateSelectionCallback userCertificateSelectionCallback = this.Config.LocalCertificateSelectionCallback ?? new LocalCertificateSelectionCallback(this.SelectDefaultLocalCertificate);
      return new SslStream((Stream) tcpClient.GetStream(), false, this.Config.RemoteCertificateValidationCallback, userCertificateSelectionCallback, this.Config.EncryptionPolicy);
    }

    protected virtual X509Certificate SelectDefaultLocalCertificate(
      object sender,
      string targetHost,
      X509CertificateCollection localCertificates,
      X509Certificate remoteCertificate,
      string[] acceptableIssuers)
    {
      if (acceptableIssuers != null && acceptableIssuers.Length != 0 && localCertificates != null && localCertificates.Count > 0)
      {
        foreach (X509Certificate localCertificate in localCertificates)
        {
          string issuer = localCertificate.Issuer;
          if (Array.IndexOf<string>(acceptableIssuers, issuer) != -1)
            return localCertificate;
        }
      }
      return localCertificates != null && localCertificates.Count > 0 ? localCertificates[0] : (X509Certificate) null;
    }

    protected virtual Task AuthenticateSslStream(
      TcpClient tcpClient,
      SslStream sslStream,
      CancellationToken token)
    {
      SslStream sslStream1 = sslStream;
      string targetHostname = this.Config.TargetHostname;
      IEnumerable<X509Certificate> clientCertificates = this.Config.X509ClientCertificates;
      X509CertificateCollection certificateCollection = new X509CertificateCollection((clientCertificates != null ? clientCertificates.ToArray<X509Certificate>() : (X509Certificate[]) null) ?? new X509Certificate[0]);
      int enabledProtocols = (int) this.Config.EnabledProtocols;
      int num = this.Config.CheckCertificateRevocation ? 1 : 0;
      CancellationToken cancellationToken = token;
      return SslStreamExtensions.AuthenticateAsClientWithCancellationAsync(sslStream1, targetHostname, certificateCollection, (SslProtocols) enabledProtocols, num != 0, cancellationToken);
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
      return new RemoteTcpPeer(this.Config.ProtocolFrameDefragmenterFactory, tcpClient, (Stream) sslStream, sendQueue, tokenSource, new Action<RemoteTcpPeerExceptionEventArgs>(this.OnRemoteTcpPeerExceptionOccured));
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
        remoteTcpPeer.ConnectionCloseException = (Exception) ex;
      }
      catch (Exception ex)
      {
        remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.ExceptionOccured;
        remoteTcpPeer.ConnectionCloseException = ex;
        this.OnUnhandledException(new ExceptionEventArgs(ex));
      }
      ConnectionClosedEventArgs e = new ConnectionClosedEventArgs((IRemoteTcpPeer) remoteTcpPeer)
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
      ReadFrameResult readFrameResult = (ReadFrameResult) null;
      while (!cancellationToken.IsCancellationRequested)
      {
        using (CancellationTokenSource timeoutCts = this.Config.ConnectionTimeout == TimeSpan.Zero ? new CancellationTokenSource() : new CancellationTokenSource(this.Config.ConnectionTimeout))
        {
          using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken))
            readFrameResult = await remoteTcpPeer.ProtocolFrameDefragmenter.ReadFrameAsync((IRemoteTcpPeer) remoteTcpPeer, readFrameResult?.LeftOvers, linkedCts.Token).ConfigureAwait(false);
        }
        if (readFrameResult.ReadFrameStatus == ReadFrameStatus.StreamClosed)
        {
          remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.RemoteShutdown;
          return;
        }
        if (readFrameResult.ReadFrameStatus == ReadFrameStatus.FrameDropped)
        {
          readFrameResult = (ReadFrameResult) null;
        }
        else
        {
          TcpFrameArrivedEventArgs e = new TcpFrameArrivedEventArgs((IRemoteTcpPeer) remoteTcpPeer, readFrameResult.FrameData);
          this.OnFrameArrived(remoteTcpPeer, e);
        }
      }
      remoteTcpPeer.ConnectionCloseReason = ConnectionCloseReason.LocalShutdown;
    }

    protected virtual void OnClientStarted(TcpClientStartedEventArgs e)
    {
      try
      {
        EventHandler<TcpClientStartedEventArgs> clientStarted = this.ClientStarted;
        if (clientStarted == null)
          return;
        clientStarted((object) this, e);
      }
      catch (Exception ex)
      {
        this.OnUnhandledException(new ExceptionEventArgs(ex));
      }
    }

    protected virtual void OnClientStopped(TcpClientStoppedEventArgs e)
    {
      try
      {
        EventHandler<TcpClientStoppedEventArgs> clientStopped = this.ClientStopped;
        if (clientStopped == null)
          return;
        clientStopped((object) this, e);
      }
      catch (Exception ex)
      {
        this.OnUnhandledException(new ExceptionEventArgs(ex));
      }
    }

    protected virtual void OnConnectionEstablished(ConnectionEstablishedEventArgs e)
    {
      try
      {
        EventHandler<ConnectionEstablishedEventArgs> connectionEstablished = this.ConnectionEstablished;
        if (connectionEstablished == null)
          return;
        connectionEstablished((object) this, e);
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
        frameArrived((object) this, e);
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
        connectionClosed((object) this, e);
      }
      catch (Exception ex)
      {
        this.OnUnhandledException(new ExceptionEventArgs(ex));
      }
    }

    protected virtual void OnClientExceptionOccured(TcpClientExceptionEventArgs e)
    {
      try
      {
        EventHandler<TcpClientExceptionEventArgs> exceptionOccured = this.ClientExceptionOccured;
        if (exceptionOccured == null)
          return;
        exceptionOccured((object) this, e);
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
        exceptionOccured((object) this, e);
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
      exceptionOccured((object) this, e);
    }
  }
}
