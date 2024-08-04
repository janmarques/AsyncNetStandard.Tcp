
// Type: AsyncNetStandard.Tcp.Remote.IRemoteTcpPeerusing AsyncNetStandard.Tcp.Connection;
using AsyncNetStandard.Tcp.Connection;
using AsyncNetStandard.Tcp.Connection.Events;
using AsyncNetStandard.Tcp.Defragmentation;
using AsyncNetStandard.Tcp.Remote.Events;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Tcp.Remote
{
  /// <summary>An interface for remote tcp client/peer</summary>
  public interface IRemoteTcpPeer : IDisposable
  {
    /// <summary>Fires when connection with this client/peer closes</summary>
    event EventHandler<ConnectionClosedEventArgs> ConnectionClosed;

    /// <summary>Fires when TCP frame from this client/peer arrived</summary>
    event EventHandler<TcpFrameArrivedEventArgs> FrameArrived;

    /// <summary>
    /// You can set it to your own custom object that implements <see cref="T:System.IDisposable" />. Your custom object will be disposed with this remote peer
    /// </summary>
    IDisposable CustomObject { get; set; }

    /// <summary>Remote tcp peer endpoint</summary>
    IPEndPoint IPEndPoint { get; }

    /// <summary>Tcp stream</summary>
    Stream TcpStream { get; }

    /// <summary>
    /// Underlying <see cref="T:System.Net.Sockets.TcpClient" />. You should use <see cref="P:AsyncNetStandard.Tcp.Remote.IRemoteTcpPeer.TcpStream" /> instead of TcpClient.GetStream()
    /// </summary>
    TcpClient TcpClient { get; }

    /// <summary>Disconnects this peer/client</summary>
    /// <param name="reason">Disconnect reason</param>
    void Disconnect(ConnectionCloseReason reason);

    /// <summary>
    /// Adds data to the send queue. It will fail if send queue buffer is full returning false
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <returns>True - added to the send queue. False - send queue buffer is full or this client/peer is disconnected</returns>
    bool Post(byte[] data);

    /// <summary>
    /// Adds data to the send queue. It will fail if send queue buffer is full returning false
    /// </summary>
    /// <param name="buffer">Buffer containing data to send</param>
    /// <param name="offset">Data offset in <paramref name="buffer" /></param>
    /// <param name="count">Numbers of bytes to send</param>
    /// <returns>True - added to the send queue. False - send queue buffer is full or this client/peer is disconnected</returns>
    bool Post(byte[] buffer, int offset, int count);

    /// <summary>
    /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns True - added to the send queue or False - this client/peer is disconnected</returns>
    Task<bool> AddToSendQueueAsync(byte[] data);

    /// <summary>
    /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
    /// </summary>
    /// <param name="data">Data to send</param>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns True - added to the send queue or False - this client/peer is disconnected</returns>
    Task<bool> AddToSendQueueAsync(byte[] data, CancellationToken cancellationToken);

    /// <summary>
    /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
    /// </summary>
    /// <param name="buffer">Buffer containing data to send</param>
    /// <param name="offset">Data offset in <paramref name="buffer" /></param>
    /// <param name="count">Numbers of bytes to send</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns True - added to the send queue or False - send queue buffer is full or this client/peer is disconnected</returns>
    Task<bool> AddToSendQueueAsync(byte[] buffer, int offset, int count);

    /// <summary>
    /// Adds data to the send queue. It will wait asynchronously if the send queue buffer is full
    /// </summary>
    /// <param name="buffer">Buffer containing data to send</param>
    /// <param name="offset">Data offset in <paramref name="buffer" /></param>
    /// <param name="count">Numbers of bytes to send</param>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns True - added to the send queue or False - send queue buffer is full or this client/peer is disconnected</returns>
    Task<bool> AddToSendQueueAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken);

    /// <summary>Sends data asynchronously</summary>
    /// <param name="data">Data to send</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns True - data was sent or False - this client/peer is disconnected</returns>
    Task<bool> SendAsync(byte[] data);

    /// <summary>Sends data asynchronously</summary>
    /// <param name="data">Data to send</param>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns True - data was sent or False - this client/peer is disconnected</returns>
    Task<bool> SendAsync(byte[] data, CancellationToken cancellationToken);

    /// <summary>Sends data asynchronously</summary>
    /// <param name="buffer">Buffer containing data to send</param>
    /// <param name="offset">Data offset in <paramref name="buffer" /></param>
    /// <param name="count">Numbers of bytes to send</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns True - data was sent or False - this client/peer is disconnected</returns>
    Task<bool> SendAsync(byte[] buffer, int offset, int count);

    /// <summary>Sends data asynchronously</summary>
    /// <param name="buffer">Buffer containing data to send</param>
    /// <param name="offset">Data offset in <paramref name="buffer" /></param>
    /// <param name="count">Numbers of bytes to send</param>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns True - data was sent or False - this client/peer is disconnected</returns>
    Task<bool> SendAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken);

    void SwitchProtocol(
      Func<IRemoteTcpPeer, IProtocolFrameDefragmenter> protocolFrameDefragmenterFactory);
  }
}
