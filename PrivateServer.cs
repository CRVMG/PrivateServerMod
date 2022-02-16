﻿using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using MelonLoader;
using Transmtn;
using UnhollowerBaseLib;
using VRC.Core;

namespace PrivateServer
{
    /// <summary>
    /// The main MelonMod, this is where all the magic happens.
    /// </summary>
    public class PrivateServer : MelonMod
    {
        public static MelonLogger.Instance Logger;
        public static MelonPreferences_Category PrivateServerPrefs;
        public static MelonPreferences_Entry<bool> PrivateServerEnabled;
        public static MelonPreferences_Entry<string> PrivateServerApiUrl;
        public static MelonPreferences_Entry<string> PrivateServerWebsocketUrl;
        public static MelonPreferences_Entry<string> PrivateServerNameServerHost;
        public static string ApiBaseUri = "api/1/"; // Base is required only by *some* methods in the game.

        #region Functions & Callbacks
        /// <summary>
        /// Enumerator that ensures that a callback happens on OnUiManagerInitCallback only after the UI manager
        /// has initialized. Thanks Davi!
        /// Sourced from: (https://discord.com/channels/439093693769711616/548545237123989505/854708903694958622)
        /// </summary>
        /// <returns></returns>
        static IEnumerator OnUiManagerInit()
        {
            while (VRCUiManager.prop_VRCUiManager_0 == null)
                yield return null;
            OnUiManagerInitCallback();
        }
        
        /// <summary>
        /// Callback for OnUiManagerInit; This ensures that the API url is set properly during the UI initialization.
        /// </summary>
        private static void OnUiManagerInitCallback()
        {
            API.SetApiUrl(PrivateServerApiUrl.Value + ApiBaseUri);
        }
        
        /// <summary>
        /// Called when the game starts. This method is used to initialize all MelonPreferences and Harmony patches.
        /// Additionally, it starts the OnUiManagerInit coroutine, which is used to properly set the API URL
        /// later on.
        /// </summary>
        public override void OnApplicationStart()
        {
            #region Variable Initialization
            PrivateServerPrefs = MelonPreferences.CreateCategory("PrivateServer");
            PrivateServerEnabled = PrivateServerPrefs.CreateEntry("Enabled", false);
            PrivateServerApiUrl = PrivateServerPrefs.CreateEntry("ApiUrl", "");
            PrivateServerWebsocketUrl = PrivateServerPrefs.CreateEntry("WebsocketUrl", "");
            PrivateServerNameServerHost = PrivateServerPrefs.CreateEntry("NameServerHost", "");
            
            Logger = LoggerInstance;
            #endregion

            if (!PrivateServerEnabled.Value) return;
            if (!(PrivateServerApiUrl.Value.ToLower().StartsWith("http://") ||
                  PrivateServerApiUrl.Value.ToLower().StartsWith("https://")))
            {
                Logger.Error("Invalid api url. Should start with `http://` or `https://`");
                return;
            }
            if (!(PrivateServerWebsocketUrl.Value.ToLower().StartsWith("ws://") ||
                  PrivateServerWebsocketUrl.Value.ToLower().StartsWith("wss://")))
            {
                Logger.Error("Invalid websocket url. Should start with `ws://` or `wss://`");
                return;
            }
            
            #region Harmony Patches
            // Photon LoadBalancingClient patches
            HarmonyInstance.Patch(typeof(VRCNetworkingClient).GetMethod("Method_Private_String_0"), GetPatch(
                "PatchGetNameServerAddress"));

            // SecurePlayerPrefs patches
            HarmonyInstance.Patch(typeof(SecurePlayerPrefs).GetMethod("HasKey"),
                GetPatch("PatchSecurePlayerPrefs"));
            HarmonyInstance.Patch(typeof(SecurePlayerPrefs).GetMethod("DeleteKey"),
                GetPatch("PatchSecurePlayerPrefs"));
            HarmonyInstance.Patch(typeof(SecurePlayerPrefs).GetMethod("SetString"),
                GetPatch("PatchSecurePlayerPrefs"));
            HarmonyInstance.Patch(typeof(SecurePlayerPrefs).GetMethods().First(x => x.GetParameters().Length == 2),
                GetPatch("PatchSecurePlayerPrefs"));
            HarmonyInstance.Patch(typeof(SecurePlayerPrefs).GetMethods().First(x => x.GetParameters().Length == 3),
                GetPatch("PatchSecurePlayerPrefs"));
            
            // Potentially deprecated: Websocket URI patch
            HarmonyInstance.Patch(typeof(WebSocketSharp.Ext).GetMethod("TryCreateWebSocketUri"),
                GetPatch("PatchTryCreateWebSocketUri"));
            #endregion
            
            DetourApiCtor();
            MelonCoroutines.Start(OnUiManagerInit());
            API.SetApiUrl(PrivateServerApiUrl.Value + ApiBaseUri);
        }

        /// <summary>
        /// Called after OnApplicationStart; This is simply done to ensure that the API url is set properly,
        /// and is likely redundant.
        /// </summary>
        public override void OnApplicationLateStart()
        {
            if (PrivateServerEnabled.Value)
                API.SetApiUrl(PrivateServerApiUrl.Value + ApiBaseUri);
        }
        #endregion
        
        #region Harmony Patches
        
        /// <summary>
        /// Gets a patch in the current class by the method's name.
        /// </summary>
        /// <param name="name">The name of the method the patch is contained in.</param>
        /// <returns>A HarmonyMethod that can be used as a patch.</returns>
        private static HarmonyMethod GetPatch(string name)
        {
            return new HarmonyMethod(typeof(PrivateServer).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic));
        }

        /// <summary>
        /// A patch for the WebsocketSharp.Ext.TryCreateWebSocketUri method to always return the websocket uri we have
        /// specified in our configuration.
        /// </summary>
        /// <param name="uriString"></param>
        [Obsolete("Method is *possibly* obsoleted by the Transmtn.Api patch.")]
        private static void PatchTryCreateWebSocketUri(ref string uriString)
        {
            uriString = PrivateServerWebsocketUrl.Value;
        }

        /// <summary>
        /// Patch to ensure that the Photon NameServer address is properly set.
        /// </summary>
        /// <param name="__result"></param>
        /// <returns></returns>
        private static bool PatchGetNameServerAddress(ref string __result)
        {
            __result = PrivateServerNameServerHost.Value;
            return false;
        }

        /// <summary>
        /// Adds a prefix to the SecurePlayerPrefs namespaces VRChat uses.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool PatchSecurePlayerPrefs(ref string key)
        {
            key = PrivateServerApiUrl.Value + "_" + key;
            return true;
        }

        #endregion
        
        #region D a n g e r (Native Hooks!)

        /// <summary>
        /// Delegate for the detour from Transmtn.Api.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ApiDelegate(IntPtr _this,
            IntPtr httpEndpoint, 
            IntPtr websocketEndpoint, 
            IntPtr auth, 
            IntPtr macAddress,
            IntPtr clientVersion,
            IntPtr platform,
            IntPtr defaultErrorResponse,
            IntPtr defaultLogResponse,
            IntPtr onReadyResponse,
            IntPtr onLostConnectionResponse,
            IntPtr nativeMethodInfo);
        private static ApiDelegate _apiDelegate;
        
        /// <summary>
        /// Detour method for Transmtn.Api - The patch for which can be found at PatchApiCtor
        /// Thanks to Bono & Ben for the help and guiding me through how native hooks work!
        /// </summary>
        private static unsafe void DetourApiCtor()
        {
            IntPtr apiConstructorOrig = *(IntPtr*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(typeof(Api).GetConstructors().First(x => x.GetParameters().Length > 6)).GetValue(null);
            MelonUtils.NativeHookAttach((IntPtr)(&apiConstructorOrig), typeof(PrivateServer).
                GetMethod(nameof(PatchApiCtor), BindingFlags.Static | BindingFlags.NonPublic)!.
                MethodHandle.GetFunctionPointer());
            _apiDelegate = Marshal.GetDelegateForFunctionPointer<ApiDelegate>(apiConstructorOrig);
        }

        /// <summary>
        /// Patch of the Transmtn.Api constructor.
        /// This patch ensures that when the constructor of Transmtn.Api is called and detoured, the http & websocket
        /// endpoints are rewritten prior to forwarding the call.
        /// </summary>
        private static void PatchApiCtor(IntPtr _this,
            IntPtr httpEndpoint, 
            IntPtr websocketEndpoint, 
            IntPtr auth, 
            IntPtr macAddress,
            IntPtr clientVersion,
            IntPtr platform,
            IntPtr defaultErrorResponse,
            IntPtr defaultLogResponse,
            IntPtr onReadyResponse,
            IntPtr onLostConnectionResponse,
            IntPtr nativeMethodInfo
            )
        {
            httpEndpoint = new Il2CppSystem.Uri(PrivateServerApiUrl.Value).Pointer;
            websocketEndpoint = new Il2CppSystem.Uri(PrivateServerWebsocketUrl.Value).Pointer;
            _apiDelegate(_this,
                httpEndpoint,
                websocketEndpoint,
                auth,
                macAddress,
                clientVersion,
                platform,
                defaultErrorResponse,
                defaultLogResponse,
                onReadyResponse,
                onLostConnectionResponse, nativeMethodInfo);
        }
        #endregion
    }
}