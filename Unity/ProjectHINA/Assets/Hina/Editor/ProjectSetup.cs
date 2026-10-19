using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hina.Editor
{
    public static class ProjectSetup
    {
        const string ScenePath="Assets/Hina/Scenes/Bootstrap.unity";
        [InitializeOnLoadMethod]
        static void OnOpen() { EditorApplication.delayCall+=()=>{if(!File.Exists(ScenePath)&&!EditorApplication.isPlayingOrWillChangePlaymode)CreateScene();}; }
        [MenuItem("HINA/Open Demo Scene")]
        public static void CreateScene()
        {
            if(!File.Exists(ScenePath))
            {
                // Runtime bootstrap creates the camera and game. No prefabs are required.
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                Directory.CreateDirectory("Assets/Hina/Scenes");EditorSceneManager.SaveScene(scene,ScenePath);
            }
            PlayerSettings.companyName="Project HINA";PlayerSettings.productName="PROJECT HINA: OPERATION GEHENNA";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        }
        [MenuItem("HINA/Reimport Sprite Sheets")]
        public static void Reimport()
        {
            foreach(var path in Directory.GetFiles("Assets/Hina/Resources/Hina/Sheets","*.png"))AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
        }
        [MenuItem("HINA/Build Windows x64")]
        public static void BuildWindows()
        {
            CreateScene();Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new[]{ScenePath},"Builds/Windows/ProjectHINA.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("Windows build failed: "+report.summary.result);
        }
        [MenuItem("HINA/Build WebGL")]
        public static void BuildWeb()
        {
            CreateScene();Directory.CreateDirectory("Builds/WebGL");
            var report=BuildPipeline.BuildPlayer(new[]{ScenePath},"Builds/WebGL",BuildTarget.WebGL,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new System.Exception("WebGL build failed: "+report.summary.result);
        }
    }
}
