using System;
using System.Collections.Generic;

namespace Hina.Core
{
    public enum Mode { Title, Playing, Paused, Continue, GameOver, Clear, Complete }
    public struct Controls { public float Move; public bool Up, Down, Shoot, Jump, Special; }
    [Serializable] public class RectData { public float x, y, width, height; public bool Contains(float px,float py) { return px>=x&&px<=x+width&&py>=y&&py<=y+height; } }
    [Serializable] public class WaveData { public float trigger, spawn_x; public int[] kinds; [NonSerialized] public bool triggered; }
    [Serializable] public class PropData { public float x, width, height; public int style; }
    [Serializable] public class DecorData { public float x,height=42,baseline=302; public int style; }
    [Serializable] public class StageData { public int id; public string name, boss, music; public float length; public int[] opening; public RectData[] platforms; public WaveData[] waves; public PropData[] props; public DecorData[] decor; }
    [Serializable] public class StageCollection { public StageData[] stages; }
    public class Player { public float X=80,Y=310,VY,Invulnerable,FireTimer,KickTimer,SpecialTimer; public int Facing=1,Stock=3; public bool Grounded=true,Alive=true; public string Animation="idle"; }
    public class Enemy { public int Id,Kind,HP; public float X,Y=310,Timer,Age,DeathTime,Recoil,Hurt; public bool Dead; }
    public class Bullet { public float X,Y,VX,VY,Life=2.8f; public bool Friendly; public int Damage=1; public string Art; }
    public class Prop { public PropData Data; public int HP=3; public bool Dead; public float DeathTime; }
    public class Effect { public float X,Y,Life=.4f; public string Kind; public int Facing=1; }
    public class Pickup { public float X,Y; public bool Special; }
    public class Hazard { public float X,Y,Width,Height,Delay,Life=.4f; }
    public class RaidTarget { public float X,Y; public int HP; public string Kind; public bool Dead; }
    public class Boss { public float X,HP,MaxHP,Timer=2,CoreTime,LockedX,LockedY,Hurt; public int Pattern,Wave; public bool Warning,Dead; public string Attack="INTRO"; public readonly List<RaidTarget> Targets=new List<RaidTarget>(); }
    public class SoundEvent { public string Name; public SoundEvent(string name) { Name=name; } }

    // Engine-independent fixed-step simulation. Pixel units match the source data.
    public sealed class GameModel
    {
        public Mode Mode=Mode.Title;
        public StageData Stage { get { return Stages[StageIndex]; } }
        public readonly StageData[] Stages;
        public Player Player=new Player();
        public Boss Boss;
        public int StageIndex,Lives=3,Score,Coins,NextId,Unlocked=1;
        public float MissionTime=99,ContinueTime,Clock,RespawnTime,ClearTime;
        public readonly List<Enemy> Enemies=new List<Enemy>();
        public readonly List<Bullet> Bullets=new List<Bullet>();
        public readonly List<Prop> Props=new List<Prop>();
        public readonly List<Effect> Effects=new List<Effect>();
        public readonly List<Pickup> Pickups=new List<Pickup>();
        public readonly List<Hazard> Hazards=new List<Hazard>();
        public readonly Queue<SoundEvent> Sounds=new Queue<SoundEvent>();
        public float CameraX { get { return Clamp(Player.X,320,Math.Max(320,Stage.length-320)); } }
        public GameModel(StageData[] stages) { if(stages==null||stages.Length==0)throw new ArgumentException("Stages are required"); Stages=stages; }
        static float Clamp(float x,float lo,float hi) { return Math.Max(lo,Math.Min(hi,x)); }
        static float Abs(float x) { return Math.Abs(x); }
        static float Sqrt(float x) { return (float)Math.Sqrt(x); }
        void Sound(string name) { Sounds.Enqueue(new SoundEvent(name)); }
        public void Start(int stage=0) { Score=0;Lives=3;Load(stage); }
        public void Load(int index)
        {
            StageIndex=(int)Clamp(index,0,Stages.Length-1);Mode=Mode.Playing;Player=new Player();Player.Invulnerable=1.5f;
            Boss=null;Bullets.Clear();Enemies.Clear();Props.Clear();Effects.Clear();Pickups.Clear();Hazards.Clear();Sounds.Clear();
            MissionTime=99;RespawnTime=0;ClearTime=0;
            foreach(var w in Stage.waves??Array.Empty<WaveData>())w.triggered=false;
            foreach(var p in Stage.props??Array.Empty<PropData>())Props.Add(new Prop{Data=p});
            var opening=Stage.opening??new[]{0,1,2};for(int i=0;i<opening.Length;i++)SpawnEnemy(430+i*105,opening[i]);
            Sound("ui");
        }
        public Enemy SpawnEnemy(float x,int kind) { var e=new Enemy{Id=++NextId,X=x,Kind=kind,HP=kind==8?18:kind==9?26:1,Timer=.9f+(NextId%3)*.3f};Enemies.Add(e);return e; }
        public void TogglePause() { if(Mode==Mode.Playing)Mode=Mode.Paused;else if(Mode==Mode.Paused)Mode=Mode.Playing; }
        public void InsertCoin()
        {
            Coins++;Sound("pickup");
            if(Mode==Mode.Continue){Lives++;Mode=Mode.Playing;Revive();}
            else if(Mode==Mode.GameOver){Load(StageIndex);Lives=1;}
            else if(Mode==Mode.Playing||Mode==Mode.Paused)Lives++;
        }
        public void HitPlayer(bool force=false)
        {
            if(!Player.Alive||(!force&&Player.Invulnerable>0))return;
            Lives--;Player.Alive=false;RespawnTime=.6f;Sound("death");
        }
        void Revive() { Player.Alive=true;Player.Invulnerable=3;Player.Stock=3;Player.VY=0;Player.KickTimer=0;Player.SpecialTimer=0;RespawnTime=0;Sound("respawn"); }
        public void HitEnemy(Enemy e,int damage)
        {
            if(e.Dead)return;e.Hurt=.12f;e.HP-=damage;Effects.Add(new Effect{X=e.X,Y=e.Y-25,Kind="hit",Life=.12f});Sound("hit");if(e.HP>0)return;
            e.Dead=true;Score+=e.Kind>=8?1000:100;Sound(e.Kind>=8?"explosion":"enemy_down");
            Effects.Add(new Effect{X=e.X,Y=e.Y-25,Kind="explosion"});
            if(e.Id%6==0)Pickups.Add(new Pickup{X=e.X,Y=e.Y-24,Special=e.Id%12==0});
        }
        public void HitBoss(int damage)
        {
            if(Boss==null||Boss.Dead)return;
            if(StageIndex==2&&Boss.Targets.Exists(t=>!t.Dead&&t.Kind=="pillar"))return;
            if(StageIndex==4&&Boss.CoreTime<=0)return;
            Boss.Hurt=.12f;Boss.HP-=damage*(StageIndex==4?3:1);Sound("armor_hit");
            if(Boss.HP<=0){Boss.Dead=true;Hazards.Clear();Boss.Targets.Clear();Score+=10000;Mode=Mode.Clear;ClearTime=4.8f;Sound("clear");Unlocked=Math.Max(Unlocked,StageIndex+2);}
        }
        public void StartBoss()
        {
            if(Boss!=null)return;Boss=new Boss{X=Stage.length-150,MaxHP=240+StageIndex*100,HP=240+StageIndex*100};Sound("boss");
        }
        public void Tick(float dt,Controls input)
        {
            dt=Math.Max(0,Math.Min(dt,.05f));Clock+=dt;
            if(Mode==Mode.Continue){ContinueTime-=dt;if(ContinueTime<=0)Mode=Mode.GameOver;return;}
            if(Mode==Mode.Clear){ClearTime-=dt;if(ClearTime<=0){if(StageIndex+1<Stages.Length)Load(StageIndex+1);else Mode=Mode.Complete;}return;}
            if(Mode!=Mode.Playing)return;
            if(!Player.Alive){RespawnTime-=dt;if(RespawnTime<=0){if(Lives>0)Revive();else{Mode=Mode.Continue;ContinueTime=10;}}return;}
            MissionTime-=dt;if(MissionTime<=0){MissionTime=99;HitPlayer(true);return;}
            UpdatePlayer(dt,input);
            foreach(var w in Stage.waves??Array.Empty<WaveData>())if(!w.triggered&&Player.X>=w.trigger){w.triggered=true;for(int i=0;i<w.kinds.Length;i++)SpawnEnemy(w.spawn_x+i*70,w.kinds[i]);}
            if(Player.X>Stage.length-620&&Boss==null)StartBoss();
            UpdateEnemies(dt);UpdateBoss(dt);UpdateBullets(dt);UpdateHazards(dt);
            foreach(var prop in Props)if(prop.Dead)prop.DeathTime+=dt;
            for(int i=Effects.Count-1;i>=0;i--){Effects[i].Life-=dt;if(Effects[i].Life<=0)Effects.RemoveAt(i);}
            for(int i=Pickups.Count-1;i>=0;i--){var p=Pickups[i];if(Abs(p.X-Player.X)<24&&Abs(p.Y-(Player.Y-20))<36){if(p.Special)Player.Stock=Math.Min(9,Player.Stock+1);else Score+=250;Sound("pickup");Pickups.RemoveAt(i);}}
        }
        void UpdatePlayer(float dt,Controls input)
        {
            var p=Player;p.Invulnerable=Math.Max(0,p.Invulnerable-dt);p.FireTimer-=dt;p.KickTimer=Math.Max(0,p.KickTimer-dt);
            if(input.Special&&p.Stock>0&&p.SpecialTimer<=0){p.Stock--;p.SpecialTimer=1.7f;p.Invulnerable=1.7f;Sound("charge");}
            if(p.SpecialTimer>0){float old=p.SpecialTimer;p.SpecialTimer-=dt;if(old>.85f&&p.SpecialTimer<=.85f){foreach(var e in Enemies)if(!e.Dead&&Abs(e.X-p.X)<600)HitEnemy(e,30);HitBoss(30);Effects.Add(new Effect{X=p.X+p.Facing*160,Y=p.Y-40,Life=1,Kind="special"});Effects.Add(new Effect{X=p.X+p.Facing*60,Y=p.Y-30,Life=.3f,Kind="special_bullet",Facing=p.Facing});Sound("special");}}
            if(input.Move!=0&&p.SpecialTimer<=0&&p.KickTimer<=0)p.Facing=input.Move<0?-1:1;
            bool crouch=p.Grounded&&input.Down;
            float move=p.SpecialTimer>0||p.KickTimer>0||crouch?0:input.Move*180*dt;
            p.X=Clamp(p.X+move,Boss!=null?Stage.length-630:15,Stage.length-25);
            if(input.Jump&&p.Grounded&&p.SpecialTimer<=0){p.VY=-390;p.Grounded=false;Sound("jump");}
            float oldY=p.Y;p.VY=Math.Min(600,p.VY+1050*dt);p.Y+=p.VY*dt;p.Grounded=false;
            float floor=310;
            foreach(var r in Stage.platforms??Array.Empty<RectData>())if(p.X>r.x-8&&p.X<r.x+r.width+8&&oldY<=r.y+1&&p.Y>=r.y&&p.VY>=0)floor=Math.Min(floor,r.y);
            if(p.Y>=floor&&p.VY>=0){p.Y=floor;p.VY=0;p.Grounded=true;}
            float ax=p.Facing,ay=0;if(input.Up){ay=-1;ax=input.Move;}else if(!p.Grounded&&input.Down){ax=0;ay=1;}
            float length=Sqrt(ax*ax+ay*ay);ax/=Math.Max(length,.001f);ay/=Math.Max(length,.001f);
            if(input.Shoot&&p.FireTimer<=0&&p.SpecialTimer<=0&&p.KickTimer<=0)
            {
                var close=Enemies.Find(e=>!e.Dead&&(e.X-p.X)*p.Facing>0&&(e.X-p.X)*p.Facing<55&&Abs(e.Y-p.Y)<45);
                if(close!=null){p.KickTimer=.44f;p.FireTimer=.44f;HitEnemy(close,3);Sound("kick_hit");}
                else{p.FireTimer=.09f;Fire(p.X+ax*29,p.Y-(crouch?15:30)+ay*14,ax*850,ay*850,true);Sound("shot");}
            }
            p.Animation=!p.Alive?"death":p.SpecialTimer>0?"special":p.KickTimer>0?(p.Grounded?"kick":"air_kick"):input.Up?(input.Move!=0?"shoot_diagonal":"shoot_up"):!p.Grounded?(input.Down?"air_down":input.Shoot?"air_shoot":p.VY<0?"jump_rise":"fall"):crouch?(input.Shoot?"crouch_shoot":"crouch"):input.Move!=0?(input.Shoot?"walk_shoot":"walk"):input.Shoot?"shoot":"idle";
        }
        void Fire(float x,float y,float vx,float vy,bool friendly,string art=null) { if(Bullets.Count<320)Bullets.Add(new Bullet{X=x,Y=y,VX=vx,VY=vy,Friendly=friendly,Art=art??(friendly?"friendly_bullet":"enemy_bullet")});if(friendly)Effects.Add(new Effect{X=x,Y=y,Kind="muzzle",Life=.07f,Facing=vx<0?-1:1}); }
        void UpdateEnemies(float dt)
        {
            for(int i=Enemies.Count-1;i>=0;i--)
            {
                var e=Enemies[i];if(e.Dead){e.DeathTime+=dt;if(e.DeathTime>.95f)Enemies.RemoveAt(i);continue;}
                if(Abs(e.X-Player.X)>800)continue;e.Age+=dt;e.Timer-=dt;e.Recoil=Math.Max(0,e.Recoil-dt);e.Hurt=Math.Max(0,e.Hurt-dt);
                if(e.Kind<8&&Abs(e.X-Player.X)>130)e.X+=(Player.X<e.X?-1:1)*28*dt;
                if(e.Timer<=0){e.Recoil=.15f;e.Timer=e.Kind>=8?1.8f:1.5f+(e.Kind%3)*.4f;float dx=Player.X-e.X,dy=Player.Y-25-(e.Y-32),len=Math.Max(1,Sqrt(dx*dx+dy*dy));Fire(e.X,e.Y-32,dx/len*(e.Kind>=8?240:170),dy/len*170,false,e.Kind>=3?"robot_bullet":"enemy_bullet");Sound("enemy_shot");}
                if(Abs(e.X-Player.X)<(e.Kind>=8?60:22)&&Abs(e.Y-Player.Y)<36)HitPlayer();
            }
        }
        void UpdateBullets(float dt)
        {
            for(int i=Bullets.Count-1;i>=0;i--)
            {
                var b=Bullets[i];b.Life-=dt;int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(Abs(b.VX),Abs(b.VY))*dt/5));bool dead=b.Life<=0;
                for(int s=0;s<steps&&!dead;s++)
                {
                    b.X+=b.VX*dt/steps;b.Y+=b.VY*dt/steps;if(b.Y>=310){dead=true;break;}
                    foreach(var r in Stage.platforms??Array.Empty<RectData>())if(r.Contains(b.X,b.Y)){dead=true;break;}
                    if(dead)break;
                    foreach(var prop in Props)if(!prop.Dead&&b.X>=prop.Data.x&&b.X<=prop.Data.x+prop.Data.width&&b.Y>310-prop.Data.height){dead=true;if(b.Friendly&&--prop.HP<=0){prop.Dead=true;Score+=50;Sound("explosion");Effects.Add(new Effect{X=b.X,Y=b.Y,Kind="explosion"});}break;}
                    if(dead)break;
                    if(b.Friendly)
                    {
                        if(Boss!=null)foreach(var t in Boss.Targets)if(!t.Dead&&Abs(t.X-b.X)<22&&Abs(t.Y-25-b.Y)<35){t.HP-=b.Damage;if(t.HP<=0){t.Dead=true;Score+=100;}dead=true;break;}
                        if(dead)break;
                        foreach(var e in Enemies)if(!e.Dead&&Abs(e.X-b.X)<(e.Kind>=8?54:14)&&b.Y>e.Y-(e.Kind>=8?44:48)&&b.Y<e.Y){HitEnemy(e,b.Damage);dead=true;break;}
                        if(!dead&&Boss!=null&&!Boss.Dead&&Abs(b.X-Boss.X)<70&&b.Y>95&&b.Y<300){HitBoss(b.Damage);dead=true;}
                    }
                    else if(Abs(b.X-Player.X)<9&&b.Y>Player.Y-(Player.Animation.StartsWith("crouch")?22:35)&&b.Y<Player.Y){HitPlayer();dead=true;}
                }
                if(dead)Bullets.RemoveAt(i);
            }
        }
        void UpdateBoss(float dt)
        {
            var b=Boss;if(b==null||b.Dead)return;b.Hurt=Math.Max(0,b.Hurt-dt);
            if(b.CoreTime>0){b.CoreTime-=dt;if(b.CoreTime<=0){b.Wave=0;b.Targets.Clear();}}
            if(StageIndex==4&&b.Targets.Count>0&&b.Targets.TrueForAll(t=>t.Dead)){b.Targets.Clear();b.Wave++;if(b.Wave>=4)b.CoreTime=5;}
            b.Timer-=dt;if(b.Timer>0)return;
            if(!b.Warning)
            {
                b.Warning=true;b.Timer=1.1f;b.Pattern++;b.LockedX=Player.X;b.LockedY=Player.Y-20;
                switch(StageIndex)
                {
                    case 0:b.Attack=b.Pattern%3==0?"MISSILE LANES":"LOCKED LASER";if(b.Pattern%3==0)for(int n=-1;n<=1;n++)Hazards.Add(new Hazard{X=b.LockedX+n*90-18,Y=100,Width=36,Height=210,Delay=1.1f});else Hazards.Add(new Hazard{X=Stage.length-630,Y=b.LockedY-9,Width=600,Height=18,Delay=1.1f});break;
                    case 1:b.Attack=b.Pattern%2==0?"EYE BEAM":"PERORO ADDS";if(b.Pattern%2==0)Hazards.Add(new Hazard{X=Stage.length-630,Y=b.LockedY-10,Width=610,Height=20,Delay=1.1f});else for(int n=0;n<3;n++)b.Targets.Add(new RaidTarget{X=Stage.length-450+n*90,Y=310,HP=2,Kind="peroro"});break;
                    case 2:b.Attack="DESTROY THE PILLARS";if(!b.Targets.Exists(t=>!t.Dead))for(int n=0;n<2;n++)b.Targets.Add(new RaidTarget{X=Stage.length-440+n*150,Y=310,HP=6,Kind="pillar"});Hazards.Add(new Hazard{X=Stage.length-630,Y=292,Width=600,Height=18,Delay=1.1f});break;
                    case 3:
                        b.Attack=b.Pattern%2==0?"FIND THE REAL GOZ":"BOMB LANES";
                        if(b.Pattern%2==0){b.X=Stage.length-540+(b.Pattern%3)*170;b.Targets.Clear();for(int n=0;n<3;n++){float cloneX=Stage.length-540+n*170;if(Abs(cloneX-b.X)>20)b.Targets.Add(new RaidTarget{X=cloneX,Y=310,HP=4,Kind="goz_clone"});}}
                        else for(int n=0;n<3;n++)Hazards.Add(new Hazard{X=Stage.length-550+n*160,Y=140,Width=45,Height=170,Delay=1.1f});break;
                    case 4:b.Attack=b.CoreTime>0?"CORE OPEN":"CLEAR FOUR WAVES";if(b.CoreTime<=0&&b.Targets.Count==0)for(int n=0;n<3;n++)b.Targets.Add(new RaidTarget{X=Stage.length-500+n*105,Y=310,HP=1,Kind="automata"});break;
                }
            }
            else
            {
                b.Warning=false;b.Timer=b.HP<b.MaxHP*.5f?1.35f:1.8f;
                if(StageIndex==1||StageIndex==4)for(int n=-1;n<=1;n++)Fire(b.X-40,150,-180,n*70,false);
                Sound("enemy_shot");
            }
        }
        void UpdateHazards(float dt)
        {
            for(int i=Hazards.Count-1;i>=0;i--){var h=Hazards[i];if(h.Delay>0)h.Delay-=dt;else{h.Life-=dt;if(Player.X>h.X-8&&Player.X<h.X+h.Width+8&&Player.Y>h.Y&&Player.Y-30<h.Y+h.Height)HitPlayer();if(h.Life<=0)Hazards.RemoveAt(i);}}
        }
    }
}
