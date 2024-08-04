
// Type: AsyncNetStandard.Tcp.Defragmentation.DefaultProtocolFrameLengthPrefixedDefragmentationStrategy
namespace AsyncNetStandard.Tcp.Defragmentation
{
  public class DefaultProtocolFrameLengthPrefixedDefragmentationStrategy : 
    ILengthPrefixedDefragmentationStrategy
  {
    public virtual int FrameHeaderLength { get; protected set; } = 2;

    public virtual int GetFrameLength(byte[] data)
    {
      return data.Length < 2 ? 0 : (int) data[0] << 8 | (int) data[1];
    }
  }
}
