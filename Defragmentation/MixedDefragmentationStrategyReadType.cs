
// Type: AsyncNet.Tcp.Defragmentation.MixedDefragmentationStrategyReadType
namespace AsyncNetStandard.Tcp.Defragmentation
{
  /// <summary>Read type for mixed defragmentation strategy</summary>
  public enum MixedDefragmentationStrategyReadType
  {
    /// <summary>
    /// Reads as many bytes as there is in underlying receive buffer before proceeding
    /// </summary>
    ReadDefault,
    /// <summary>Reads until data in buffer has specified length</summary>
    ReadFully,
    /// <summary>Reads until new data in buffer has specified length</summary>
    ReadNewFully,
  }
}
