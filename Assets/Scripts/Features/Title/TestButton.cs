using Core;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Features.Title
{
    public class TestButton:MonoBehaviour
    {
        public void OnGoToGameplayButton()
        {
            SceneStateMachine.ChangeSceneTo(GameScene.Gameplay);
        }

        public void OnGoToPrototypeButton()
        {
            SceneStateMachine.ChangeSceneTo(GameScene.Prototype1);
        }

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
    }
}