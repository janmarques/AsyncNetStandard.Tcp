using AsyncNetStandard.Tcp.Client.Events;
using AsyncNetStandard.Tcp.Connection.Events;
using AsyncNetStandard.Tcp.Remote;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Tcp.Client
{
  public class AwaitaibleAsyncNetTcpClient : IAwaitaibleAsyncNetTcpClient, IDisposable
  {
    private readonly int frameBufferBoundedCapacity;
    private readonly CancellationTokenSource cts;
    private TaskCompletionSource<IAwaitaibleRemoteTcpPeer> tcsConnection;

    public AwaitaibleAsyncNetTcpClient(AsyncNetTcpClient client, int frameBufferBoundedCapacity = -1)
    {
      this.Client = client;
      this.Client.ConnectionEstablished += new EventHandler<ConnectionEstablishedEventArgs>(this.ConnectionEstablishedCallback);
      this.Client.ClientStopped += new EventHandler<TcpClientStoppedEventArgs>(this.ClientStoppedCallback);
      this.frameBufferBoundedCapacity = frameBufferBoundedCapacity;
      this.cts = new CancellationTokenSource();
    }

    public AsyncNetTcpClient Client { get; }

    public void Dispose()
    {
      this.Client.ClientStopped -= new EventHandler<TcpClientStoppedEventArgs>(this.ClientStoppedCallback);
      this.Client.ConnectionEstablished -= new EventHandler<ConnectionEstablishedEventArgs>(this.ConnectionEstablishedCallback);
      this.cts.Cancel();
      this.cts.Dispose();
    }

    public virtual Task<IAwaitaibleRemoteTcpPeer> ConnectAsync()
    {
      return this.ConnectAsync(CancellationToken.None);
    }

    public virtual async Task<IAwaitaibleRemoteTcpPeer> ConnectAsync(
      CancellationToken cancellationToken)
    {
      this.tcsConnection = new TaskCompletionSource<IAwaitaibleRemoteTcpPeer>();
      this.Client.StartAsync(this.cts.Token);
      IAwaitaibleRemoteTcpPeer awaitaibleRemoteTcpPeer;
      using (cancellationToken.Register((Action) (() => this.tcsConnection.TrySetCanceled(cancellationToken)), false))
        awaitaibleRemoteTcpPeer = await this.tcsConnection.Task.ConfigureAwait(false);
      return awaitaibleRemoteTcpPeer;
    }

    protected virtual void ConnectionEstablishedCallback(
      object sender,
      ConnectionEstablishedEventArgs e)
    {
      this.tcsConnection?.TrySetResult((IAwaitaibleRemoteTcpPeer) new AwaitaibleRemoteTcpPeer(e.RemoteTcpPeer, this.frameBufferBoundedCapacity));
    }

    protected virtual void ClientStoppedCallback(object sender, TcpClientStoppedEventArgs e)
    {
      switch (e.ClientStoppedReason)
      {
        case ClientStoppedReason.InitiatingConnectionTimeout:
          TaskCompletionSource<IAwaitaibleRemoteTcpPeer> tcsConnection1 = this.tcsConnection;
          if (tcsConnection1 == null)
            break;
          tcsConnection1.TrySetCanceled();
          break;
        case ClientStoppedReason.InitiatingConnectionFailure:
          TaskCompletionSource<IAwaitaibleRemoteTcpPeer> tcsConnection2 = this.tcsConnection;
          if (tcsConnection2 == null)
            break;
          tcsConnection2.TrySetException(e.Exception);
          break;
        case ClientStoppedReason.RuntimeException:
          TaskCompletionSource<IAwaitaibleRemoteTcpPeer> tcsConnection3 = this.tcsConnection;
          if (tcsConnection3 == null)
            break;
          tcsConnection3.TrySetException(e.Exception);
          break;
      }
    }
  }
}
