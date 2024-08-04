
// Type: AsyncNet.Tcp.Defragmentation.LengthPrefixedDefragmenterusing AsyncNetStandard.Core.Extensions;
using AsyncNetStandard.Core.Extensions;
using AsyncNetStandard.Tcp.Remote;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Tcp.Defragmentation
{
  /// <summary>Length prefixed protocol frame defragmenter</summary>
  public class LengthPrefixedDefragmenter : 
    IProtocolFrameDefragmenter<ILengthPrefixedDefragmentationStrategy>,
    IProtocolFrameDefragmenter
  {
    private static readonly Lazy<LengthPrefixedDefragmenter> @default = new Lazy<LengthPrefixedDefragmenter>((Func<LengthPrefixedDefragmenter>) (() => new LengthPrefixedDefragmenter((ILengthPrefixedDefragmentationStrategy) new DefaultProtocolFrameLengthPrefixedDefragmentationStrategy())));

    /// <summary>
    /// Default length prefixed defragmenter using <see cref="T:AsyncNet.Tcp.Defragmentation.DefaultProtocolFrameLengthPrefixedDefragmentationStrategy" />
    /// </summary>
    public static LengthPrefixedDefragmenter Default => LengthPrefixedDefragmenter.@default.Value;

    /// <summary>Current length prefixed defragmentation strategy</summary>
    public virtual ILengthPrefixedDefragmentationStrategy DefragmentationStrategy { get; set; }

    /// <summary>
    /// Constructs length prefixed defragmenter that is using <paramref name="strategy" /> for defragmentation strategy
    /// </summary>
    /// <param name="strategy"></param>
    public LengthPrefixedDefragmenter(ILengthPrefixedDefragmentationStrategy strategy)
    {
      this.DefragmentationStrategy = strategy;
    }

    /// <summary>Reads one frame from the stream</summary>
    /// <param name="remoteTcpPeer">Remote peer</param>
    /// <param name="leftOvers">Any left overs from previous call or null</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Frame result</returns>
    public virtual async Task<ReadFrameResult> ReadFrameAsync(
      IRemoteTcpPeer remoteTcpPeer,
      byte[] leftOvers,
      CancellationToken cancellationToken)
    {
      byte[] readBuffer = new byte[this.DefragmentationStrategy.FrameHeaderLength];
      ConfiguredTaskAwaitable<bool> configuredTaskAwaitable = StreamExtensions.ReadUntilBufferIsFullAsync(remoteTcpPeer.TcpStream, readBuffer, 0, readBuffer.Length, cancellationToken).ConfigureAwait(false);
      if (!await configuredTaskAwaitable)
        return ReadFrameResult.StreamClosedResult;
      int frameLength = this.DefragmentationStrategy.GetFrameLength(readBuffer);
      if (frameLength < 1)
        return ReadFrameResult.FrameDroppedResult;
      byte[] frameBuffer = new byte[frameLength];
      Array.Copy((Array) readBuffer, 0, (Array) frameBuffer, 0, readBuffer.Length);
      configuredTaskAwaitable = StreamExtensions.ReadUntilBufferIsFullAsync(remoteTcpPeer.TcpStream, frameBuffer, readBuffer.Length, frameLength - readBuffer.Length, cancellationToken).ConfigureAwait(false);
      return await configuredTaskAwaitable ? new ReadFrameResult(frameBuffer) : ReadFrameResult.StreamClosedResult;
    }
  }
}
