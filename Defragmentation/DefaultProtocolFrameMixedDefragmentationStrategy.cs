
// Type: AsyncNetStandard.Tcp.Defragmentation.DefaultProtocolFrameMixedDefragmentationStrategy
namespace AsyncNetStandard.Tcp.Defragmentation
{
  public class DefaultProtocolFrameMixedDefragmentationStrategy : IMixedDefragmentationStrategy
  {
    public virtual int ReadBufferLength { get; protected set; } = 4096;

    public virtual MixedDefragmentationStrategyReadType ReadType { get; protected set; }

    public virtual int GetFrameLength(byte[] buffer, int dataLength) => dataLength;
  }
}
