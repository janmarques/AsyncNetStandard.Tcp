
// Type: AsyncNetStandard.Tcp.Defragmentation.IProtocolFrameDefragmenterusing AsyncNetStandard.Tcp.Remote;
using AsyncNetStandard.Tcp.Remote;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Tcp.Defragmentation
{
  /// <summary>
  /// An interface for protocol frame defragmenter. You can implement this interface to support any defragmentation / deframing mechanism
  /// </summary>
  public interface IProtocolFrameDefragmenter
  {
    /// <summary>Reads one frame from the stream</summary>
    /// <param name="remoteTcpPeer">Remote peer</param>
    /// <param name="leftOvers">Any left overs from previous call or null</param>
    /// <param name="cancellationToken">Cancellation token for cancelling this operation</param>
    /// <returns>Frame result</returns>
    Task<ReadFrameResult> ReadFrameAsync(
      IRemoteTcpPeer remoteTcpPeer,
      byte[] leftOvers,
      CancellationToken cancellationToken);
  }
}
