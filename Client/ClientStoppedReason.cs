
// Type: AsyncNetStandard.Tcp.Client.ClientStoppedReason
namespace AsyncNetStandard.Tcp.Client
{
  public enum ClientStoppedReason
  {
    InitiatingConnectionTimeout = 0,
    InitiatingConnectionFailure = 1,
    Disconnected = 3,
    RuntimeException = 4,
  }
}
