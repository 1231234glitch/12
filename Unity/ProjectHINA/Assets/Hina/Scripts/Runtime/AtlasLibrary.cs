using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hina.Unity
{
    [Serializable] public class SheetDefinition { public string id; public int cellWidth,cellHeight,columns; public float pivotX=.5f,pivotY; public string[] frames; public string provenance; }
    [Serializable] public class AnimationDefinition { public string actor,state,atlas; public string[] frames; public float fps=12; public bool loop=true; }
    [Serializable] public class AtlasManifest { public SheetDefinition[] atlases; public AnimationDefinition[] animations; }
    public sealed class AtlasLibrary
    {
        readonly Dictionary<string,Sprite> frames=new Dictionary<string,Sprite>();
        readonly Dictionary<string,AnimationDefinition> animations=new Dictionary<string,AnimationDefinition>();
        public AtlasLibrary()
        {
            var text=Resources.Load<TextAsset>("Hina/atlas_manifest");
            if(text==null)throw new InvalidOperationException("Missing atlas_manifest.json; run Tools/prepare_unity_assets.py");
            var manifest=JsonUtility.FromJson<AtlasManifest>(text.text);
            foreach(var sheet in manifest.atlases)foreach(var sprite in Resources.LoadAll<Sprite>("Hina/Sheets/"+sheet.id))frames[sheet.id+"/"+sprite.name]=sprite;
            foreach(var anim in manifest.animations)animations[anim.actor+"/"+anim.state]=anim;
        }
        public Sprite Frame(string atlas,string frame)
        {
            Sprite sprite;return frames.TryGetValue(atlas+"/"+frame,out sprite)?sprite:null;
        }
        public Sprite Animate(string actor,string state,float time)
        {
            AnimationDefinition a;if(!animations.TryGetValue(actor+"/"+state,out a)&&!animations.TryGetValue(actor+"/idle",out a))return null;
            int index=Math.Max(0,(int)(time*a.fps));index=a.loop?index%a.frames.Length:Math.Min(index,a.frames.Length-1);
            return Frame(a.atlas,a.frames[index]);
        }
    }
}
