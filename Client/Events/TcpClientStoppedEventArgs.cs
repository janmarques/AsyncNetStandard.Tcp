using AsyncNetStandard.Tcp.Connection;
using System;


namespace AsyncNetStandard.Tcp.Client.Events
{
  public class TcpClientStoppedEventArgs : EventArgs
  {
    public ClientStoppedReason ClientStoppedReason { get; set; }

    public Exception Exception { get; set; }

    public ConnectionCloseReason ConnectionCloseReason { get; set; }
  }
}
