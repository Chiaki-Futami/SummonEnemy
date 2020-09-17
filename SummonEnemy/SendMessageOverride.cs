using HarmonyLib;
using Oc;
using System.Reflection;

namespace SummonEnemy
{
    [HarmonyPatch(typeof(OcUI_ChatHandler))]
    [HarmonyPatch("TrySendMessage")]
    class SendMessageOverride
    {
        static bool Prefix(string message)
        {

            bool isCommand = message.StartsWith("/");
            if (!isCommand) return true;

            var trimMessage = message.Trim();

            ClearInputField();

            switch (trimMessage)
            {
                case "/summon":
                    if (!Plugin.WindowState) 
                    {
                        Plugin.WindowState = true;
                    }

                    break;
            }

            return false;
        }

        static void ClearInputField()
        {
            OcUI_ChatHandler instance = SingletonMonoBehaviour<OcUI_ChatHandler>.Inst;
            MethodInfo endEnterMessage = instance.GetType().GetMethod("EndEnterMessage", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance);
            endEnterMessage.Invoke(instance, null);
        }
    }
}
