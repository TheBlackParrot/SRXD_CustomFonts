using HarmonyLib;
using TMPro;

namespace CustomFonts.Patches;

internal abstract class PatcherFunctions
{
    private static FontAssetSystem? _fontAssetSystemInstance;
    internal static FontAssetSystemSettings.FontForName? CurrentFont;

    public static void UpdateCurrentFont()
    {
        // this (should) exist by the time this is called for the first time
        _fontAssetSystemInstance ??= GameSystemSingleton<FontAssetSystem, FontAssetSystemSettings>.Instance;
        
        CurrentFont = _fontAssetSystemInstance?.GetFontForName($"{Plugin.FontFamily.Value}-{Plugin.FontWeight.Value}", false);
    }
    
    public static void Patch(TMP_Text instance)
    {
        if (CurrentFont == null)
        {
            UpdateCurrentFont();
        }
        
        instance.font = CurrentFont?.font;
        
        // remove italics (i hate italics)
        if ((instance.fontStyle & FontStyles.Italic) == FontStyles.Italic && Plugin.DisableItalics.Value)
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
}