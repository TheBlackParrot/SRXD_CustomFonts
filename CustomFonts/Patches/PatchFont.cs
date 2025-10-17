using HarmonyLib;
using TMPro;

namespace CustomFonts.Patches;

internal abstract class PatcherFunctions
{
    private static FontAssetSystem? _fontAssetSystemInstance;
    public static void Patch(TMP_Text instance)
    {
        if (!Plugin.FontsLoaded)
        {
            return;
        }
        
        // this (should) exist by the time this is called for the first time
        _fontAssetSystemInstance ??= GameSystemSingleton<FontAssetSystem, FontAssetSystemSettings>.Instance;
        
        FontAssetSystemSettings.FontForName? fontForName = _fontAssetSystemInstance?.GetFontForName("Oxanium-Medium", false);
        instance.font = fontForName?.font;
        
        // remove italics (i hate italics)
        if ((instance.fontStyle & FontStyles.Italic) == FontStyles.Italic)
        {
            instance.fontStyle ^= FontStyles.Italic;
        }
    }
}

[HarmonyPatch]
public static class Patches
{
    // this is very aggressive
    [HarmonyPatch(typeof(TMP_Text), "text", MethodType.Setter)]
    [HarmonyPrefix]
    [HarmonyPriority(int.MinValue)]
    // ReSharper disable once InconsistentNaming
    internal static bool TMP_Text_textSetter(TMP_Text __instance)
    {
        if (!__instance.isActiveAndEnabled)
        {
            return true;
        }
        
        PatcherFunctions.Patch(__instance);
        
        return true;
    }

    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    [HarmonyPrefix]
    [HarmonyPriority(int.MinValue)]
    // ReSharper disable once InconsistentNaming
    internal static bool TextMeshProUGUI_OnEnable(TextMeshProUGUI __instance)
    {
        PatcherFunctions.Patch(__instance);
        return true;
    }
    
    // uncommenting this patch will garble the HUD numbers
    // also it's very aggressive
    /*
    [HarmonyPatch(typeof(TextNumber), nameof(TextNumber.Update))]
    [HarmonyPrefix]
    [HarmonyPriority(int.MinValue)]
    // ReSharper disable once InconsistentNaming
    internal static bool TextNumber_fontGetter(ref string ___font)
    {
        ___font = Plugin.LoadedFonts[0];
        return true;
    }
    */
}