using System.Runtime.InteropServices;
using UnityEngine;

// Phone/tablet detection that also works in mobile browsers (WebGL), where
// iPadOS Safari pretends to be a desktop Mac.
public static class DeviceInfo
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern int SalaIsMobileBrowser();
    [DllImport("__Internal")] static extern int SalaIsIOS();
    static int mobile = -1, ios = -1;
    public static bool IsMobile => (mobile < 0 ? mobile = (SalaIsMobileBrowser() == 1 || Application.isMobilePlatform) ? 1 : 0 : mobile) == 1;
    public static bool IsIOS => (ios < 0 ? ios = SalaIsIOS() : ios) == 1;
#else
    public static bool IsMobile => Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
    public static bool IsIOS => Application.platform == RuntimePlatform.IPhonePlayer;
#endif
}
