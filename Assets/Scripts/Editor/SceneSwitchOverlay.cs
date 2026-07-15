using System.IO;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

namespace Tools
{
    [Overlay(typeof(SceneView), "Scenes")]
    public class SceneSwitchOverlay:Overlay
    {
        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if(!scene.enabled) continue;
                var path = scene.path;
                var name = Path.GetFileNameWithoutExtension(path);
                root.Add(new Button(() => Open(path)) {text = name});
            }

            return root;
        }

        private static void Open(string path)
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
        }
    }
    
}