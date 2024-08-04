# AsyncNetStandard.Tcp

Package taken from https://www.nuget.org/packages/AsyncNetStandard.Tcp (MIT License), which was unlisted.

The original source code was unlisted as well ( https://github.com/bartlomiej-stys/AsyncNet ).

Dropped the .NET Framework support and republished on NuGet: https://www.nuget.org/packages/AsyncNetStandard.Tcp

## Usage
// TODO

```
using AsyncNetStandard.Tcp.Server;
_client = new AsyncNetTcpServer(_port);
_client.FrameArrived += async (s, e) => await ProcessPacket(e.FrameData, e.RemoteTcpPeer);
_task = Task.Run(async () => await _client.StartAsync(_cancellationToken), _cancelationTokenSource.Token);


await peer.SendAsync(sendBytes);
```