using System;
using System.Threading.Tasks;
using BepInEx.Configuration;
using SpinCore.UI;
using UnityEngine;

namespace CustomFonts;

public partial class Plugin
{
    internal static ConfigEntry<string> FontFamily = null!;
    internal static ConfigEntry<string> FontWeight = null!;
    
    internal static ConfigEntry<bool> DisableItalics = null!;

    private void RegisterConfigEntries()
    {
        FontFamily = Config.Bind("Font", "FontFamily", "Arial",
            "Name of the custom font");
        FontWeight = Config.Bind("Font", "FontWeight", "Bold",
            "Weight of the custom font");

        DisableItalics = Config.Bind("Tweaks", "DisableItalics", false,
            "Forcibly disable italics on all text elements");
    }
    
    private static void CreateModPage()
    {
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
        CustomInputField fontFamilyInput = UIHelper.CreateInputField(fontFamilyGroup, nameof(FontFamily), null);
        fontFamilyInput.InputField.SetText(FontFamily.Value);
        #endregion
        
        #region FontWeight
        CustomGroup fontWeightGroup = UIHelper.CreateGroup(modGroup, "FontWeightGroup");
        fontWeightGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateLabel(fontWeightGroup, "FontWeightLabel", $"{nameof(CustomFonts)}_{nameof(FontWeight)}");
        CustomInputField fontWeightInput = UIHelper.CreateInputField(fontWeightGroup, nameof(FontWeight), null);
        fontWeightInput.InputField.SetText(FontWeight.Value);
        #endregion
        
        UIHelper.CreateButton(modGroup, "ApplyFontsButton", $"{nameof(CustomFonts)}_ApplyButtonText", () =>
        {
            FontFamily.Value = fontFamilyInput.InputField.text;
            FontWeight.Value = fontWeightInput.InputField.text;
            
            Task.Run(async () =>
            {
                try
                {
                    Log.LogInfo($"Changing font to {FontFamily.Value}-{FontWeight.Value}...");
                    await LoadAllFontVariants();
                }
                catch (Exception e)
                {
                    Log.LogError(e);
                }
            });
        });
        
        #region DisableItalics
        CustomGroup disableItalicsGroup = UIHelper.CreateGroup(modGroup, "DisableItalicsGroup");
        disableItalicsGroup.LayoutDirection = Axis.Horizontal;
        UIHelper.CreateSmallToggle(disableItalicsGroup, nameof(DisableItalics),
            $"{nameof(CustomFonts)}_{nameof(DisableItalics)}", DisableItalics.Value, value =>
            {
                DisableItalics.Value = value;
            });
        #endregion
        
        UIHelper.CreateButton(modGroup, "OpenCustomFontsRepositoryButton", $"{nameof(CustomFonts)}_GitHubButtonText", () =>
        {
            Application.OpenURL($"https://github.com/TheBlackParrot/{nameof(CustomFonts)}/releases/latest");
        });
    }
}