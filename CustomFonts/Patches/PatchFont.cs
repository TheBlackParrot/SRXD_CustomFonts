using HarmonyLib;
using TMPro;

namespace CustomFonts.Patches;

internal abstract class PatcherFunctions
{
    private static FontAssetSystem? _fontAssetSystemInstance;
    private static FontAssetSystemSettings.FontForName? _currentFont;

    public static void UpdateCurrentFont()
    {
        // this (should) exist by the time this is called for the first time
        _fontAssetSystemInstance ??= GameSystemSingleton<FontAssetSystem, FontAssetSystemSettings>.Instance;
        
        _currentFont = _fontAssetSystemInstance?.GetFontForName($"{Plugin.FontFamily.Value}-{Plugin.FontWeight.Value}", false);
    }
    
    public static void Patch(TMP_Text instance)
    {
        if (instance is CustomTextMeshPro { fontName: "DefaultHud3D" or "NumberHudWheel" })
        {
            return;
        }
        
        if (_currentFont == null)
        {
            UpdateCurrentFont();
        }

        instance.font = _currentFont?.font;
        if (instance is CustomTextMeshProUGUI ugui)
        {
            ugui.FontName = _currentFont?.name;
            ugui.UpdateFontAsset();
        }

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
    
    [HarmonyPatch(typeof(CustomTextMeshProUGUI), "OnEnable")]
    [HarmonyPrefix]
    [HarmonyPriority(int.MinValue)]
    // ReSharper disable once InconsistentNaming
    internal static bool CustomTextMeshProUGUI_OnEnable(CustomTextMeshProUGUI __instance)
    {
        PatcherFunctions.Patch(__instance);
        return true;
    }
}