using AsyncNetStandard.Tcp.Defragmentation;
using AsyncNetStandard.Tcp.Remote;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;


namespace AsyncNetStandard.Tcp.Client
{
  public class AsyncNetTcpClientConfig
  {
    public Func<IRemoteTcpPeer, IProtocolFrameDefragmenter> ProtocolFrameDefragmenterFactory { get; set; } = (Func<IRemoteTcpPeer, IProtocolFrameDefragmenter>) (_ => (IProtocolFrameDefragmenter) MixedDefragmenter.Default);

    public string TargetHostname { get; set; }

    public int TargetPort { get; set; }

    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.Zero;

    public int MaxSendQueueSize { get; set; } = -1;

    public Action<TcpClient> ConfigureTcpClientCallback { get; set; }

    public Func<IPAddress[], IEnumerable<IPAddress>> FilterResolvedIpAddressListForConnectionCallback { get; set; }

    public bool UseSsl { get; set; }

    public IEnumerable<X509Certificate> X509ClientCertificates { get; set; }

    public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; } = (RemoteCertificateValidationCallback) ((_, __, ___, ____) => true);

    public LocalCertificateSelectionCallback LocalCertificateSelectionCallback { get; set; }

    public EncryptionPolicy EncryptionPolicy { get; set; }

    public bool CheckCertificateRevocation { get; set; }

    public SslProtocols EnabledProtocols { get; set; } = SslProtocols.Default;
  }
}
