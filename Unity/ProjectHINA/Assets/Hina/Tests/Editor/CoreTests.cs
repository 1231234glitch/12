using Hina.Core;
using NUnit.Framework;

namespace Hina.Tests
{
    public sealed class CoreTests
    {
        GameModel New()
        {
            var stage=new StageData{name="TEST",boss="binah",music="coast",length=4620,platforms=new RectData[0],waves=new WaveData[0],props=new PropData[0],opening=new int[0]};
            var g=new GameModel(new[]{stage});g.Start();return g;
        }
        [Test] public void OneHitConsumesOneLife()
        {
            var g=New();g.Player.Invulnerable=0;g.HitPlayer();g.HitPlayer(true);
            Assert.AreEqual(2,g.Lives);Assert.IsFalse(g.Player.Alive);
        }
        [Test] public void TankNeedsEighteenDamage()
        {
            var g=New();var tank=g.SpawnEnemy(1000,8);g.HitEnemy(tank,17);Assert.IsFalse(tank.Dead);g.HitEnemy(tank,1);Assert.IsTrue(tank.Dead);
        }
        [Test] public void PauseFreezesPhysics()
        {
            var g=New();g.TogglePause();float x=g.Player.X;g.Tick(.02f,new Controls{Move=1});Assert.AreEqual(x,g.Player.X);
        }
        [Test] public void JumpChangesVerticalVelocity()
        {
            var g=New();g.Tick(1/60f,new Controls{Jump=true});Assert.Less(g.Player.VY,0);Assert.IsFalse(g.Player.Grounded);
        }
    }
}
