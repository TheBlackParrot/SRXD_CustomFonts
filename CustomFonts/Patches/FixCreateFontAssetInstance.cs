using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace CustomFonts.Patches;

// for whatever reason i am too lazy to figure out a proper solution for, ShaderUtilities.ShaderRef_MobileSDF returns null
// and this is what TMP's trying to use for a material shader when generating font assets
// idk, this is ugly. just cleaned up with rider de-comp'd

[HarmonyPatch]
internal class FixCreateFontAssetInstance
{
    [HarmonyPatch(typeof(TMP_FontAsset), "CreateFontAssetInstance")]
    [HarmonyPrefix]
    [SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags")]
    internal static bool CreateFontAssetInstance(Font font, int atlasPadding, GlyphRenderMode renderMode,
        int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode, bool enableMultiAtlasSupport,
        // ReSharper disable once InconsistentNaming
        ref TMP_FontAsset __result)
    {
        Shader foundShader = Resources.FindObjectsOfTypeAll<Shader>().First(x => x.name == "TextMeshPro/Distance Field");
        Plugin.Log.LogInfo($"Found TextMeshPro Shader: {foundShader.name}");
    
        TMP_FontAsset instance = ScriptableObject.CreateInstance<TMP_FontAsset>();
        instance.m_Version = "1.1.0";
        instance.faceInfo = FontEngine.GetFaceInfo();
        if (atlasPopulationMode == AtlasPopulationMode.Dynamic && font != null)
        {
            instance.sourceFontFile = font; 
        }
        instance.atlasPopulationMode = atlasPopulationMode;
        instance.clearDynamicDataOnBuild = TMP_Settings.clearDynamicDataOnBuild;
        instance.atlasWidth = atlasWidth;
        instance.atlasHeight = atlasHeight;
        instance.atlasPadding = atlasPadding;
        instance.atlasRenderMode = renderMode;
        instance.atlasTextures = new Texture2D[1];
        TextureFormat textureFormat = (renderMode & (GlyphRenderMode) 65536) == (GlyphRenderMode) 65536 ? TextureFormat.RGBA32 : TextureFormat.Alpha8;
        Texture2D texture2D = new(1, 1, textureFormat, false);
        instance.atlasTextures[0] = texture2D;
        instance.isMultiAtlasTexturesEnabled = enableMultiAtlasSupport;
        int num;
        if ((renderMode & (GlyphRenderMode) 16) == (GlyphRenderMode) 16)
        {
            num = 0;
            Material material = textureFormat != TextureFormat.Alpha8 ? new Material(Shader.Find("TextMeshPro/Sprite")) : new Material(foundShader);
            material.SetTexture(ShaderUtilities.ID_MainTex, texture2D);
            material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
            material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);
            instance.material = material;
        }
        else
        {
            num = 1;
            Material material = new(foundShader);
            material.SetTexture(ShaderUtilities.ID_MainTex, texture2D);
            material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
            material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);
            material.SetFloat(ShaderUtilities.ID_GradientScale, (atlasPadding + num));
            material.SetFloat(ShaderUtilities.ID_WeightNormal, instance.normalStyle);
            material.SetFloat(ShaderUtilities.ID_WeightBold, instance.boldStyle);
            instance.material = material;
        }
        instance.freeGlyphRects = new List<GlyphRect>(8)
        {
            new(0, 0, atlasWidth - num, atlasHeight - num)
        };
        instance.usedGlyphRects = new List<GlyphRect>(8);
        instance.ReadFontAssetDefinition();
        
        __result = instance;
        return false;
    }
}