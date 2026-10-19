using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hina.Editor
{
    public static class ProjectSetup
    {
        public const string ScenePath="Assets/Hina/Scenes/Bootstrap.unity";
        [InitializeOnLoadMethod]
        static void OnOpen()
        {
            EditorApplication.delayCall+=()=>
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||SessionState.GetBool("HinaPrepared",false))return;
                SessionState.SetBool("HinaPrepared",true);PrepareProject();
            };
        }
        public static void PrepareProject()
        {
            Reimport();
            if(!File.Exists(ScenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                Directory.CreateDirectory("Assets/Hina/Scenes");EditorSceneManager.SaveScene(scene,ScenePath);EditorSceneManager.CloseScene(scene,true);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.companyName="Project HINA";PlayerSettings.productName="PROJECT HINA: OPERATION GEHENNA";
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;PlayerSettings.colorSpace=ColorSpace.Gamma;
        }
        [MenuItem("HINA/Open Demo Scene")]
        public static void CreateScene()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            PrepareProject();EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        }
        [MenuItem("HINA/Reimport Sprite Sheets")]
        public static void Reimport()
        {
            const string folder="Assets/Hina/Resources/Hina/Sheets";
            if(!Directory.Exists(folder))throw new InvalidOperationException("Missing game sprite sheets");
            foreach(var path in Directory.GetFiles(folder,"*.png"))AssetDatabase.ImportAsset(path.Replace('\\','/'),ImportAssetOptions.ForceUpdate);
        }
        [MenuItem("HINA/Build Windows x64")]
        public static void BuildWindows() { Build(BuildTarget.StandaloneWindows64,"Builds/Windows/ProjectHINA.exe"); }
        [MenuItem("HINA/Build WebGL")]
        public static void BuildWeb() { Build(BuildTarget.WebGL,"Builds/WebGL"); }
        public static void BuildCI()
        {
            var target=EditorUserBuildSettings.activeBuildTarget;
            if(target!=BuildTarget.StandaloneWindows64&&target!=BuildTarget.WebGL)throw new InvalidOperationException("Unsupported CI target: "+target);
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../build",target.ToString()));
            Build(target,target==BuildTarget.StandaloneWindows64?Path.Combine(folder,"ProjectHINA.exe"):folder);
        }
        static void Build(BuildTarget target,string output)
        {
            PrepareProject();Directory.CreateDirectory(target==BuildTarget.StandaloneWindows64?Path.GetDirectoryName(output):output);
            var report=BuildPipeline.BuildPlayer(new[]{ScenePath},output,target,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Unity build failed: "+report.summary.result);
        }
    }
}
