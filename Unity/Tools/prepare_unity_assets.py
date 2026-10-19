"""Install newly designed reference-based artwork and build Unity frame metadata.

Original PNGs are copied without resizing, palette reduction or pixel editing.
Sprite slicing is metadata only; variable frame rectangles handle art-sheet rows.
Run with --generated-dir to install the delivered artwork, or without arguments
inside the standalone project to refresh metadata for the already included PNGs.
"""
from pathlib import Path
from PIL import Image
import argparse,csv,hashlib,json,shutil
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'ProjectHINA/Assets/Hina/Resources/Hina'
parser=argparse.ArgumentParser();parser.add_argument('--generated-dir',type=Path);args=parser.parse_args()
FILES={'player':'exec-9b16352c-03bc-4eb3-b7df-fc7fc40e0d34.png','enemies':'exec-f30078c1-3960-4c7c-aeb9-7d23c950785f.png','bosses':'exec-fa90fc7c-3ac8-4e9f-8d7b-5acd7977ebef.png','props':'exec-3f58981f-f5c4-467b-abb1-75d15174af89.png','backgrounds':'exec-07d94fa3-78b8-44b2-ac40-5cedb13f48d3.png'}
(OUT/'Sheets').mkdir(parents=True,exist_ok=True)
if args.generated_dir:
    for name,source in FILES.items():shutil.copy2(args.generated_dir/source,OUT/'Sheets'/(name+'.png'))
# All legacy stand-in images are replaced, not mixed into the new build.
for p in (OUT/'Sheets').glob('*.png'):
    if p.stem not in FILES:p.unlink()
manifest={'design_assets_complete':True,'official_assets_used':False,'style_constraint':'No pixel-art requirement; retain full-resolution full-color RGBA images','atlases':[],'animations':[]}
source_records=[];requirements=[];definitions={}

def sheet(id,rows,counts,ppu):
    im=Image.open(OUT/'Sheets'/(id+'.png')).convert('RGBA');w,h=im.size
    d={'id':id,'cellWidth':0,'cellHeight':0,'columns':8,'pivotX':.5,'pivotY':0,'pixelsPerUnit':ppu,'smoothFiltering':True,'frames':[],'frameRects':[],'provenance':'new_reference_based_design'}
    definitions[id]=(d,im,rows,counts);manifest['atlases'].append(d)
    source_records.append({'atlas':id,'file':f'Sheets/{id}.png','sha256':hashlib.file_digest((OUT/'Sheets'/(id+'.png')).open('rb'),'sha256').hexdigest(),'width':w,'height':h,'creation':'Newly designed and generated for this project, referencing Blue Archive character/faction/raid concepts. Not official extracted assets.','modified_pixels':False})
    assert rows[0]==0 and rows[-1]==h
    return d

def frame(atlas,name,row,col,pivotX=.5):
    d,im,rows,counts=definitions[atlas];cols=counts[row]
    x0=round(im.width*col/cols);x1=round(im.width*(col+1)/cols);y0,y1=rows[row:row+2]
    # Find foot baseline from actual alpha, keeping the original image unchanged.
    bbox=im.getchannel('A').crop((x0,y0,x1,y1)).point(lambda x:255 if x>128 else 0).getbbox()
    pivotY=(y1-y0-bbox[3]+1)/(y1-y0) if bbox else 0
    d['frames'].append(name);d['frameRects'].append({'x':x0,'y':im.height-y1,'width':x1-x0,'height':y1-y0,'pivotX':pivotX,'pivotY':max(0,min(1,pivotY))})
    return name

def explicit(atlas,name,x,y,w,h):
    d=definitions[atlas][0];d['frames'].append(name);d['frameRects'].append({'x':x,'y':y,'width':w,'height':h,'pivotX':.5,'pivotY':0});return name

def anim(actor,state,atlas,frames,fps=10,loop=True):manifest['animations'].append(dict(actor=actor,state=state,atlas=atlas,frames=frames,fps=fps,loop=loop))
def asset(id,reference,design,atlas):requirements.append({'id':id,'reference_setting':reference,'new_design':design,'atlas':atlas,'status':'NEW_DESIGN_APPLIED','official_asset_required':'NO'})

sheet('player',[0,150,303,473,624,775,933,1083,1254],[8]*8,128)
for r in range(8):
    for c in range(8):frame('player',f'hina_{r}_{c}',r,c,.59)
def pf(row,cols):return [f'hina_{row}_{c}' for c in cols]
for state,frames,fps,loop in [
 ('idle',pf(5,range(8)),5,True),('walk',pf(0,range(8)),13,True),('walk_shoot',pf(0,range(8)),13,True),('shoot',pf(6,range(8)),14,True),
 ('jump_rise',pf(1,[0,1]),9,False),('jump_apex',pf(1,[2,3]),8,True),('fall',pf(1,[2,3]),8,True),('air_shoot',pf(2,range(4)),12,True),('air_down',pf(1,range(4,8)),12,True),
 ('shoot_up',pf(2,[4,5]),10,True),('shoot_diagonal',pf(2,[6,7]),10,True),('crouch',pf(3,[2]),8,True),('crouch_shoot',pf(3,[2]),8,True),
 ('kick',pf(4,range(6)),15,False),('air_kick',pf(4,[6,7,4,5]),12,False),('special',pf(7,range(8)),5,False),('hurt',pf(3,[5]),8,False),('death',pf(3,[6]),8,False),('land',pf(3,[7,0]),14,False)]:anim('hina',state,'player',frames,fps,loop)
asset('hina','Blue Archive / Sorasaki Hina: silver hair, violet eyes, horns, halo, wings, military/gothic silhouette','Violet Breakwater: redesigned armor/clothing/weapon details and newly drawn action frames','player')

sheet('enemies',[0,160,320,470,630,783,924,1072,1247],[8]*8,128)
enemies=[('helmet','Helmet Gang: white safety helmet, delinquent uniform'),('sukeban','Sukeban: delinquent school-uniform faction'),('sniper','Sukeban marksman role'),('shield','Kaiser PMC shield automaton'),('military','Kaiser PMC infantry automaton'),('tank','Kaiser tracked combat vehicle'),('railgun','Kaiser heavy electromagnetic gun vehicle')]
for r,(actor,reference) in enumerate(enemies):
    names=[frame('enemies',f'{actor}_{c}',r,c) for c in range(8)]
    for state,indices,loop in [('idle',[0],True),('walk',[1,2,3,4],True),('attack',[5],False),('hurt',[6],False),('death',[6,7,7],False)]:anim(actor,state,'enemies',[names[i] for i in indices],10,loop)
    asset(actor,reference,'New faction-specific uniform/chassis, weapon, motion, recoil and damaged states','enemies')

sheet('bosses',[0,160,314,473,628,783,880,979,1122],[8,8,8,8,8,9,9,8],64)
bosses=[('binah','Binah / gold articulated mechanical serpent'),('perorodzilla','Perorodzilla / colossal white Peroro chicken mascot'),('hod','HOD / neural machine tower and cable-tentacle roots'),('goz','Goz / enormous cat magician in tuxedo and top hat'),('chesed','Chesed / spherical computation core with opening armored shell')]
for r,(actor,reference) in enumerate(bosses):
    names=[frame('bosses',f'{actor}_{c}',r,c) for c in range(8)]
    for state,indices,loop in [('idle',[0,1],True),('warn',[2],False),('attack',[3],False),('phase2',[4],True),('hurt',[5],False),('death',[6,7],False)]:anim(actor,state,'bosses',[names[i] for i in indices],5,loop)
    asset(actor,reference,'New silhouette detailing, armor/costume, attack, weak-point and defeat artwork','bosses')
for actor,row,cols in [('peroro',5,[0,1,2,3,4]),('pillar',5,[5,6,7,8]),('goz_clone',6,[0,1,2,3,4]),('chesed_drone',6,[5,6,7,8])]:
    names=[frame('bosses',f'{actor}_{i}',row,c) for i,c in enumerate(cols)]
    anim(actor,'idle','bosses',names[:2],5);anim(actor,'attack','bosses',[names[min(2,len(names)-1)]],7,False);anim(actor,'death','bosses',[names[-1]],7,False)
    asset(actor,'Blue Archive raid-associated support entity','New matching raid summon/clone/pillar/drone design','bosses')
for c in range(8):frame('bosses',f'raid_fx_{c}',7,c)
anim('effects','special','bosses',[f'raid_fx_{i}' for i in range(8)],8,False)

sheet('props',[0,162,308,447,597,738,887,971,1122],[8]*8,128)
prop_names=['supply_crate','fuel_drum','cover_barricade','machine_crate','rail_platform','coastal_platform','comms_unit','ruin_platform','facility_panel','street_barrier','reactor_unit','debris']
for i,name in enumerate(prop_names):
    for n in range(4):frame('props',f'prop_{i:02}_{n}',i//2,(i%2)*4+n)
    # Alias preserves the stage-data style assignments.
    frame('props',f'prop_{i:02}',i//2,(i%2)*4)
    for state,n in [('intact',0),('damaged',1),('breaking',2),('destroyed',3)]:anim(f'prop_{i:02}',state,'props',[f'prop_{i:02}_{n}'],8,False)
    asset(name,'Kivotos / Gehenna / Kaiser facilities and equipment','New detailed prop with pristine, damaged, breaking and debris states','props')
item_names=['score_pickup','special_pickup','friendly_bullet','enemy_bullet','robot_bullet','special_bullet','muzzle','hit']
for c,name in enumerate(item_names):
    frame('props',name,6,c)
    asset(name,'Blue Archive inspired ammunition, supply and energy design language','New matching pickup/projectile/effect art','props')
for c in range(8):frame('props',f'ex_{c}',7,c)
anim('effects','explosion','props',[f'ex_{i}' for i in range(8)],16,False)
asset('explosion','Military ordnance / violet energy palette','New eight-frame expanding fire-and-smoke animation','props')

sheet('backgrounds',[0,210,418,608,812,1024],[1]*5,64)
for i,name in enumerate(['Kivotos coastal landing','Abydos desert ruins','Millennium/Kaiser railway','Night Gehenna academy district','Decagrammaton reactor hall']):
    frame('backgrounds',f'stage_{i}',i,0)
    d,im,rows,_=definitions['backgrounds'];y0,y1=rows[i:i+2]
    explicit('backgrounds',f'ground_{i}',0,im.height-y1,im.width,32)
    asset(f'background_{i+1}',name,'New panoramic environment and terrain strip','backgrounds')

(OUT/'atlas_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2))
(ROOT/'DESIGN_SOURCES.json').write_text(json.dumps({'reference_method':'Designs based on familiar Blue Archive canonical character/faction/raid settings; newly generated imagery, no official asset extraction. External reference-page fetch was unavailable in this environment.','sprites':source_records},ensure_ascii=False,indent=2))
with (ROOT/'ASSET_REQUIREMENTS.csv').open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=requirements[0].keys());w.writeheader();w.writerows(requirements)
print(json.dumps({'new_atlases':len(manifest['atlases']),'animation_mappings':len(manifest['animations']),'new_design_groups_applied':len(requirements),'frames':sum(len(a['frames']) for a in manifest['atlases'])}))
