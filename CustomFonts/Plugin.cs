using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using CustomFonts.Patches;
using HarmonyLib;
using SpinCore.Translation;
using TMPro;
using UnityEngine;

namespace CustomFonts;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("srxd.raoul1808.spincore", "1.1.2")]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log = null!;
    private static Harmony _harmony = null!;

    private void Awake()
    {
        Log = Logger;
        
        TranslationHelper.AddTranslation($"{nameof(CustomFonts)}_ModName", nameof(CustomFonts));
        TranslationHelper.AddTranslation($"{nameof(CustomFonts)}_GitHubButtonText", $"{nameof(CustomFonts)} Releases (GitHub)");
        TranslationHelper.AddTranslation($"{nameof(CustomFonts)}_ApplyButtonText", "Apply Font");
        TranslationHelper.AddTranslation($"{nameof(CustomFonts)}_{nameof(FontFamily)}", "Font family");
        TranslationHelper.AddTranslation($"{nameof(CustomFonts)}_{nameof(FontWeight)}", "Font weight");
        TranslationHelper.AddTranslation($"{nameof(CustomFonts)}_{nameof(DisableItalics)}", "Forcibly disable italics");
        
        RegisterConfigEntries();
        CreateModPage();
        
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        Log.LogInfo("Plugin loaded");
    }

    private static async Task LoadAllFontVariants()
    {
        await LoadCustomFont();
        await LoadCustomFont("Outline at 40 Material");
        await LoadCustomFont("Outline at 40 Material Always On Top");
        await LoadCustomFont("Outline at 90 Material");
    }

    private void OnEnable()
    {
        Task.Run(async () =>
        {
            try
            {
                await Awaitable.MainThreadAsync();
                await LoadAllFontVariants();
                _harmony.PatchAll();
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
        });
    }

    private void OnDisable()
    {
        _harmony.UnpatchSelf();
    }

    private static FontAssetSystem? _fontAssetSystem;
    private static async Task<TMP_FontAsset> LoadSystemFont(string family, string weight, string? variant = null)
    {
        FixCreateFontAssetInstance.WantedVariant = variant ?? string.Empty;
        Log.LogInfo($"Loading system font: {family} ({weight}) (variant: {FixCreateFontAssetInstance.WantedVariant})");
        
        FontAssetCreationSettings? settings = null;
        while (settings == null)
        {
            try
            {
                settings = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                    .First(asset => asset.name == "Montserrat-ExtraBold SDF").creationSettings;
            }
            catch (InvalidOperationException)
            {
                // ignored
            }
            
            await Awaitable.NextFrameAsync();
        }
        
        Log.LogInfo("Creating font asset...");
        
        TMP_FontAsset? font = TMP_FontAsset.CreateFontAsset(family, weight, settings.Value.pointSize);
        if (font == null)
        {
            Log.LogWarning($"Failed to load font: {family} ({weight})");
            font = TMP_FontAsset.CreateFontAsset("Arial", "Regular", settings.Value.pointSize);
            
            family = "Arial";
            weight = "Regular";
        }
        font.name = $"{family}-{weight}{(variant == null ? string.Empty : $"-{FixCreateFontAssetInstance.WantedVariant}")}";

        while (_fontAssetSystem?.defaultEmpty == null)
        {
            await Awaitable.NextFrameAsync();
        }
        
        font.fallbackFontAssets = [_fontAssetSystem.defaultEmpty.font];
        font.fallbackFontAssetTable = font.fallbackFontAssets;

        return font;
    }
    
    private static readonly MethodInfo? OriginalAssetMethod = typeof(TMP_FontAsset)
        .GetMethod(nameof(TMP_FontAsset.CreateFontAssetInstance), BindingFlags.NonPublic | BindingFlags.Static)?.GetBaseDefinition();
    private static readonly MethodInfo? NewAssetMethod = typeof(FixCreateFontAssetInstance)
        .GetMethod(nameof(FixCreateFontAssetInstance.CreateFontAssetInstance), BindingFlags.NonPublic | BindingFlags.Static)?.GetBaseDefinition();
    private static async Task LoadCustomFont(string? variant = null)
    {
        string fullFontName = $"{FontFamily.Value}-{FontWeight.Value}{(variant == null ? string.Empty : $"-{variant}")}";
        
        while (_fontAssetSystem == null)
        {
            _fontAssetSystem = GameSystemSingleton<FontAssetSystem, FontAssetSystemSettings>.Instance;
            await Awaitable.NextFrameAsync();
        }

        // no, please, take your time
        while (!_fontAssetSystem.HasAppliedSettings)
        {
            await Awaitable.NextFrameAsync();
        }

        if (_fontAssetSystem.Fonts.ContainsKey(fullFontName))
        {
            // already loaded, don't bother
            PatcherFunctions.UpdateCurrentFont();
            return;
        }
        
        // i specifically only want this particular patch to run once, since it works around a missing shader issue in base game
        if (OriginalAssetMethod == null)
        {
            Log.LogInfo("originalMethod null");
            return;
        }
        if (NewAssetMethod == null)
        {
            Log.LogInfo("newMethod null");
            return;
        }

        _harmony.Patch(OriginalAssetMethod, new HarmonyMethod(NewAssetMethod));
        
        TMP_FontAsset? loadedFontAsset = null;
        await Awaitable.MainThreadAsync();
        try
        {
            loadedFontAsset = await LoadSystemFont(FontFamily.Value, FontWeight.Value, variant);
        }
        catch (Exception e)
        {
            Log.LogError(e);
        }

        if (loadedFontAsset == null)
        {
            throw new NullReferenceException();
        }
        
        // think there's some steps missing here, worry about this later
        // it looks? like? it properly generates the atlas for the HUD numbers but it doesn't? seem to update? where things are? idk
        FontAssetSystemSettings.FontForName ffn = new()
        {
            font = loadedFontAsset,
            name = fullFontName
        };
        ffn.Init();
        ffn.GetNumberMaterials();
        
        FontAssetSystem.AddFont(ffn, _fontAssetSystem.Fonts, ref _fontAssetSystem.defaultFont);

        PatcherFunctions.UpdateCurrentFont();
        
        Log.LogInfo("Font loading complete");
        
        // ok we no longer need the workaround :)
        _harmony.Unpatch(OriginalAssetMethod, HarmonyPatchType.Prefix, MyPluginInfo.PLUGIN_GUID);
    }
}