using System.IO;
using Hina.Unity;
using UnityEditor;
using UnityEngine;

namespace Hina.Editor
{
    // Slice by named cells, so replacing the PNG preserves gameplay mappings.
    public class SheetImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Resources/Hina/Sheets/")||!assetPath.EndsWith(".png"))return;
            string manifestPath="Assets/Hina/Resources/Hina/atlas_manifest.json";
            if(!File.Exists(manifestPath))return;
            var manifest=JsonUtility.FromJson<AtlasManifest>(File.ReadAllText(manifestPath));
            string id=Path.GetFileNameWithoutExtension(assetPath);
            var definition=System.Array.Find(manifest.atlases,x=>x.id==id);
            if(definition==null)return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=64;importer.filterMode=FilterMode.Point;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=8192;importer.npotScale=TextureImporterNPOTScale.None;
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            var cells=new SpriteMetaData[definition.frames.Length];
            for(int i=0;i<cells.Length;i++)cells[i]=new SpriteMetaData{name=definition.frames[i],rect=new Rect(i%definition.columns*definition.cellWidth,height-(i/definition.columns+1)*definition.cellHeight,definition.cellWidth,definition.cellHeight),alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(definition.pivotX,definition.pivotY)};
            importer.spritesheet=cells;
        }
    }
}
