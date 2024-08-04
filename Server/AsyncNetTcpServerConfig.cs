
// Type: AsyncNet.Tcp.Server.AsyncNetTcpServerConfigusing AsyncNetStandard.Tcp.Defragmentation;
using AsyncNetStandard.Tcp.Defragmentation;
using AsyncNetStandard.Tcp.Remote;
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;


namespace AsyncNetStandard.Tcp.Server
{
  public class AsyncNetTcpServerConfig
  {
    public Func<IRemoteTcpPeer, IProtocolFrameDefragmenter> ProtocolFrameDefragmenterFactory { get; set; } = (Func<IRemoteTcpPeer, IProtocolFrameDefragmenter>) (_ => (IProtocolFrameDefragmenter) MixedDefragmenter.Default);

    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.Zero;

    public int MaxSendQueuePerPeerSize { get; set; } = -1;

    public IPAddress IPAddress { get; set; } = IPAddress.Any;

    public int Port { get; set; }

    public Action<TcpListener> ConfigureTcpListenerCallback { get; set; }

    public bool UseSsl { get; set; }

    public X509Certificate X509Certificate { get; set; }

    public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; } = (RemoteCertificateValidationCallback) ((_, __, ___, ____) => true);

    public EncryptionPolicy EncryptionPolicy { get; set; }

    public Func<TcpClient, bool> ClientCertificateRequiredCallback { get; set; } = (Func<TcpClient, bool>) (_ => false);

    public Func<TcpClient, bool> CheckCertificateRevocationCallback { get; set; } = (Func<TcpClient, bool>) (_ => false);

    public SslProtocols EnabledProtocols { get; set; } = SslProtocols.Default;
  }
}
