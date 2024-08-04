
// Type: AsyncNetStandard.Tcp.Defragmentation.IProtocolFrameDefragmenter`1
namespace AsyncNetStandard.Tcp.Defragmentation
{
  /// <summary>
  /// An interface for protocol frame defragmenter. You can implement this interface to support any defragmentation / deframing mechanism
  /// </summary>
  /// <typeparam name="TStrategy"></typeparam>
  public interface IProtocolFrameDefragmenter<TStrategy> : IProtocolFrameDefragmenter
  {
    /// <summary>Current defragmentation strategy</summary>
    TStrategy DefragmentationStrategy { get; set; }
  }
}
