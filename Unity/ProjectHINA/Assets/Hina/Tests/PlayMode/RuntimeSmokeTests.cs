using System.Collections;
using Hina.Core;
using Hina.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hina.Tests
{
    public sealed class RuntimeSmokeTests
    {
        HinaGame game;
        [UnitySetUp] public IEnumerator Setup()
        {
            game=Object.FindObjectOfType<HinaGame>();
            if(game==null)game=new GameObject("Hina Unity smoke test").AddComponent<HinaGame>();
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup() { if(game!=null)Object.Destroy(game.gameObject);yield return null; }
        [UnityTest] public IEnumerator GameplayCreatesRealUnitySpriteViews()
        {
            game.Model.Start();yield return null;yield return null;
            Assert.AreEqual(Mode.Playing,game.Model.Mode);Assert.IsNotNull(Camera.main);Assert.IsTrue(Camera.main.orthographic);
            int visible=0;foreach(var view in Object.FindObjectsOfType<SpriteRenderer>())if(view.gameObject.activeInHierarchy&&view.sprite!=null)visible++;
            Assert.Greater(visible,8);
        }
        [UnityTest] public IEnumerator BossAndPauseRemainConnectedToScene()
        {
            game.Model.Start(4);game.Model.StartBoss();game.Model.Player.Invulnerable=9999;
            yield return null;Assert.IsNotNull(game.Model.Boss);game.Model.TogglePause();float x=game.Model.Player.X;
            yield return null;yield return null;Assert.AreEqual(Mode.Paused,game.Model.Mode);Assert.AreEqual(x,game.Model.Player.X);
        }
    }
}
