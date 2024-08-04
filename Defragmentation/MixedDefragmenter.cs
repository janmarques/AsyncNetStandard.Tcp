
// Type: AsyncNetStandard.Tcp.Defragmentation.MixedDefragmenterusing AsyncNetStandard.Core.Extensions;
using AsyncNetStandard.Core.Extensions;
using AsyncNetStandard.Tcp.Remote;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace AsyncNetStandard.Tcp.Defragmentation
{
  /// <summary>Mixed protocol frame defragmenter</summary>
  public class MixedDefragmenter : 
    IProtocolFrameDefragmenter<IMixedDefragmentationStrategy>,
    IProtocolFrameDefragmenter
  {
    private static readonly byte[] emptyArray = new byte[0];
    private static readonly Lazy<MixedDefragmenter> @default = new Lazy<MixedDefragmenter>((Func<MixedDefragmenter>) (() => new MixedDefragmenter((IMixedDefragmentationStrategy) new DefaultProtocolFrameMixedDefragmentationStrategy())));

    /// <summary>
    /// Default mixed defragmenter using <see cref="T:AsyncNetStandard.Tcp.Defragmentation.DefaultProtocolFrameMixedDefragmentationStrategy" />
    /// </summary>
    public static MixedDefragmenter Default => MixedDefragmenter.@default.Value;

    /// <summary>Current mixed defragmentation strategy</summary>
    public virtual IMixedDefragmentationStrategy DefragmentationStrategy { get; set; }

    /// <summary>
    /// Constructs mixed frame defragmenter that is using <paramref name="strategy" /> for defragmentation strategy
    /// </summary>
    /// <param name="strategy"></param>
    public MixedDefragmenter(IMixedDefragmentationStrategy strategy)
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
      int frameLength = 0;
      byte[] numArray = leftOvers;
      int dataLength = numArray != null ? numArray.Length : 0;
      byte[] frameBuffer = leftOvers = leftOvers ?? MixedDefragmenter.emptyArray;
      if (dataLength > 0)
        frameLength = this.DefragmentationStrategy.GetFrameLength(frameBuffer, dataLength);
      while (frameLength == 0)
      {
        frameBuffer = this.DefragmentationStrategy.ReadType != MixedDefragmentationStrategyReadType.ReadFully ? new byte[dataLength + this.DefragmentationStrategy.ReadBufferLength] : (this.DefragmentationStrategy.ReadBufferLength <= dataLength ? new byte[dataLength] : new byte[this.DefragmentationStrategy.ReadBufferLength]);
        if (dataLength > 0)
          Array.Copy((Array) leftOvers, 0, (Array) frameBuffer, 0, dataLength);
        leftOvers = frameBuffer;
        int num;
        if (this.DefragmentationStrategy.ReadType == MixedDefragmentationStrategyReadType.ReadNewFully)
          num = await StreamExtensions.ReadUntilBufferIsFullAsync(remoteTcpPeer.TcpStream, frameBuffer, dataLength, this.DefragmentationStrategy.ReadBufferLength, cancellationToken).ConfigureAwait(false) ? this.DefragmentationStrategy.ReadBufferLength : 0;
        else if (this.DefragmentationStrategy.ReadType == MixedDefragmentationStrategyReadType.ReadFully)
          num = await StreamExtensions.ReadUntilBufferIsFullAsync(remoteTcpPeer.TcpStream, frameBuffer, 0, this.DefragmentationStrategy.ReadBufferLength, cancellationToken).ConfigureAwait(false) ? this.DefragmentationStrategy.ReadBufferLength : 0;
        else
          num = await StreamExtensions.ReadWithRealCancellationAsync(remoteTcpPeer.TcpStream, frameBuffer, dataLength, this.DefragmentationStrategy.ReadBufferLength, cancellationToken).ConfigureAwait(false);
        if (num < 1)
          return ReadFrameResult.StreamClosedResult;
        dataLength += num;
        frameLength = this.DefragmentationStrategy.GetFrameLength(frameBuffer, dataLength);
        if (frameLength < 0)
          return ReadFrameResult.FrameDroppedResult;
      }
      if (dataLength < frameLength)
      {
        if (frameBuffer.Length < frameLength)
        {
          frameBuffer = new byte[frameLength];
          Array.Copy((Array) leftOvers, 0, (Array) frameBuffer, 0, dataLength);
        }
        if (!await StreamExtensions.ReadUntilBufferIsFullAsync(remoteTcpPeer.TcpStream, frameBuffer, dataLength, frameLength - dataLength, cancellationToken).ConfigureAwait(false))
          return ReadFrameResult.StreamClosedResult;
        dataLength = frameLength;
        leftOvers = (byte[]) null;
      }
      else if (dataLength > frameLength)
      {
        byte[] destinationArray = new byte[frameLength];
        int length = dataLength - frameLength;
        leftOvers = new byte[length];
        Array.Copy((Array) frameBuffer, 0, (Array) destinationArray, 0, frameLength);
        Array.Copy((Array) frameBuffer, frameLength, (Array) leftOvers, 0, length);
        frameBuffer = destinationArray;
      }
      else
      {
        if (frameBuffer.Length > dataLength)
        {
          frameBuffer = new byte[dataLength];
          Array.Copy((Array) leftOvers, 0, (Array) frameBuffer, 0, dataLength);
        }
        leftOvers = (byte[]) null;
      }
      return new ReadFrameResult(frameBuffer, leftOvers);
    }
  }
}
