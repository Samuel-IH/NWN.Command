using System.Runtime.InteropServices;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Native.API;

namespace SamuelIH.Nwn.Command;

 public sealed class OnChatMessageSend : IEvent
    {
        public string? Message { get; internal init; }
        public NwObject? Sender { get; internal init; }
        
        public bool Skip { get; set; }

        NwObject? IEvent.Context => Sender;

        internal sealed unsafe class Factory : HookEventFactory
        {
            [NativeFunction("_ZN11CNWSMessage29SendServerToPlayerChatMessageEhj10CExoStringjRKS0_", "?SendServerToPlayerChatMessage@CNWSMessage@@QEAAHEIVCExoString@@IAEBV2@@Z")]
            private delegate int SendServerToPlayerChatMessageFunction(void* pMessage, byte nChatMessageType, uint oidSpeaker, void* sSpeakerMessage, uint nTellPlayerId, void* tellName);
            
            private static FunctionHook<SendServerToPlayerChatMessageFunction> SendServerToPlayerChatMessageHook { get; set; } = null!;

            protected override IDisposable[] RequestHooks()
            {
                delegate* unmanaged<void*, byte, uint, void*, uint, void*, int> pHook = &OnChatMessageSend;
                SendServerToPlayerChatMessageHook = HookService.RequestHook<SendServerToPlayerChatMessageFunction>(pHook, HookOrder.Late);
                return new IDisposable[] { SendServerToPlayerChatMessageHook };
            }

            [UnmanagedCallersOnly]
            private static int OnChatMessageSend(void* pMessage, byte nChatMessageType, uint oidSpeaker, void* sSpeakerMessage, uint nTellPlayerId, void* tellName)
            {
                var speakerMessage = CExoString.FromPointer(sSpeakerMessage);
                var speaker = oidSpeaker.ToNwObject()!;
                
                var eventData = ProcessEvent(EventCallbackType.Before, new OnChatMessageSend()
                {
                    Message = speakerMessage.ToString(),
                    Sender = speaker
                }, executeInScriptContext: false);
                
                return eventData.Skip ? false.ToInt() : SendServerToPlayerChatMessageHook.CallOriginal(pMessage, nChatMessageType, oidSpeaker, sSpeakerMessage, nTellPlayerId, tellName);
            }
        }
    }