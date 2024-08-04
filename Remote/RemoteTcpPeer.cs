
// Type: AsyncNetStandard.Tcp.Remote.RemoteTcpPeerusing AsyncNetStandard.Core;
using AsyncNetStandard.Core;
using AsyncNetStandard.Tcp.Connection;
using AsyncNetStandard.Tcp.Connection.Events;
using AsyncNetStandard.Tcp.Defragmentation;
using AsyncNetStandard.Tcp.Remote.Events;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;


namespace AsyncNetStandard.Tcp.Remote
{
  /// <summary>An implementation of remote tcp peer</summary>
  public class RemoteTcpPeer : IRemoteTcpPeer, IDisposable
  {
    private readonly ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly Action<RemoteTcpPeerExceptionEventArgs> exceptionHandler;
    private readonly CancellationToken cancellationToken;
    private ConnectionCloseReason connectionCloseReason;
    private Func<IRemoteTcpPeer, IProtocolFrameDefragmenter> protocolFrameDefragmenterFactory;
    private Exception connectionCloseException;

    public RemoteTcpPeer(
      Func<IRemoteTcpPeer, IProtocolFrameDefragmenter> protocolFrameDefragmenterFactory,
      TcpClient tcpClient,
      ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue,
      CancellationTokenSource cts,
      Action<RemoteTcpPeerExceptionEventArgs> exceptionHandler)
    {
      this.protocolFrameDefragmenterFactory = protocolFrameDefragmenterFactory;
      this.TcpClient = tcpClient;
      this.TcpStream = (Stream) tcpClient.GetStream();
      this.IPEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
      this.sendQueue = sendQueue;
      this.cancellationTokenSource = cts;
      this.cancellationToken = cts.Token;
      this.exceptionHandler = exceptionHandler;
    }

    public RemoteTcpPeer(
      Func<IRemoteTcpPeer, IProtocolFrameDefragmenter> protocolFrameDefragmenterFactory,
      TcpClient tcpClient,
      Stream tcpStream,
      ActionBlock<RemoteTcpPeerOutgoingMessage> sendQueue,
      CancellationTokenSource cts,
      Action<RemoteTcpPeerExceptionEventArgs> exceptionHandler)
    {
      this.protocolFrameDefragmenterFactory = protocolFrameDefragmenterFactory;
      this.TcpClient = tcpClient;
      this.TcpStream = tcpStream;
      this.IPEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;
      this.sendQueue = sendQueue;
      this.cancellationTokenSource = cts;
      this.cancellationToken = cts.Token;
      this.exceptionHandler = exceptionHandler;
    }

    /// <summary>Fires when TCP frame from this client/peer arrived</summary>
    public event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

    /// <summary>Fires when connection with this client/peer closes</summary>
    public event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

    /// <summary>
    /// Underlying <see cref="P:AsyncNetStandard.Tcp.Remote.RemoteTcpPeer.TcpClient" />. You should use <see cref="P:AsyncNetStandard.Tcp.Remote.IRemoteTcpPeer.TcpStream" /> instead of TcpClient.GetStream()
    /// </summary>
    public virtual TcpClient TcpClient { get; }

    /// <summary>Tcp stream</summary>
    public virtual Stream TcpStream { get; }

    /// <summary>Remote tcp peer endpoint</summary>
    public virtual IPEndPoint IPEndPoint { get; }

    /// <summary>
    /// You can set it to your own custom object that implements <see cref="T:System.IDisposable" />. Your custom object will be disposed with this remote peer
    /// </summary>
    public virtual IDisposable CustomObject { get; set; }

    /// <summary>
    /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <returns>True - added to the send queue. False - this client/peer is disconnected</returns>
    public virtual Task<bool> AddToSendQueueAsync(byte[] data)
    {
      return this.AddToSendQueueAsync(data, CancellationToken.None);
    }

    /// <summary>
    /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns>True - added to the send queue. False - this client/peer is disconnected</returns>
    public virtual Task<bool> AddToSendQueueAsync(byte[] data, CancellationToken cancellationToken)
    {
      return this.AddToSendQueueAsync(data, 0, data.Length, cancellationToken);
    }

    /// <summary>
    /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
    /// </summary>
    /// <param name="buffer">Buffer containing data to send</param>
    /// <param name="offset">Data offset in <paramref name="buffer" /></param>
    /// <param name="count">Numbers of bytes to send</param>
    /// <returns>True - added to the send queue. False - send queue buffer is full or this client/peer is disconnected</returns>
    public virtual Task<bool> AddToSendQueueAsync(byte[] buffer, int offset, int count)
    {
      return this.AddToSendQueueAsync(buffer, offset, count, CancellationToken.None);
    }

    /// <summary>
    /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
    /// </summary>
    /// <param name="buffer">Buffer containing data to send</param>
    /// <param name="offset">Data offset in <paramref name="buffer" /></param>
    /// <param name="count">Numbers of bytes to send</param>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns>True - added to the send queue. False - send queue buffer is full or this client/peer is disconnected</returns>
    public virtual async Task<bool> AddToSendQueueAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken)
    {
      RemoteTcpPeer remoteTcpPeer = this;
      bool sendQueueAsync;
      using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(remoteTcpPeer.cancellationToken, cancellationToken))
      {
        try
        {
          sendQueueAsync = await DataflowBlock.SendAsync<RemoteTcpPeerOutgoingMessage>((ITargetBlock<RemoteTcpPeerOutgoingMessage>) remoteTcpPeer.sendQueue, new RemoteTcpPeerOutgoingMessage(remoteTcpPeer, new AsyncNetBuffer(buffer, offset, count), remoteTcpPeer.cancellationToken), linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
          if (cancellationToken.IsCancellationRequested && !remoteTcpPeer.cancellationToken.IsCancellationRequested)
            throw;
          else
            sendQueueAsync = false;
        }
      }
      return sendQueueAsync;
    }

    /// <summary>Sends data asynchronously</summary>
    /// <param name="data">Data to send</param>
    /// <returns>True - data was sent. False - this client/peer is disconnected</returns>
    public virtual Task<bool> SendAsync(byte[] data)
    {
      return this.SendAsync(data, 0, data.Length, CancellationToken.None);
    }

    /// <summary>Sends data asynchronously</summary>
    /// <param name="data">Data to send</param>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns>True - data was sent. False - this client/peer is disconnected</returns>
    public virtual Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken)
    {
      return this.SendAsync(data, 0, data.Length, cancellationToken);
    }

    /// <summary>Sends data asynchronously</summary>
    /// <param name="buffer">Buffer containing data to send</param>
    /// <param name="offset">Data offset in <paramref name="buffer" /></param>
    /// <param name="count">Numbers of bytes to send</param>
    /// <returns>True - data was sent. False - this client/peer is disconnected</returns>
    public virtual Task<bool> SendAsync(byte[] buffer, int offset, int count)
    {
      return this.SendAsync(buffer, offset, count, CancellationToken.None);
    }

    /// <summary>Sends data asynchronously</summary>
    /// <param name="buffer">Buffer containing data to send</param>
    /// <param name="offset">Data offset in <paramref name="buffer" /></param>
    /// <param name="count">Numbers of bytes to send</param>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns>True - data was sent. False - this client/peer is disconnected</returns>
    public virtual async Task<bool> SendAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken)
    {
      RemoteTcpPeer remoteTcpPeer = this;
      bool flag;
      using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(remoteTcpPeer.cancellationToken, cancellationToken))
      {
        RemoteTcpPeerOutgoingMessage message = new RemoteTcpPeerOutgoingMessage(remoteTcpPeer, new AsyncNetBuffer(buffer, offset, count), linkedCts.Token);
        try
        {
          ConfiguredTaskAwaitable<bool> configuredTaskAwaitable = DataflowBlock.SendAsync<RemoteTcpPeerOutgoingMessage>((ITargetBlock<RemoteTcpPeerOutgoingMessage>) remoteTcpPeer.sendQueue, message, linkedCts.Token).ConfigureAwait(false);
          flag = await configuredTaskAwaitable;
          if (!flag)
            return flag;
          CancellationTokenRegistration tokenRegistration = linkedCts.Token.Register((Action) (() => message.SendTaskCompletionSource.TrySetCanceled(linkedCts.Token)));
          try
          {
            configuredTaskAwaitable = message.SendTaskCompletionSource.Task.ConfigureAwait(false);
            flag = await configuredTaskAwaitable;
          }
          finally
          {
            tokenRegistration.Dispose();
          }
          tokenRegistration = new CancellationTokenRegistration();
        }
        catch (OperationCanceledException ex)
        {
          if (cancellationToken.IsCancellationRequested && !remoteTcpPeer.cancellationToken.IsCancellationRequested)
            throw;
          else
            flag = false;
        }
      }
      return flag;
    }

    /// <summary>
    /// Adds data to the send queue. It will fail if send queue buffer is full returning false
    /// </summary>
    /// <param name="data"></param>
    /// <returns>True - added to the send queue. False - send queue buffer is full or this client/peer is disconnected</returns>
    public virtual bool Post(byte[] data) => this.Post(data, 0, data.Length);

    /// <summary>
    /// Adds data to the send queue. It will fail if send queue buffer is full returning false
    /// </summary>
    /// <param name="buffer">Buffer containing data to send</param>
    /// <param name="offset">Data offset in <paramref name="buffer" /></param>
    /// <param name="count">Numbers of bytes to send</param>
    /// <returns>True - added to the send queue. False - send queue buffer is full or this client/peer is disconnected</returns>
    public virtual bool Post(byte[] buffer, int offset, int count)
    {
      return this.sendQueue.Post(new RemoteTcpPeerOutgoingMessage(this, new AsyncNetBuffer(buffer, offset, count), this.cancellationToken));
    }

    public virtual void Dispose()
    {
      this.Disconnect(this.ConnectionCloseReason);
      try
      {
        this.CustomObject?.Dispose();
        this.cancellationTokenSource.Dispose();
      }
      catch (Exception ex)
      {
        Action<RemoteTcpPeerExceptionEventArgs> exceptionHandler = this.exceptionHandler;
        if (exceptionHandler == null)
          return;
        exceptionHandler(new RemoteTcpPeerExceptionEventArgs((IRemoteTcpPeer) this, ex));
      }
    }

    /// <summary>Disconnects this peer/client</summary>
    /// <param name="reason">Disconnect reason</param>
    public virtual void Disconnect(ConnectionCloseReason reason)
    {
      try
      {
        this.connectionCloseReason = reason;
        this.cancellationTokenSource.Cancel();
      }
      catch (Exception ex)
      {
        Action<RemoteTcpPeerExceptionEventArgs> exceptionHandler = this.exceptionHandler;
        if (exceptionHandler == null)
          return;
        exceptionHandler(new RemoteTcpPeerExceptionEventArgs((IRemoteTcpPeer) this, ex));
      }
    }

    public virtual void SwitchProtocol(
      Func<IRemoteTcpPeer, IProtocolFrameDefragmenter> protocolFrameDefragmenterFactory)
    {
      this.protocolFrameDefragmenterFactory = protocolFrameDefragmenterFactory;
    }

    public virtual IProtocolFrameDefragmenter ProtocolFrameDefragmenter
    {
      get => this.protocolFrameDefragmenterFactory((IRemoteTcpPeer) this);
    }

    public virtual ConnectionCloseReason ConnectionCloseReason
    {
      get => this.connectionCloseReason;
      set
      {
        this.connectionCloseReason = this.connectionCloseReason != ConnectionCloseReason.Unknown ? this.connectionCloseReason : value;
      }
    }

    public virtual Exception ConnectionCloseException
    {
      get => this.connectionCloseException;
      set
      {
        this.connectionCloseException = this.connectionCloseException == null ? value : this.connectionCloseException;
      }
    }

    public virtual void OnFrameArrived(TcpFrameArrivedEventArgs e)
    {
      EventHandler<TcpFrameArrivedEventArgs> frameArrived = this.FrameArrived;
      if (frameArrived == null)
        return;
      frameArrived((object) this, e);
    }

    public virtual void OnConnectionClosed(ConnectionClosedEventArgs e)
    {
      EventHandler<ConnectionClosedEventArgs> connectionClosed = this.ConnectionClosed;
      if (connectionClosed == null)
        return;
      connectionClosed((object) this, e);
    }
  }
}
