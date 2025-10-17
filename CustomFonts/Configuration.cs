using System;
using System.Threading.Tasks;
using BepInEx.Configuration;
using SpinCore.Translation;
using SpinCore.UI;
using UnityEngine;

namespace CustomFonts;

public partial class Plugin
{
    internal static ConfigEntry<string> FontFamily = null!;
    internal static ConfigEntry<string> FontWeight = null!;

    private void RegisterConfigEntries()
    {
        FontFamily = Config.Bind("Font", "FontFamily", "Arial",
            "Name of the custom font");
        FontWeight = Config.Bind("Font", "FontWeight", "Bold",
            "Weight of the custom font");
    }
    
    private static void CreateModPage()
    {
        TranslationHelper.AddTranslation($"{nameof(CustomFonts)}_ModName", nameof(CustomFonts));
        TranslationHelper.AddTranslation($"{nameof(CustomFonts)}_GitHubButtonText", $"{nameof(CustomFonts)} Releases (GitHub)");
        TranslationHelper.AddTranslation($"{nameof(CustomFonts)}_{nameof(FontFamily)}", "Font family");
        TranslationHelper.AddTranslation($"{nameof(CustomFonts)}_{nameof(FontWeight)}", "Font weight");
        
        CustomPage rootModPage = UIHelper.CreateCustomPage("ModSettings");
        rootModPage.OnPageLoad += RootModPageOnPageLoad;
        
        UIHelper.RegisterMenuInModSettingsRoot("CustomFonts_ModName", rootModPage);
    }

    private static void RootModPageOnPageLoad(Transform rootModPageTransform)
    {
        CustomGroup modGroup = UIHelper.CreateGroup(rootModPageTransform, nameof(CustomFonts));
        UIHelper.CreateSectionHeader(modGroup, "ModGroupHeader", $"{nameof(CustomFonts)}_ModName", false);
            
        #region FontFamily
        CustomGroup fontFamilyGroup = UIHelper.CreateGroup(modGroup, "FontFamilyGroup");
        fontFamilyGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(fontFamilyGroup, "FontFamilyLabel", $"{nameof(CustomFonts)}_{nameof(FontFamily)}");
        CustomInputField fontFamilyInput = UIHelper.CreateInputField(fontFamilyGroup, nameof(FontFamily), (_, newValue) =>
        {
            if (newValue == FontFamily.Value)
            {
                // erm
                return;
            }
            
            FontFamily.Value = newValue;
            Task.Run(async () =>
            {
                try
                {
                    Log.LogInfo($"Changing font to {FontFamily.Value}-{FontWeight.Value}...");
                    await LoadCustomFont();
                }
                catch (Exception e)
                {
                    Log.LogError(e);
                }
            });
        });
        fontFamilyInput.InputField.SetText(FontFamily.Value);
        #endregion
        
        #region FontWeight
        CustomGroup fontWeightGroup = UIHelper.CreateGroup(modGroup, "FontWeightGroup");
        fontWeightGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(fontWeightGroup, "FontWeightLabel", $"{nameof(CustomFonts)}_{nameof(FontWeight)}");
        CustomInputField fontWeightInput = UIHelper.CreateInputField(fontWeightGroup, nameof(FontWeight), (_, newValue) =>
        {
            if (newValue == FontWeight.Value)
            {
                // erm
                return;
            }
            
            FontWeight.Value = newValue;
            Task.Run(async () =>
            {
                try
                {
                    Log.LogInfo($"Changing font to {FontFamily.Value}-{FontWeight.Value}...");
                    await LoadCustomFont();
                }
                catch (Exception e)
                {
                    Log.LogError(e);
                }
            });
        });
        fontWeightInput.InputField.SetText(FontWeight.Value);
        #endregion

        UIHelper.CreateButton(modGroup, "OpenCustomFontsRepositoryButton", "CustomFonts_GitHubButtonText", () =>
        {
            Application.OpenURL($"https://github.com/TheBlackParrot/{nameof(CustomFonts)}/releases/latest");
        });
    }
}