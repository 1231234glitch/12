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
        float animationTime,masterVolume=.8f,accumulator;
        bool shoot,jump,special;
        float movement;bool up,down;
        GUIStyle titleStyle,labelStyle,smallStyle,buttonStyle;
        Texture2D white;
        Font korean;
        bool settings;
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
            cam=Camera.main;if(cam==null)cam=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            cam.orthographic=true;cam.orthographicSize=180/64f;cam.backgroundColor=new Color(.08f,.12f,.2f);cam.clearFlags=CameraClearFlags.SolidColor;
            if(FindObjectOfType<AudioListener>()==null)cam.gameObject.AddComponent<AudioListener>();
            music=gameObject.AddComponent<AudioSource>();music.loop=true;music.volume=.35f;
            for(int i=0;i<16;i++){var source=gameObject.AddComponent<AudioSource>();sfx.Add(source);}
            foreach(var clip in Resources.LoadAll<AudioClip>("Hina/Audio"))audio[clip.name]=clip;
            masterVolume=PlayerPrefs.GetFloat("HinaVolume",.8f);AudioListener.volume=masterVolume;
            white=new Texture2D(1,1);white.SetPixel(0,0,Color.white);white.Apply();
        }
        void Update()
        {
            if(Model==null)return;
            movement=Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?-1:Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0;
            up=Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow);down=Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow);
            shoot=Input.GetKey(KeyCode.J);jump|=Input.GetKeyDown(KeyCode.K);special|=Input.GetKeyDown(KeyCode.L);
            if(Input.GetKeyDown(KeyCode.Alpha1)||Input.GetKeyDown(KeyCode.Keypad1))Model.InsertCoin();
            if(Input.GetKeyDown(KeyCode.Escape)){settings=false;Model.TogglePause();}
            if(Input.GetKeyDown(KeyCode.Return)&&Model.Mode==Mode.Title)Model.Start();
            accumulator+=Math.Min(Time.unscaledDeltaTime,.15f);
            while(accumulator>=1/60f)
            {
                Model.Tick(1/60f,new Controls{Move=movement,Up=up,Down=down,Shoot=shoot,Jump=jump,Special=special});
                jump=false;special=false;accumulator-=1/60f;
            }
            if(Model.Mode==Mode.Playing||Model.Mode==Mode.Title)animationTime+=Time.unscaledDeltaTime;
            while(Model.Sounds.Count>0)
            {
                string name=Model.Sounds.Dequeue().Name;AudioClip clip;
                if(audio.TryGetValue(name,out clip)){var source=sfx[soundIndex++%sfx.Count];source.pitch=name=="shot"?UnityEngine.Random.Range(.96f,1.04f):1;source.PlayOneShot(clip,name=="shot"?.4f:.7f);}
            }
            string track=Model.Mode==Mode.Title?"menu":Model.Boss!=null&&!Model.Boss.Dead?"boss":Model.Stage.music;
            if(track!=musicKey){musicKey=track;AudioClip clip;if(audio.TryGetValue("bgm_"+track,out clip)){music.clip=clip;music.Play();}}
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
        void Actor(Sprite sprite,float x,float y,int order=10,bool flip=false,Color? tint=null,float scale=1)
        {
            var r=Next(sprite,order,tint??Color.white);r.transform.position=Position(x,y);r.flipX=flip;r.transform.localScale=Vector3.one*scale;
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
            if(Model.Mode==Mode.Title)ImageRect(atlas.Frame("title","title"),left,0,640,360,-100);
            else
            {
                ImageRect(atlas.Frame("backgrounds","stage_"+Model.StageIndex),left-Math.Min(left*.3f,896),0,1536,310,-100);
                for(int i=-1;i<3;i++)ImageRect(atlas.Frame("ground","ground_"+Model.StageIndex),(float)Math.Floor(left/768)*768+i*768,310,768,60,-50);
                foreach(var p in Model.Stage.platforms)ImageRect(atlas.Frame("props","prop_"+(Model.StageIndex==0?"05":"04")),p.x,p.y,p.width,p.height,0);
                foreach(var prop in Model.Props)if(!prop.Dead)ImageRect(atlas.Frame("props","prop_"+prop.Data.style.ToString("00")),prop.Data.x,310-prop.Data.height,prop.Data.width,prop.Data.height,5);
                foreach(var e in Model.Enemies)
                {
                    string actor=EnemyActor(e.Kind);string state=e.Dead?"death":"walk";var sprite=atlas.Animate(actor,state,e.Dead?e.DeathTime:e.Age);
                    Actor(sprite,e.X,e.Y,10,Model.Player.X>e.X,new Color(1,1,1,e.Dead?Mathf.Clamp01((.95f-e.DeathTime)/.3f):1));
                }
                if(Model.Boss!=null)
                {
                    var b=Model.Boss;Actor(atlas.Animate(Model.Stage.boss,"idle",animationTime),b.X,310,8,false,b.Dead?new Color(1,1,1,.2f):Color.white);
                    foreach(var t in b.Targets)if(!t.Dead)Actor(atlas.Animate(t.Kind=="automata"?"military":t.Kind=="peroro"?"perorodzilla":"hod","idle",animationTime),t.X,t.Y,10,false,Color.white,t.Kind=="automata"?1:.4f);
                }
                var p=Model.Player;Actor(atlas.Animate("hina",p.Alive?p.Animation:"death",animationTime),p.X,p.Y,15,p.Facing<0,new Color(1,1,1,p.Alive&&p.Invulnerable>0&&((int)(animationTime*14)%2)==0?.4f:1));
                foreach(var bullet in Model.Bullets)Actor(atlas.Frame("items",bullet.Friendly?"friendly_bullet":"enemy_bullet"),bullet.X,bullet.Y,25);
                foreach(var item in Model.Pickups)Actor(atlas.Frame("items",item.Special?"special_pickup":"score_pickup"),item.X,item.Y,20);
                foreach(var fx in Model.Effects)Actor(atlas.Animate("effects","explosion",.4f-fx.Life),fx.X,fx.Y,30,false,Color.white,fx.Kind=="special"?4:1);
            }
            for(int i=drawIndex;i<pool.Count;i++)pool[i].gameObject.SetActive(false);
        }
        static string EnemyActor(int kind) { return new[]{"helmet","sukeban","sniper","shield","military","military","shield","military","tank","railgun"}[Mathf.Clamp(kind,0,9)]; }
        void Style()
        {
            if(labelStyle!=null)return;
            labelStyle=new GUIStyle(GUI.skin.label){fontSize=13,font=korean,normal={textColor=new Color(.96f,.94f,.8f)}};
            titleStyle=new GUIStyle(labelStyle){fontSize=25,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            smallStyle=new GUIStyle(labelStyle){fontSize=10};
            buttonStyle=new GUIStyle(GUI.skin.button){font=korean,fontSize=14};
        }
        void Panel(Rect rect)
        {
            GUI.color=new Color(.06f,.12f,.18f,.95f);GUI.DrawTexture(rect,white);GUI.color=new Color(.9f,.8f,.55f);GUI.DrawTexture(new Rect(rect.x,rect.y,rect.width,2),white);GUI.color=Color.white;
        }
        bool Button(float y,string text) { return GUI.Button(new Rect(200,y,240,30),text,buttonStyle); }
        void OnGUI()
        {
            if(Model==null||white==null)return;Style();float scale=Math.Min(Screen.width/640f,Screen.height/360f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-640*scale)/2,(Screen.height-360*scale)/2),Quaternion.identity,Vector3.one*scale);
            if(Model.Mode==Mode.Title)
            {
                if(Button(222,"출격 / START"))Model.Start();
                if(Button(258,"설정 / OPTIONS"))settings=!settings;
                if(Button(294,"게임 종료 / EXIT"))Application.Quit();
                if(settings)Settings();GUI.Label(new Rect(20,338,600,18),"A/D MOVE · J FIRE · K JUMP · L SPECIAL · 1 COIN · ESC PAUSE",smallStyle);return;
            }
            float left=Model.CameraX-320;

            foreach(var h in Model.Hazards){GUI.color=h.Delay>0?new Color(1,.25f,.3f,.18f+.12f*Mathf.Sin(animationTime*12)):new Color(1,.4f,.2f,.8f);GUI.DrawTexture(new Rect(h.X-left,h.Y,h.Width,h.Height),white);}

            GUI.color=Color.white;
            Panel(new Rect(10,8,180,46));GUI.Label(new Rect(18,10,170,22),"HINA    "+Model.Score.ToString("0000000"),labelStyle);GUI.Label(new Rect(18,30,170,20),"1UP "+Model.Lives+"    BOMB "+Model.Player.Stock,labelStyle);
            GUI.Label(new Rect(275,8,100,35),Mathf.CeilToInt(Model.MissionTime).ToString("00"),titleStyle);
            GUI.Label(new Rect(455,10,180,40),"HI "+highScore.ToString("0000000")+"\nMISSION "+(Model.StageIndex+1),labelStyle);
            GUI.Label(new Rect(18,339,610,18),"A/D MOVE   J FIRE/KICK   K JUMP   L SPECIAL   1 COIN   ESC PAUSE",smallStyle);
            if(Model.Boss!=null&&!Model.Boss.Dead)
            {
                Panel(new Rect(120,62,400,28));GUI.color=new Color(.8f,.3f,.5f);GUI.DrawTexture(new Rect(125,81,390*Mathf.Clamp01(Model.Boss.HP/Model.Boss.MaxHP),4),white);GUI.color=Color.white;
                GUI.Label(new Rect(125,62,390,22),Model.Stage.boss.ToUpper()+" / "+Model.Boss.Attack,smallStyle);
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
