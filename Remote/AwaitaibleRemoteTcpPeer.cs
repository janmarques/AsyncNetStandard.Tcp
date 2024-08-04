
// Type: AsyncNet.Tcp.Remote.AwaitaibleRemoteTcpPeerusing AsyncNetStandard.Tcp.Connection.Events;
using AsyncNetStandard.Tcp.Connection.Events;
using AsyncNetStandard.Tcp.Remote.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;


namespace AsyncNetStandard.Tcp.Remote
{
  public class AwaitaibleRemoteTcpPeer : IAwaitaibleRemoteTcpPeer, IDisposable
  {
    private readonly BufferBlock<byte[]> frameBuffer;

    public AwaitaibleRemoteTcpPeer(IRemoteTcpPeer remoteTcpPeer, int frameBufferBoundedCapacity = -1)
    {
      this.RemoteTcpPeer = remoteTcpPeer;
      this.RemoteTcpPeer.FrameArrived += new EventHandler<TcpFrameArrivedEventArgs>(this.FrameArrivedCallback);
      this.RemoteTcpPeer.ConnectionClosed += new EventHandler<ConnectionClosedEventArgs>(this.ConnectionClosedCallback);
      this.frameBuffer = new BufferBlock<byte[]>(new DataflowBlockOptions()
      {
        BoundedCapacity = frameBufferBoundedCapacity
      });
    }

    /// <summary>Fires when frame buffer is full</summary>
    public event EventHandler<AddingTcpFrameToFrameBufferFailedEventArgs> AddingTcpFrameToFrameBufferFailed;

    public void Dispose()
    {
      this.RemoteTcpPeer.FrameArrived -= new EventHandler<TcpFrameArrivedEventArgs>(this.FrameArrivedCallback);
      this.RemoteTcpPeer.ConnectionClosed -= new EventHandler<ConnectionClosedEventArgs>(this.ConnectionClosedCallback);
      this.frameBuffer.Complete();
    }

    /// <summary>Underlying remote peer. Use this for sending data</summary>
    public virtual IRemoteTcpPeer RemoteTcpPeer { get; }

    /// <summary>Reads tcp frame in an asynchronous manner</summary>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns tcp frame bytes</returns>
    public virtual Task<byte[]> ReadFrameAsync() => this.ReadFrameAsync(CancellationToken.None);

    /// <summary>Reads tcp frame in an asynchronous manner</summary>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns tcp frame bytes</returns>
    public virtual async Task<byte[]> ReadFrameAsync(CancellationToken cancellationToken)
    {
      try
      {
        return await DataflowBlock.ReceiveAsync<byte[]>((ISourceBlock<byte[]>) this.frameBuffer, cancellationToken).ConfigureAwait(false);
      }
      catch (OperationCanceledException ex)
      {
        throw;
      }
      catch (InvalidOperationException ex)
      {
        return new byte[0];
      }
    }

    /// <summary>
    /// Reads all bytes from tcp stream until peer disconnects in an asynchronous manner
    /// </summary>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns byte array</returns>
    public virtual Task<byte[]> ReadAllBytesAsync()
    {
      return this.ReadAllBytesAsync(CancellationToken.None);
    }

    /// <summary>
    /// Reads all bytes from tcp stream until peer disconnects in an asynchronous manner
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns byte array</returns>
    public virtual async Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken)
    {
      byte[] array;
      using (MemoryStream ms = new MemoryStream())
      {
        while (true)
        {
          byte[] buffer;
          if ((buffer = await this.ReadFrameAsync(cancellationToken).ConfigureAwait(false)).Length != 0)
            await ms.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
          else
            break;
        }
        array = ms.ToArray();
      }
      return array;
    }

    /// <summary>
    /// Reads all frames until peer disconnects in an asynchronous manner
    /// </summary>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns list of frames</returns>
    public virtual Task<IEnumerable<byte[]>> ReadAllFramesAsync()
    {
      return this.ReadAllFramesAsync(CancellationToken.None);
    }

    /// <summary>
    /// Reads all frames until peer disconnects in an asynchronous manner
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns><see cref="T:System.Threading.Tasks.Task`1" /> which returns list of frames</returns>
    public virtual async Task<IEnumerable<byte[]>> ReadAllFramesAsync(
      CancellationToken cancellationToken)
    {
      List<byte[]> list = new List<byte[]>();
      while (true)
      {
        byte[] numArray;
        if ((numArray = await this.ReadFrameAsync(cancellationToken).ConfigureAwait(false)).Length != 0)
          list.Add(numArray);
        else
          break;
      }
      return (IEnumerable<byte[]>) list;
    }

    protected virtual void FrameArrivedCallback(object sender, TcpFrameArrivedEventArgs e)
    {
      if (DataflowBlock.Post<byte[]>((ITargetBlock<byte[]>) this.frameBuffer, e.FrameData) || this.frameBuffer.Completion.IsCompleted)
        return;
      this.OnAddingTcpFrameToFrameBufferFailed(new AddingTcpFrameToFrameBufferFailedEventArgs((IAwaitaibleRemoteTcpPeer) this, e.FrameData));
    }

    protected virtual void ConnectionClosedCallback(object sender, ConnectionClosedEventArgs e)
    {
      this.frameBuffer.Complete();
    }

    protected virtual void OnAddingTcpFrameToFrameBufferFailed(
      AddingTcpFrameToFrameBufferFailedEventArgs e)
    {
      EventHandler<AddingTcpFrameToFrameBufferFailedEventArgs> frameBufferFailed = this.AddingTcpFrameToFrameBufferFailed;
      if (frameBufferFailed == null)
        return;
      frameBufferFailed((object) this, e);
    }
  }
}
