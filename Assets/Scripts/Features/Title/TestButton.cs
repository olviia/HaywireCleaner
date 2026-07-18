using Core;
using Core.SaveSystem;
using Core.SceneControls;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Features.Title
{
    public class TestButton:MonoBehaviour
    {

        public void OnSwitchLocale()
        {
            var currentLocale = LocalizationSettings.SelectedLocale;
            if (currentLocale.Identifier.Code.Equals("en"))
            {
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("uk");
            }
            else
            {
                LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
            }
        }

        public void OnSaveTestButton()
        {
            WorldState.Save();
        }
    }
}