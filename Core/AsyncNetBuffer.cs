
// Type: AsyncNet.Core.AsyncNetBuffer
// Assembly: AsyncNet.Core, Version=1.2.4.0, Culture=neutral, PublicKeyToken=3f4900b9c5b8c297
// MVID: F4D263BE-F9A6-48FD-A5F5-96E8D9DF4B57
// Assembly location: C:\Users\pc2020\AppData\Local\Temp\Sibazez\907aa7090e\lib\netstandard2.0\AsyncNet.Core.dll
// XML documentation location: C:\Users\pc2020\AppData\Local\Temp\Sibazez\907aa7090e\lib\netstandard2.0\AsyncNet.Core.xml

using System;


namespace AsyncNetStandard.Core
{
  public class AsyncNetBuffer
  {
    public AsyncNetBuffer(byte[] memory, int offset, int count)
    {
      this.Memory = memory;
      this.Offset = offset;
      this.Count = count;
    }

    public byte[] Memory { get; }

    public int Offset { get; }

    public int Count { get; }

    public byte[] ToBytes()
    {
      if (this.Offset == 0 && this.Count == this.Memory.Length)
        return this.Memory;
      byte[] destinationArray = new byte[this.Count];
      Array.Copy((Array) this.Memory, this.Offset, (Array) destinationArray, 0, this.Count);
      return destinationArray;
    }
  }
}
