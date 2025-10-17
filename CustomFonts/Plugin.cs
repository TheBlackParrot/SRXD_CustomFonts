using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using CustomFonts.Patches;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace CustomFonts;

/*
 a lot of this is probably a combination of:
 - spicy
 - bad
 - ugly
 - disgusting
 - inefficient
 - what
 
 take your pick
*/

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public partial class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log = null!;
    private static Harmony _harmony = null!;
    private static string DataPath => Path.Combine(Paths.ConfigPath, nameof(CustomFonts));

    private void Awake()
    {
        Log = Logger;

        if (!Directory.Exists(DataPath))
        {
            Directory.CreateDirectory(DataPath);
        }
        
        RegisterConfigEntries();
        CreateModPage();

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        Log.LogInfo("Plugin loaded");
    }

    private void OnEnable()
    {
        Task.Run(async () =>
        {
            try
            {
                await Awaitable.MainThreadAsync();
                await LoadCustomFont();
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
    private static async Task<TMP_FontAsset> LoadSystemFont(string family, string weight)
    {
        Log.LogInfo($"Loading system font: {family} ({weight})");
        
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
        
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(family, weight, settings.Value.pointSize);
        font.name = $"{family}-{weight}";
        
        while (_fontAssetSystem == null)
        {
            _fontAssetSystem = GameSystemSingleton<FontAssetSystem, FontAssetSystemSettings>.Instance;
            await Awaitable.NextFrameAsync();
        }

        while (_fontAssetSystem.defaultEmpty == null)
        {
            await Awaitable.NextFrameAsync();
        }
        
        font.fallbackFontAssets = [_fontAssetSystem.defaultEmpty.font];
        font.fallbackFontAssetTable = font.fallbackFontAssets;

        return font;
    }

    private static async Task LoadCustomFont()
    {
        string fullFontName = $"{FontFamily.Value}-{FontWeight.Value}";
        
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
            return;
        }
        
        // i specifically only want this particular patch to run once, since it works around a missing shader issue in base game
        // just get the stuff now
        MethodInfo? originalAssetMethod = typeof(TMP_FontAsset)
            .GetMethod(nameof(TMP_FontAsset.CreateFontAssetInstance), BindingFlags.NonPublic | BindingFlags.Static)?.GetBaseDefinition();
        if (originalAssetMethod == null)
        {
            Log.LogInfo("originalMethod null");
            return;
        }
        MethodInfo? newAssetMethod = typeof(FixCreateFontAssetInstance)
            .GetMethod(nameof(FixCreateFontAssetInstance.CreateFontAssetInstance), BindingFlags.NonPublic | BindingFlags.Static)?.GetBaseDefinition();
        if (newAssetMethod == null)
        {
            Log.LogInfo("newMethod null");
            return;
        }

        _harmony.Patch(originalAssetMethod, new HarmonyMethod(newAssetMethod));
        
        TMP_FontAsset? loadedFontAsset = null;
        await Awaitable.MainThreadAsync();
        try
        {
            loadedFontAsset = await LoadSystemFont(FontFamily.Value, FontWeight.Value);
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
        ffn._numberMaterials = new FontAssetSystemSettings.FontForName.NumberMaterials(ffn);
        
        FontAssetSystem.AddFont(ffn, _fontAssetSystem.Fonts, ref _fontAssetSystem.defaultFont);
        
        Log.LogInfo("Font loading complete");
        
        // ok we no longer need the workaround :)
        _harmony.Unpatch(originalAssetMethod, HarmonyPatchType.Prefix, MyPluginInfo.PLUGIN_GUID);
    }
}