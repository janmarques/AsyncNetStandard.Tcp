
// Type: AsyncNet.Core.Events.ExceptionEventArgs
// Assembly: AsyncNet.Core, Version=1.2.4.0, Culture=neutral, PublicKeyToken=3f4900b9c5b8c297
// MVID: F4D263BE-F9A6-48FD-A5F5-96E8D9DF4B57
// Assembly location: C:\Users\pc2020\AppData\Local\Temp\Sibazez\907aa7090e\lib\netstandard2.0\AsyncNet.Core.dll
// XML documentation location: C:\Users\pc2020\AppData\Local\Temp\Sibazez\907aa7090e\lib\netstandard2.0\AsyncNet.Core.xml

using System;


namespace AsyncNetStandard.Core.Events
{
  public class ExceptionEventArgs : EventArgs
  {
    public ExceptionEventArgs(Exception ex) => this.Exception = ex;

    public Exception Exception { get; }
  }
}
