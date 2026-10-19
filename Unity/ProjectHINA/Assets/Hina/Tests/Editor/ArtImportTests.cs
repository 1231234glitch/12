using System;
using Hina.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace Hina.Tests
{
    public sealed class ArtImportTests
    {
        [OneTimeSetUp] public void Prepare() { Hina.Editor.ProjectSetup.PrepareProject(); }
        [Test] public void AllSheetsImportAsNamedSprites()
        {
            var text=Resources.Load<TextAsset>("Hina/atlas_manifest");Assert.IsNotNull(text);
            var manifest=JsonUtility.FromJson<AtlasManifest>(text.text);
            foreach(var sheet in manifest.atlases)
            {
                var sprites=Resources.LoadAll<Sprite>("Hina/Sheets/"+sheet.id);
                Assert.AreEqual(sheet.frames.Length,sprites.Length,sheet.id);
                foreach(var frame in sheet.frames)Assert.IsTrue(Array.Exists(sprites,s=>s.name==frame),sheet.id+"/"+frame);
            }
        }
        [Test] public void BootstrapSceneIsIncludedInBuild()
        {
            Assert.IsTrue(Array.Exists(EditorBuildSettings.scenes,s=>s.enabled&&s.path==Hina.Editor.ProjectSetup.ScenePath));
        }
        [Test] public void CanonicalPlayerAndEveryBossResolve()
        {
            var atlas=new AtlasLibrary();Assert.IsNotNull(atlas.Animate("hina","idle",0));
            foreach(var actor in new[]{"binah","perorodzilla","hod","goz","chesed"})
                foreach(var state in new[]{"idle","warn","attack","phase2","hurt","death"})Assert.IsNotNull(atlas.Animate(actor,state,0),actor+"/"+state);
        }
    }
}
