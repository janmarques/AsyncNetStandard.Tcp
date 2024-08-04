# AsyncNetStandard.Tcp

Package taken from https://www.nuget.org/packages/AsyncNet.Tcp (MIT License), which was unlisted.

The original source code was unlisted as well ( https://github.com/bartlomiej-stys/AsyncNet ).

Dropped the .NET Framework support and republished

## Usage
// TODO

```
using AsyncNetStandard.Tcp.Server;
_client = new AsyncNetTcpServer(_port);
_client.FrameArrived += async (s, e) => await ProcessPacket(e.FrameData, e.RemoteTcpPeer);

await peer.SendAsync(sendBytes);
```