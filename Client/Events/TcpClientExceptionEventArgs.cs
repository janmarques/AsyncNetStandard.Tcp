using AsyncNetStandard.Core.Events;
using System;


namespace AsyncNetStandard.Tcp.Client.Events
{
  public class TcpClientExceptionEventArgs : ExceptionEventArgs
  {
    public TcpClientExceptionEventArgs(Exception ex)
      : base(ex)
    {
    }
  }
}
