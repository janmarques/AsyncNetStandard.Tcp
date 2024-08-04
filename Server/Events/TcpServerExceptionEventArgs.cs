
// Type: AsyncNetStandard.Tcp.Server.Events.TcpServerExceptionEventArgsusing AsyncNetStandard.Core.Events;
using AsyncNetStandard.Core.Events;
using System;


namespace AsyncNetStandard.Tcp.Server.Events
{
  public class TcpServerExceptionEventArgs : ExceptionEventArgs
  {
    public TcpServerExceptionEventArgs(Exception ex)
      : base(ex)
    {
    }
  }
}
