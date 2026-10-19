using System;
using System.Collections.Generic;
using Hina.Core;
using UnityEngine;

namespace Hina.Unity
{
    public sealed class HinaGame : MonoBehaviour
    {
        public GameModel Model { get; private set; }
        AtlasLibrary atlas;
        Camera cam;
        readonly List<SpriteRenderer> pool=new List<SpriteRenderer>();
        readonly Dictionary<string,AudioClip> audio=new Dictionary<string,AudioClip>();
        readonly List<AudioSource> sfx=new List<AudioSource>();
        AudioSource music;
        int drawIndex,soundIndex;
        string musicKey="";
        float animationTime,playerStateTime,masterVolume=.8f,accumulator;
        string lastPlayerState="";
        bool shoot,jump,special;
        float movement;bool up,down;
        GUIStyle titleStyle,labelStyle,smallStyle,buttonStyle;
        Texture2D white,buttonNormal,buttonHover,buttonPressed;
        Font korean;
        bool settings,selectingStage;
        int highScore;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(FindObjectOfType<HinaGame>()==null)new GameObject("Project HINA / Unity").AddComponent<HinaGame>();
        }
        void Awake()
        {
            Application.targetFrameRate=60;
            var data=Resources.Load<TextAsset>("Hina/stages");
            if(data==null){Debug.LogError("Missing Hina/stages resource");enabled=false;return;}
            Model=new GameModel(JsonUtility.FromJson<StageCollection>(data.text).stages);
            Model.Unlocked=PlayerPrefs.GetInt("HinaUnlocked",1);highScore=PlayerPrefs.GetInt("HinaHighScore",0);
            atlas=new AtlasLibrary();korean=Resources.Load<Font>("Hina/NotoSansCJKkr-Regular");
            cam=Camera.main;
            if(cam==null){var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";cameraObject.transform.SetParent(transform,false);cam=cameraObject.GetComponent<Camera>();}
            cam.orthographic=true;cam.orthographicSize=180/64f;cam.backgroundColor=new Color(.78f,.9f,.98f);cam.clearFlags=CameraClearFlags.SolidColor;
            if(FindObjectOfType<AudioListener>()==null)cam.gameObject.AddComponent<AudioListener>();
            music=gameObject.AddComponent<AudioSource>();music.loop=true;music.volume=.35f;
            for(int i=0;i<16;i++){var source=gameObject.AddComponent<AudioSource>();sfx.Add(source);}
            foreach(var clip in Resources.LoadAll<AudioClip>("Hina/Audio"))audio[clip.name]=clip;
            masterVolume=PlayerPrefs.GetFloat("HinaVolume",.8f);AudioListener.volume=masterVolume;
            white=SolidTexture(Color.white);buttonNormal=SolidTexture(new Color(.96f,.985f,1));buttonHover=SolidTexture(new Color(.72f,.91f,1));buttonPressed=SolidTexture(new Color(.3f,.72f,.96f));
        }
        void Update()
        {
            if(Model==null)return;
            movement=Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?-1:Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0;
            up=Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow);down=Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow);
            shoot=Input.GetKey(KeyCode.J);jump|=Input.GetKeyDown(KeyCode.K);special|=Input.GetKeyDown(KeyCode.L);
            if(Input.GetKeyDown(KeyCode.Alpha1)||Input.GetKeyDown(KeyCode.Keypad1))Model.InsertCoin();
            if(Input.GetKeyDown(KeyCode.Escape)){if(settings||selectingStage){settings=false;selectingStage=false;}else Model.TogglePause();}
            if(Input.GetKeyDown(KeyCode.Return)&&Model.Mode==Mode.Title&&!settings&&!selectingStage)Model.Start();
            accumulator+=Math.Min(Time.unscaledDeltaTime,.15f);
            while(accumulator>=1/60f)
            {
                Model.Tick(1/60f,new Controls{Move=movement,Up=up,Down=down,Shoot=shoot,Jump=jump,Special=special});
                jump=false;special=false;accumulator-=1/60f;
            }
            if(Model.Mode==Mode.Playing||Model.Mode==Mode.Title)animationTime+=Time.unscaledDeltaTime;
            string playerState=Model.Player.Alive?Model.Player.Animation:"death";
            if(playerState!=lastPlayerState){lastPlayerState=playerState;playerStateTime=0;}
            else if(Model.Mode==Mode.Playing)playerStateTime+=Time.unscaledDeltaTime;
            while(Model.Sounds.Count>0)
            {
                string name=Model.Sounds.Dequeue().Name;AudioClip clip;
                if(audio.TryGetValue(name,out clip)){var source=sfx[soundIndex++%sfx.Count];source.pitch=name=="shot"?UnityEngine.Random.Range(.96f,1.04f):1;source.PlayOneShot(clip,name=="shot"?.4f:.7f);}
            }
            string track=Model.Mode==Mode.Clear||Model.Mode==Mode.Complete?"":Model.Mode==Mode.Title?"menu":Model.Boss!=null&&!Model.Boss.Dead?"boss":Model.Stage.music;
            if(track!=musicKey){musicKey=track;if(track=="")music.Stop();AudioClip clip;if(audio.TryGetValue("bgm_"+track,out clip)){music.clip=clip;music.Play();}}
            if(Model.Score>highScore){highScore=Model.Score;PlayerPrefs.SetInt("HinaHighScore",highScore);}
            PlayerPrefs.SetInt("HinaUnlocked",Math.Min(5,Model.Unlocked));
            DrawWorld();
        }
        static Vector3 Position(float x,float y) { return new Vector3(x/64f,(310-y)/64f,0); }
        SpriteRenderer Next(Sprite sprite,int order,Color color)
        {
            if(drawIndex>=pool.Count){var go=new GameObject("Sprite "+drawIndex);go.transform.SetParent(transform,false);pool.Add(go.AddComponent<SpriteRenderer>());}
            var r=pool[drawIndex++];r.gameObject.SetActive(sprite!=null);r.sprite=sprite;r.sortingOrder=order;r.color=color;r.flipX=false;r.transform.localScale=Vector3.one;r.transform.rotation=Quaternion.identity;return r;
        }
        void Actor(Sprite sprite,float x,float y,int order=10,bool flip=false,Color? tint=null,float scale=1,float angle=0)
        {
            var r=Next(sprite,order,tint??Color.white);r.transform.position=Position(x,y);r.flipX=flip;r.transform.localScale=Vector3.one*scale;r.transform.rotation=Quaternion.Euler(0,0,angle);
        }
        void ImageRect(Sprite sprite,float x,float y,float w,float h,int order,Color? tint=null)
        {
            var r=Next(sprite,order,tint??Color.white);if(sprite==null)return;
            float sx=w/64f/sprite.bounds.size.x,sy=h/64f/sprite.bounds.size.y;
            r.transform.localScale=new Vector3(sx,sy,1);
            r.transform.position=Position(x+w/2,y+h/2)-new Vector3(sprite.bounds.center.x*sx,sprite.bounds.center.y*sy,0);
        }
        void DrawWorld()
        {
            drawIndex=0;float cameraX=Model.Mode==Mode.Title?320:Model.CameraX,left=cameraX-320;
            cam.transform.position=new Vector3(cameraX/64f,130/64f,-10);
            float screenRatio=(float)Screen.width/Math.Max(1,Screen.height),target=16/9f;
            cam.rect=screenRatio>target?new Rect((1-target/screenRatio)/2,0,target/screenRatio,1):new Rect(0,(1-screenRatio/target)/2,1,screenRatio/target);
            if(Model.Mode==Mode.Title)
            {
                var titleBackground=atlas.Frame("backgrounds","stage_0");float width=360*titleBackground.bounds.size.x/titleBackground.bounds.size.y;ImageRect(titleBackground,left-(width-640)*.25f,0,width,360,-100);
                Actor(atlas.Animate("hina","idle",animationTime),left+120,315,10,false,Color.white,2.4f);
                Actor(atlas.Animate("binah","idle",animationTime),left+540,310,8,false,new Color(1,1,1,.4f),.85f);
            }
            else
            {
                var scenery=atlas.Frame("backgrounds","stage_"+Model.StageIndex);float sceneryWidth=310*scenery.bounds.size.x/scenery.bounds.size.y;ImageRect(scenery,left-Math.Min(left*.5f,Math.Max(0,sceneryWidth-640)),0,sceneryWidth,310,-100);
                for(int i=-1;i<3;i++)ImageRect(atlas.Frame("backgrounds","ground_"+Model.StageIndex),(float)Math.Floor(left/768)*768+i*768,310,768,60,-50);
                foreach(var decoration in Model.Stage.decor??Array.Empty<DecorData>())
                {
                    float height=decoration.height>0?decoration.height:42;
                    ImageRect(atlas.Frame("props","prop_"+decoration.style.ToString("00")),decoration.x,(decoration.baseline>0?decoration.baseline:302)-height,height*1.3f,height,-10,new Color(.65f,.72f,.8f,.9f));
                }
                foreach(var platform in Model.Stage.platforms)ImageRect(atlas.Frame("props","prop_"+(Model.StageIndex==0?"05":"04")),platform.x,platform.y,platform.width,platform.height,0);
                foreach(var prop in Model.Props)
                {
                    if(prop.Dead&&prop.DeathTime>1.6f)continue;
                    string state=prop.Dead?"destroyed":prop.HP<=1?"breaking":prop.HP==2?"damaged":"intact";
                    ImageRect(atlas.Animate("prop_"+prop.Data.style.ToString("00"),state,prop.DeathTime),prop.Data.x,310-prop.Data.height,prop.Data.width,prop.Data.height,5,prop.Dead?new Color(1,1,1,Mathf.Clamp01((1.6f-prop.DeathTime)/.5f)):prop.HP==1?new Color(1,.75f,.7f):prop.HP==2?new Color(.9f,.9f,.88f):Color.white);
                }
                foreach(var e in Model.Enemies)
                {
                    string actor=EnemyActor(e.Kind);string state=e.Dead?"death":e.Hurt>0?"hurt":e.Recoil>0?"attack":Math.Abs(e.X-Model.Player.X)>130?"walk":"idle";var sprite=atlas.Animate(actor,state,e.Dead?e.DeathTime:e.Age);
                    Actor(sprite,e.X,e.Y,10,Model.Player.X>e.X,new Color(1,1,1,e.Dead?Mathf.Clamp01((.95f-e.DeathTime)/.3f):1),e.Kind>=8?1.3f:1);
                }
                if(Model.Boss!=null)
                {
                    var b=Model.Boss;string bossState=b.Dead?"death":b.Hurt>0?"hurt":b.Warning?"warn":b.Timer>1.4f?"attack":b.HP<b.MaxHP*.5f?"phase2":"idle";
                    if(Model.StageIndex==4&&b.CoreTime>0)bossState="phase2";
                    Actor(atlas.Animate(Model.Stage.boss,bossState,b.Dead?4.8f-Model.ClearTime:animationTime),b.X,310,8,false,Color.white);
                    foreach(var t in b.Targets)if(!t.Dead)Actor(atlas.Animate(t.Kind=="automata"?"chesed_drone":t.Kind,"idle",animationTime),t.X,t.Y,10,false,Color.white,t.Kind=="goz_clone"?2:t.Kind=="pillar"?1:.65f);
                }
                var p=Model.Player;Actor(atlas.Animate("hina",p.Alive?p.Animation:"death",playerStateTime),p.X,p.Y,15,p.Facing<0,new Color(1,1,1,p.Alive&&p.Invulnerable>0&&((int)(animationTime*14)%2)==0?.4f:1));
                foreach(var bullet in Model.Bullets)Actor(atlas.Frame("effects",bullet.Art??(bullet.Friendly?"friendly_bullet":"enemy_bullet")),bullet.X,bullet.Y,25,false,Color.white,.13f,Mathf.Atan2(-bullet.VY,bullet.VX)*Mathf.Rad2Deg);
                foreach(var item in Model.Pickups)Actor(atlas.Frame("effects",item.Special?"special_pickup":"score_pickup"),item.X,item.Y,20,false,Color.white,.32f);
                foreach(var fx in Model.Effects)
                {
                    bool animated=fx.Kind=="special"||fx.Kind=="explosion";
                    var sprite=animated?atlas.Animate("effects",fx.Kind,fx.Kind=="special"?1-fx.Life:.4f-fx.Life):atlas.Frame("effects",fx.Kind);
                    Actor(sprite,fx.X,fx.Y,30,fx.Facing<0,Color.white,fx.Kind=="special"?4:fx.Kind=="special_bullet"?.7f:animated?1:.2f,fx.Angle);
                }
            }
            for(int i=drawIndex;i<pool.Count;i++)pool[i].gameObject.SetActive(false);
        }
        static readonly string[] EnemyActors={"helmet","sukeban","sniper","shield","military","military","shield","military","tank","railgun"};
        static string EnemyActor(int kind) { return EnemyActors[Mathf.Clamp(kind,0,9)]; }
        void Style()
        {
            if(labelStyle!=null)return;
            labelStyle=new GUIStyle(GUI.skin.label){fontSize=13,font=korean,normal={textColor=new Color(.1f,.21f,.31f)}};
            titleStyle=new GUIStyle(labelStyle){fontSize=25,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            smallStyle=new GUIStyle(labelStyle){fontSize=10};
            buttonStyle=new GUIStyle(GUI.skin.button){font=korean,fontSize=14,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            buttonStyle.normal.background=buttonNormal;buttonStyle.hover.background=buttonHover;buttonStyle.focused.background=buttonHover;buttonStyle.active.background=buttonPressed;
            foreach(var state in new[]{buttonStyle.normal,buttonStyle.hover,buttonStyle.focused,buttonStyle.active})state.textColor=new Color(.08f,.21f,.34f);
        }
        void Panel(Rect rect)
        {
            GUI.color=new Color(.06f,.15f,.25f,.2f);GUI.DrawTexture(new Rect(rect.x+2,rect.y+3,rect.width,rect.height),white);GUI.color=new Color(.96f,.985f,1,.94f);GUI.DrawTexture(rect,white);GUI.color=new Color(.1f,.68f,.94f);GUI.DrawTexture(new Rect(rect.x,rect.y,rect.width,2),white);GUI.color=Color.white;
        }
        bool Button(float y,string text) { return GUI.Button(new Rect(200,y,240,30),text,buttonStyle); }
        void OnGUI()
        {
            if(Model==null||white==null)return;Style();float scale=Math.Min(Screen.width/640f,Screen.height/360f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-640*scale)/2,(Screen.height-360*scale)/2),Quaternion.identity,Vector3.one*scale);
            if(Model.Mode==Mode.Title)
            {
                Panel(new Rect(185,30,355,140));GUI.Label(new Rect(185,42,355,52),"PROJECT HINA",titleStyle);GUI.Label(new Rect(185,94,355,34),"OPERATION GEHENNA",titleStyle);
                GUI.Label(new Rect(205,137,320,24),"선도부장 히나 · CREDIT "+Model.Credits,labelStyle);
                if(!settings&&!selectingStage)
                {
                    if(Button(200,"출격 / START"))Model.Start();
                    if(Button(236,"작전 선택 / MISSIONS"))selectingStage=true;
                    if(Button(272,"설정 / OPTIONS"))settings=true;
                    if(Application.platform!=RuntimePlatform.WebGLPlayer&&Button(308,"게임 종료 / EXIT"))Application.Quit();
                }
                if(settings)Settings();if(selectingStage)StageSelection();
                Panel(new Rect(0,338,640,22));GUI.Label(new Rect(20,339,600,18),"A/D MOVE · J FIRE · K JUMP · L SPECIAL · 1 COIN · ESC PAUSE",smallStyle);return;
            }
            float left=Model.CameraX-320;

            foreach(var h in Model.Hazards){GUI.color=h.Delay>0?new Color(1,.25f,.3f,.18f+.12f*Mathf.Sin(animationTime*12)):new Color(1,.4f,.2f,.8f);GUI.DrawTexture(new Rect(h.X-left,h.Y,h.Width,h.Height),white);}

            GUI.color=Color.white;
            Panel(new Rect(10,8,180,46));GUI.Label(new Rect(18,10,170,22),"HINA    "+Model.Score.ToString("0000000"),labelStyle);GUI.Label(new Rect(18,30,170,20),"1UP "+Model.Lives+"    BOMB "+Model.Player.Stock,labelStyle);
            GUI.Label(new Rect(275,8,100,35),Mathf.CeilToInt(Model.MissionTime).ToString("00"),titleStyle);
            GUI.Label(new Rect(455,10,180,40),"HI "+highScore.ToString("0000000")+"\nMISSION "+(Model.StageIndex+1),labelStyle);
            Panel(new Rect(0,336,640,24));GUI.Label(new Rect(18,339,610,18),"A/D MOVE   J FIRE/KICK   K JUMP   L SPECIAL   1 COIN   ESC PAUSE",smallStyle);
            if(Model.Boss!=null&&!Model.Boss.Dead)
            {
                Panel(new Rect(120,62,400,28));GUI.color=new Color(.07f,.61f,.9f);GUI.DrawTexture(new Rect(125,81,390*Mathf.Clamp01(Model.Boss.HP/Model.Boss.MaxHP),4),white);GUI.color=Color.white;
                GUI.Label(new Rect(125,62,390,22),Model.Stage.boss.ToUpper()+" / "+Model.Boss.Attack,smallStyle);
            }
            if(Model.Mode==Mode.Playing&&Model.StageElapsed<2.2f)
            {
                Panel(new Rect(185,133,270,68));GUI.Label(new Rect(185,137,270,35),"MISSION "+(Model.StageIndex+1).ToString("00"),titleStyle);GUI.Label(new Rect(205,176,250,22),Model.Stage.name,labelStyle);
            }
            if(Model.Mode==Mode.Paused)
            {
                Panel(new Rect(150,95,340,220));GUI.Label(new Rect(150,105,340,40),"일시정지 / PAUSE",titleStyle);
                if(Button(153,"계속 / RESUME"))Model.TogglePause();if(Button(191,"재시작 / RESTART"))Model.Load(Model.StageIndex);
                if(Button(229,"설정 / OPTIONS"))settings=!settings;if(Button(267,"메인 메뉴 / TITLE"))Model.Mode=Mode.Title;
            }
            if(Model.Mode==Mode.Continue||Model.Mode==Mode.GameOver)
            {
                Panel(new Rect(140,115,360,145));GUI.Label(new Rect(140,123,360,45),Model.Mode==Mode.Continue?"CONTINUE? "+Mathf.CeilToInt(Model.ContinueTime):"GAME OVER",titleStyle);
                if(Button(182,"1 코인 투입 / INSERT COIN"))Model.InsertCoin();
            }
            if(Model.Mode==Mode.Clear||Model.Mode==Mode.Complete){Panel(new Rect(130,125,380,110));GUI.Label(new Rect(130,135,380,80),Model.Mode==Mode.Complete?"ALL MISSIONS COMPLETE":"MISSION COMPLETE",titleStyle);if(Model.Mode==Mode.Complete&&Button(250,"메인 메뉴 / TITLE"))Model.Mode=Mode.Title;}
            if(settings)Settings();
        }
        void StageSelection()
        {
            Panel(new Rect(145,54,350,270));GUI.Label(new Rect(145,62,350,34),"작전 선택",titleStyle);
            for(int i=0;i<Model.Stages.Length;i++)
            {
                GUI.enabled=i<Model.Unlocked;
                if(GUI.Button(new Rect(165,104+i*32,310,28),(i+1).ToString("00")+"  "+Model.Stages[i].name,buttonStyle)){selectingStage=false;Model.Start(i);}
            }
            GUI.enabled=true;if(GUI.Button(new Rect(165,274,310,28),"돌아가기 / BACK",buttonStyle))selectingStage=false;
        }
        static Texture2D SolidTexture(Color color) { var texture=new Texture2D(1,1);texture.SetPixel(0,0,color);texture.Apply();return texture; }
        void OnApplicationFocus(bool focused) { if(!focused&&!Application.isBatchMode&&Model!=null&&Model.Mode==Mode.Playing)Model.TogglePause(); }
        void OnDestroy() { foreach(var texture in new[]{white,buttonNormal,buttonHover,buttonPressed})if(texture!=null)Destroy(texture); }
        void Settings()
        {
            Panel(new Rect(120,110,400,200));GUI.Label(new Rect(140,125,350,30),"음량 / VOLUME",labelStyle);
            masterVolume=GUI.HorizontalSlider(new Rect(145,165,350,20),masterVolume,0,1);AudioListener.volume=masterVolume;
            if(GUI.Button(new Rect(145,200,350,30),"전체 화면 전환 / FULLSCREEN",buttonStyle))Screen.fullScreen=!Screen.fullScreen;
            if(GUI.Button(new Rect(145,250,350,30),"닫기 / CLOSE",buttonStyle)){settings=false;PlayerPrefs.SetFloat("HinaVolume",masterVolume);PlayerPrefs.Save();}
        }
        void OnApplicationQuit() { PlayerPrefs.Save(); }
    }
}
