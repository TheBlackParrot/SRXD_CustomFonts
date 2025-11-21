using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace CustomFonts.Patches;

// for whatever reason i am too lazy to figure out a proper solution for, ShaderUtilities.ShaderRef_MobileSDF returns null
// and this is what TMP's trying to use for a material shader when generating font assets

// a transpiler method would probably be better suited here but that's outside of my ability to program. that shit scary

internal class FixCreateFontAssetInstance
{
    internal static string WantedVariant = "Outline at 40 Material";
    
    internal static bool CreateFontAssetInstance(Font font, int atlasPadding, GlyphRenderMode renderMode,
        int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode, bool enableMultiAtlasSupport,
        // ReSharper disable once InconsistentNaming
        ref TMP_FontAsset __result)
    {
        Texture2D texture2D = new(1, 1, TextureFormat.Alpha8, false);
        TMP_FontAsset instance = ScriptableObject.CreateInstance<TMP_FontAsset>();
        
        instance.sourceFontFile = font;
        instance.faceInfo = FontEngine.GetFaceInfo();
        
        instance.atlasPopulationMode = atlasPopulationMode;
        instance.atlasWidth = atlasWidth;
        instance.atlasHeight = atlasHeight;
        instance.atlasPadding = atlasPadding;
        instance.atlasRenderMode = renderMode;
        instance.atlasTextures = [texture2D];
        instance.isMultiAtlasTexturesEnabled = enableMultiAtlasSupport;
        
        instance.freeGlyphRects = new List<GlyphRect>(8)
        {
            new(0, 0, atlasWidth - 1, atlasHeight - 1)
        };
        instance.usedGlyphRects = new List<GlyphRect>(8);
        instance.ReadFontAssetDefinition();
        
        Material material;
        try
        {
            material = new Material(Resources.FindObjectsOfTypeAll<Material>()
                .First(x => x.name == $"Montserrat-ExtraBold SDF {WantedVariant}"));

#if DEBUG
            foreach (Material tmp in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (tmp.name.Contains("Montserrat-ExtraBold SDF Outline"))
                {
                    Plugin.Log.LogInfo(tmp.name);
                }
            }
#endif
        }
        catch (Exception)
        {
            material = new Material(Resources.FindObjectsOfTypeAll<Shader>()
                .First(x => x.name == "TextMeshPro/Distance Field"));
        }

        material.SetTexture(ShaderUtilities.ID_MainTex, texture2D);
        material.SetInt(ShaderUtilities.ID_TextureWidth, atlasWidth);
        material.SetInt(ShaderUtilities.ID_TextureHeight, atlasHeight);
        material.SetInt(ShaderUtilities.ID_GradientScale, (atlasPadding + 1));
        material.SetFloat(ShaderUtilities.ID_WeightNormal, instance.normalStyle);
        material.SetFloat(ShaderUtilities.ID_WeightBold, instance.boldStyle);
        instance.material = material;
        
        __result = instance;
        return false;
    }
}