using System;
using System.Threading.Tasks;
using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Interfaces;

namespace UnitTest.Processors;

[Processor("test.broadcastereceiver")]
class TestProcessorTplBroadcastReceiver : ProcessorTplBase
{
    public override string[] Pushes => null;
    public bool IsBroadcasted = false;
    public uint ClientId;
    public int EventId;
    public object Data;

    public override async Task OnBroadcast(uint clientId, int eventId, object data)
    {
        IsBroadcasted = true;
        ClientId = clientId;
        EventId = eventId;
        Data = data;
    }
}