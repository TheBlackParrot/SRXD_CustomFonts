using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
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
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log = null!;
    private static Harmony _harmony = null!;
    private static string DataPath => Path.Combine(Paths.ConfigPath, nameof(CustomFonts));

    internal static bool FontsLoaded;
    internal static readonly List<string> LoadedFonts = [];

    private void Awake()
    {
        Log = Logger;

        if (!Directory.Exists(DataPath))
        {
            Directory.CreateDirectory(DataPath);
        }

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

        Log.LogInfo("Plugin loaded");
    }

    private void OnEnable()
    {
        Task.Run(async () =>
        {
            try
            {
                await FirstTimeFontLoad();
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
    private static async Task<TMP_FontAsset> LoadFromTTF(string path)
    {
        Log.LogInfo($"Loading TTF: {path}");
        
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
        
        await Awaitable.MainThreadAsync();

        Font fnt = new(path);
        Enum.TryParse(settings.Value.renderMode.ToString(), out GlyphRenderMode renderMode);
    
        Log.LogInfo("Creating font asset...");
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(fnt, settings.Value.pointSize, settings.Value.padding,
            renderMode, settings.Value.atlasWidth, settings.Value.atlasHeight);
        font.name = Path.GetFileNameWithoutExtension(path);

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

    private static async Task FirstTimeFontLoad()
    {
        if (FontsLoaded)
        {
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

        _harmony.PatchAll();

        string[] files = Directory.GetFiles(DataPath);
        foreach (string file in files)
        {
            if (!file.EndsWith(".ttf") && !file.EndsWith(".otf"))
            {
                continue;
            }
            
            string fontName = Path.GetFileNameWithoutExtension(file);
            
            if (LoadedFonts.Contains(fontName))
            {
                continue;
            }
            
            await Awaitable.MainThreadAsync();

            Log.LogInfo("Adding font " + Path.GetFileName(file));
            TMP_FontAsset? loadedFontAsset = null;
            try
            {
                loadedFontAsset = await LoadFromTTF(file);
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }

            // think there's some steps missing here, worry about this later
            // it looks? like? it properly generates the atlas for the HUD numbers but it doesn't? seem to update? where things are? idk
            FontAssetSystemSettings.FontForName ffn = new()
            {
                font = loadedFontAsset,
                name = fontName
            };
            ffn.Init();
            ffn._numberMaterials = new FontAssetSystemSettings.FontForName.NumberMaterials(ffn);

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
            
            FontAssetSystem.AddFont(ffn, _fontAssetSystem.Fonts, ref _fontAssetSystem.defaultFont);
            LoadedFonts.Add(fontName);
        }

        Log.LogInfo("Font loading complete");
        FontsLoaded = true;
        
        // ok we no longer need the workaround :)
        _harmony.Unpatch(originalAssetMethod, HarmonyPatchType.Prefix, MyPluginInfo.PLUGIN_GUID);

        if (LoadedFonts.Count == 0)
        {
            Log.LogInfo("No fonts loaded");
            _harmony.UnpatchSelf();
        }
    }
}