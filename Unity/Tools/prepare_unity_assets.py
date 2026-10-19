"""Pack the existing generated stand-ins into Unity sprite sheets.

These are NOT official Blue Archive assets. Supply original sheets with
--original-dir to replace matching atlas IDs, and record their source in
original_sources.json. Never mark generated assets as original automatically.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import argparse,csv,json,shutil,hashlib
UNITY_ROOT=Path(__file__).resolve().parents[1]
BASE=UNITY_ROOT.parent
PROJECT=UNITY_ROOT/'ProjectHINA'
OUT=PROJECT/'Assets/Hina/Resources/Hina'
parser=argparse.ArgumentParser();parser.add_argument('--original-dir',type=Path);args=parser.parse_args()
# Standalone delivery can replace prepared sheets without the previous project.
if not (BASE/'data/animation_manifest.json').exists():
    if not args.original_dir:parser.error('Use --original-dir to replace sheets in the standalone Unity package. Regenerating stand-ins requires the earlier project.')
    existing=json.loads((OUT/'atlas_manifest.json').read_text())
    metadata=json.loads((args.original_dir/'original_sources.json').read_text())
    replacements=[]
    for a in existing['atlases']:
        source=args.original_dir/(a['id']+'.png')
        if not source.exists():continue
        candidate=Image.open(source);current=Image.open(OUT/'Sheets'/(a['id']+'.png'))
        if candidate.size!=current.size:raise ValueError(a['id']+': sheet dimensions do not match')
        if not metadata.get(a['id'],{}).get('source'):raise ValueError(a['id']+': missing source record')
        replacements.append((source,OUT/'Sheets'/source.name,a))
    for source,target,a in replacements:
        shutil.copy2(source,target);a['provenance']='user_supplied_original_unverified'
    existing['original_assets_complete']=False
    (OUT/'atlas_manifest.json').write_text(json.dumps(existing,ensure_ascii=False,indent=2))
    print('Replaced',len(replacements),'sheets; source authenticity remains unverified. Reimport in Unity.')
    raise SystemExit(0)

(OUT/'Sheets').mkdir(parents=True,exist_ok=True)
manifest={'atlases':[],'animations':[]};requirements=[]

def add_sheet(id,frames,w,h,columns=8,pivotY=0):
    im=Image.new('RGBA',(w*columns,h*((len(frames)+columns-1)//columns)))
    for i,(_,source) in enumerate(frames):
        tile=source if isinstance(source,Image.Image) else Image.open(source).convert('RGBA')
        if tile.width>w or tile.height>h:tile.thumbnail((w,h),Image.Resampling.NEAREST)
        im.alpha_composite(tile,(i%columns*w+(w-tile.width)//2,i//columns*h+h-tile.height))
    provenance='generated_placeholder'
    if args.original_dir and (args.original_dir/(id+'.png')).exists():
        candidate=Image.open(args.original_dir/(id+'.png')).convert('RGBA')
        if candidate.size!=im.size:raise ValueError(f'{id}: expected {im.size}, got {candidate.size}')
        sources=args.original_dir/'original_sources.json'
        records=json.loads(sources.read_text()) if sources.exists() else {}
        if not records.get(id,{}).get('source'):raise ValueError(f'{id}: original_sources.json needs a source record')
        im=candidate;provenance='user_supplied_original_unverified'
    im.save(OUT/'Sheets'/f'{id}.png')
    manifest['atlases'].append({'id':id,'cellWidth':w,'cellHeight':h,'columns':columns,'pivotX':.5,'pivotY':pivotY,'frames':[x[0] for x in frames],'provenance':provenance})

def anim(actor,state,atlas,frames,fps=12,loop=True):
    manifest['animations'].append(dict(actor=actor,state=state,atlas=atlas,frames=frames,fps=fps,loop=loop))

def requirement(id,identity,atlas,animations,source=''):
    requirements.append({'id':id,'blue_archive_identity':identity,'atlas':atlas,'required_animations':animations,'original_source_reference':source,'original_asset_status':'MISSING','runtime_asset_status':'GENERATED_PLACEHOLDER'})

player_manifest=json.loads((BASE/'data/animation_manifest.json').read_text())
files=sorted(set(f['file'] for v in player_manifest.values() for f in v['frames']))
add_sheet('player',[(Path(f).stem,BASE/f) for f in files],128,128,pivotY=28/128)
for state,v in player_manifest.items():anim('hina',state,'player',[Path(f['file']).stem for f in v['frames']],v['fps'],v['loop'])
requirement('hina','소라사키 히나 / Sorasaki Hina','player','idle,walk,shoot,crouch,jump,air_shoot,air_down,kick,special,hurt,death','07c7b83a-1d98-4df3-b916-7f1b3f28ad06.png (not supplied)')
enemy_frames=[]
identities={'helmet':'헬멧단','sukeban':'스케반','sniper':'스케반 저격병 (원본 확인 필요)','shield':'카이저 방패 자동기계','military':'카이저 자동기계','tank':'카이저 전차','railgun':'카이저 레일건 전차 (원본 확인 필요)'}
for actor in identities:
    for state,count in [('idle',1),('walk',6),('death',4)]:
        names=[]
        for i in range(count):
            path=BASE/(f'assets/enemies_v9/{actor}_walk_{i}.png' if state=='walk' else f'assets/enemies_v8/{actor}_death_{i}.png' if state=='death' else f'assets/enemies_v8/{actor}_0.png')
            name=f'{actor}_{state}_{i}';enemy_frames.append((name,path));names.append(name)
        anim(actor,state,'enemies',names,8 if state=='death' else 10,state!='death')
    requirement(actor,identities[actor],'enemies','idle,walk,attack,hurt,death')
add_sheet('enemies',enemy_frames,160,96)
boss_frames=[]
for i,(actor,name) in enumerate([('binah','비나'),('perorodzilla','페로로지라'),('hod','호드'),('goz','고즈'),('chesed','헤세드')]):
    frames=[]
    source=Image.open(BASE/f'assets/bosses_v7/boss_{i}.png').convert('RGBA')
    for n in range(4):
        key=f'{actor}_idle_{n}';frames.append(key)
        boss_frames.append((key,source.rotate([0,1,0,-1][n],resample=Image.Resampling.NEAREST)))
    anim(actor,'idle','bosses',frames,4)
    requirement(actor,name,'bosses','intro,idle,attack,phase2,weak_point,defeat')
add_sheet('bosses',boss_frames,256,224,columns=4)
add_sheet('props',[(f'prop_{i:02}',BASE/f'assets/props_v9/prop_{i:02}.png') for i in range(12)],96,64,columns=4)
for i,name in enumerate(['보급 상자','폭발 드럼통','군용 엄폐물','금속 상자','철도 발판','부두 발판','보급 장비','유적 발판','군수 시설','방벽','시설 장치','파괴 잔해']):requirement(f'prop_{i:02}',name+' (블루 아카이브 원본 지정 필요)','props','intact,damaged,destroyed')
add_sheet('backgrounds',[(f'stage_{i}',BASE/f'assets/backgrounds_v6/stage_{i}.png') for i in range(5)],1536,310,columns=1)
add_sheet('ground',[(f'ground_{i}',BASE/f'assets/backgrounds_v6/ground_{i}.png') for i in range(5)],768,60,columns=1)
add_sheet('title',[('title',BASE/'assets/backgrounds_v9/title.png')],640,360,columns=1)
add_sheet('effects',[(f'ex_{i}',BASE/f'assets/effects/ex_{i}.png') for i in range(4)],64,64,columns=4,pivotY=.5)
anim('effects','explosion','effects',[f'ex_{i}' for i in range(4)],10,False)
items=[]
for name,color in [('friendly_bullet','#ffe3a0'),('enemy_bullet','#ff6c91'),('score_pickup','#efc878'),('special_pickup','#c796ee')]:
    im=Image.new('RGBA',(16,16));d=ImageDraw.Draw(im)
    if 'bullet' in name:d.rectangle((2,6,13,9),fill=color);d.rectangle((9,7,14,8),fill='white')
    else:d.rectangle((2,2,13,13),fill='#243548',outline=color,width=2);d.ellipse((5,5,10,10),fill=color)
    items.append((name,im));requirement(name,'블루 아카이브 '+name+' 원본 지정 필요','items','idle')
add_sheet('items',items,16,16,columns=4,pivotY=.5)
for i in range(5):requirement(f'background_{i+1}',f'스테이지 {i+1} 블루 아카이브 배경·지형 원본 지정 필요','backgrounds,ground','static/tile')
requirement('effects','블루 아카이브 사격·폭발·히나 EX 이펙트','effects','muzzle,impact,explosion,special')
manifest['original_assets_complete']=all(x['provenance']!='generated_placeholder' for x in manifest['atlases'])
(OUT/'atlas_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2))
stages=json.loads((BASE/'data/stages.json').read_text())
for stage in stages:stage['platforms']=[dict(zip(['x','y','width','height'],r)) for r in stage['platforms']]
(OUT/'stages.json').write_text(json.dumps({'stages':stages},indent=2))
audio=OUT/'Audio';audio.mkdir(exist_ok=True)
for p in (BASE/'assets/sounds').iterdir():
    if p.suffix in ['.ogg','.wav']:shutil.copy2(p,audio/p.name)
shutil.copy2(BASE/'assets/fonts/NotoSansCJKkr-Regular.otf',OUT/'NotoSansCJKkr-Regular.otf')
shutil.copy2(BASE/'assets/fonts/LICENSE.txt',OUT/'FONT_LICENSE.txt')
with (BASE/'Unity/ASSET_REQUIREMENTS.csv').open('w',newline='',encoding='utf-8-sig') as f:
    writer=csv.DictWriter(f,fieldnames=requirements[0].keys());writer.writeheader();writer.writerows(requirements)
# A separate visual index permits artwork review without opening Unity.
thumb=Image.new('RGB',(960,((len(requirements)+5)//6)*110),'#172434');d=ImageDraw.Draw(thumb)
for i,r in enumerate(requirements):
    x=i%6*160;y=i//6*110;d.rectangle((x+3,y+3,x+157,y+106),outline='#526275');d.text((x+8,y+8),r['id'],fill='#efdbad');d.text((x+8,y+87),'ORIGINAL MISSING',fill='#ff9b9b')
    a=next((a for a in manifest['animations'] if a['actor']==r['id'] and a['state']=='idle'),None)
    if a is None:
        sheet_id=r['atlas'].split(',')[0];sheet=next((v for v in manifest['atlases'] if v['id']==sheet_id),None)
        key=r['id']
        if key.startswith('background_'):key='stage_'+str(int(key.split('_')[1])-1)
        if sheet and key in sheet['frames']:a={'atlas':sheet_id,'frames':[key]}
        elif sheet:a={'atlas':sheet_id,'frames':[sheet['frames'][0]]}
    if a:
        sheet=next(s for s in manifest['atlases'] if s['id']==a['atlas']);idx=sheet['frames'].index(a['frames'][0]);im=Image.open(OUT/'Sheets'/(sheet['id']+'.png'));w,h=sheet['cellWidth'],sheet['cellHeight'];tile=im.crop((idx%sheet['columns']*w,idx//sheet['columns']*h,(idx%sheet['columns']+1)*w,(idx//sheet['columns']+1)*h));tile.thumbnail((130,60));thumb.paste(tile,(x+80-tile.width//2,y+24),tile)
thumb.save(BASE/'Unity/SPRITE_REVIEW.png')
print(json.dumps({'atlases':len(manifest['atlases']),'animations':len(manifest['animations']),'asset_requirements':len(requirements),'original_assets_complete':manifest['original_assets_complete']}))
