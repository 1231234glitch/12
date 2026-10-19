"""Apply reference-faithful anime art and create Unity slicing metadata.

Reference images are used only as drawing guides. Delivered PNGs are newly
illustrated. No raster resizing/recoloring/background removal occurs here.
"""
from pathlib import Path
from PIL import Image
import argparse,csv,hashlib,json,shutil
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'ProjectHINA/Assets/Hina/Resources/Hina'
p=argparse.ArgumentParser();p.add_argument('--generated-dir',type=Path);args=p.parse_args()
FILES={'player':'exec-871cb039-41d5-4ea5-8500-a2976d18f4da.png','enemies':'exec-635390f5-3302-49d2-a1b9-29a49c5659d5.png','vehicles':'exec-c41e3979-4c17-4c63-b66f-cc4c6d9eeb1f.png','bosses':'exec-8b1c0a7f-c4a4-4561-be89-a5aa0d420a6d.png','support':'exec-828b9e1f-3b7d-469f-ad9d-919189addeea.png','props':'exec-4d8a7374-0b4d-484c-859a-94aeea8d6b94.png','effects':'exec-ddd6ceae-b16d-4535-871e-8491617388ec.png','backgrounds':'exec-c8b34e2a-01bf-4e67-8db5-e3f7da6881dc.png'}
(OUT/'Sheets').mkdir(parents=True,exist_ok=True)
if args.generated_dir:
 for name,source in FILES.items():shutil.copy2(args.generated_dir/source,OUT/'Sheets'/(name+'.png'))
for f in (OUT/'Sheets').glob('*.png'):
 if f.stem not in FILES:f.unlink()
m={'design_assets_complete':True,'official_extracted_runtime_assets':False,'reference_target':'Faithful default Blue Archive illustrations and enemy models, not alternative costumes','style_constraint':'Smooth anime linework and cel shading; no pixel-art requirement','atlases':[],'animations':[]}
records=[];requirements=[];definitions={}
REF='https://raw.githubusercontent.com/lonqie/SchaleDB/main/images/'

def sheet(id,rough,ppu,columns=4,reference_files=()):
 im=Image.open(OUT/'Sheets'/(id+'.png')).convert('RGBA');w,h=im.size;assert rough[0]==0 and rough[-1]==h,(id,im.size)
 d={'id':id,'cellWidth':0,'cellHeight':0,'columns':columns,'pixelsPerUnit':ppu,'smoothFiltering':True,'frames':[],'frameRects':[],'provenance':'canonical_reference_illustration'}
 bounds=[]
 for col in range(columns):
  x0,x1=round(w*col/columns),round(w*(col+1)/columns);edges=[0]
  for boundary in rough[1:-1]:
   if id=='backgrounds':edges.append(boundary);continue
   radius=min(35,max(10,round(h/(len(rough)-1)*.12)))
   candidates=range(max(edges[-1]+1,boundary-radius),min(h,boundary+radius+1))
   alpha=im.getchannel('A')
   def cost(y):return (sum(alpha.crop((x0,y,x1,y+1)).histogram()[160:]),abs(y-boundary))
   edges.append(min(candidates,key=cost))
  edges.append(h);bounds.append(edges)
 definitions[id]=(d,im,bounds);m['atlases'].append(d)
 records.append({'atlas':id,'file':'Sheets/'+id+'.png','width':w,'height':h,'sha256':hashlib.file_digest((OUT/'Sheets'/(id+'.png')).open('rb'),'sha256').hexdigest(),'creation':'Newly illustrated using actual source-image references; default identity, costume, weapon and palette prioritized','reference_urls':[REF+f for f in reference_files],'modified_pixels':False})

def frame(atlas,name,row,col,pivotX=.5):
 d,im,bounds=definitions[atlas];columns=d['columns'];x0,x1=round(im.width*col/columns),round(im.width*(col+1)/columns);y0,y1=bounds[col][row:row+2]
 bbox=im.getchannel('A').crop((x0,y0,x1,y1)).point(lambda a:255 if a>160 else 0).getbbox();py=(y1-y0-bbox[3]+1)/(y1-y0) if bbox else 0
 d['frames'].append(name);d['frameRects'].append(dict(x=x0,y=im.height-y1,width=x1-x0,height=y1-y0,pivotX=pivotX,pivotY=max(0,min(1,py))));return name

def alias(atlas,name,old):
 d=definitions[atlas][0];i=d['frames'].index(old);d['frames'].append(name);d['frameRects'].append(dict(d['frameRects'][i]));return name

def anim(actor,state,atlas,frames,fps=8,loop=True):m['animations'].append(dict(actor=actor,state=state,atlas=atlas,frames=frames,fps=fps,loop=loop))
def asset(id,reference,detail,atlas):requirements.append(dict(id=id,reference_setting=reference,reference_fidelity=detail,atlas=atlas,status='CANONICAL_REFERENCE_APPLIED',original_files_required='NO'))

sheet('player',[0,284,544,790,1024],256,reference_files=['student/portrait/10004.webp','weapon/weapon_icon_10004.webp'])
for r in range(4):
 for c in range(4):frame('player',f'hina_{r}_{c}',r,c,.58)
def pf(r,cols):return [f'hina_{r}_{c}' for c in cols]
for state,fs,fps,loop in [('idle',pf(0,[0]),5,True),('walk',pf(0,[1,2,3,2]),11,True),('walk_shoot',pf(0,[1,2,3,2]),11,True),('shoot',pf(2,[0]),12,True),('jump_rise',pf(1,[0]),8,False),('jump_apex',pf(1,[1]),8,True),('fall',pf(1,[2]),8,True),('air_shoot',pf(1,[1,2]),8,True),('air_down',pf(1,[3]),8,True),('shoot_up',pf(2,[1]),8,True),('shoot_diagonal',pf(2,[2]),8,True),('crouch',pf(2,[3]),8,True),('crouch_shoot',pf(2,[3]),8,True),('kick',pf(3,[0]),8,False),('air_kick',pf(3,[0]),8,False),('special',pf(3,[1,1,2,2]),2.4,False),('hurt',pf(3,[3]),8,False),('death',pf(3,[3]),8,False),('land',pf(0,[0]),8,False)]:anim('hina',state,'player',fs,fps,loop)
asset('hina','Sorasaki Hina / default illustration and Destroyer of Denouement','Dark uniform, white draped coat, gold chain/buttons, red armband, canonical halo/horns/wings and black/violet MG','player')

human_refs=['enemy/enemyinfo_soldier_helmet_ar.webp','enemy/enemyinfo_schoolgirl_sukeban_smg.webp','enemy/enemyinfo_schoolgirl_sukeban_sr.webp','enemy/enemyinfo_soldier_kaiserpmc_sg.webp']
sheet('enemies',[0,292,576,853,1122],256,reference_files=human_refs)
for r,(actor,identity) in enumerate([('helmet','Helmet Gang: dark skull helmet/red visor'),('sukeban','Sukeban SMG: black ponytail/navy sailor uniform'),('sniper','Sukeban sniper: long black hair/navy long sailor uniform'),('shield','Kaiser PMC human shotgun soldier: tan camouflage')]):
 fs=[frame('enemies',f'{actor}_{c}',r,c,.60) for c in range(4)]
 for state,idx,loop in [('idle',[0],True),('walk',[0,1],True),('attack',[2],False),('hurt',[3],False),('death',[3],False)]:anim(actor,state,'enemies',[fs[i] for i in idx],6,loop)
 asset(actor,identity,'Source-image silhouette, clothing, hair, halo, weapon and palette closely retained','enemies')
vehicle_refs=['enemy/enemyinfo_droid_kaiserpmc_ar.webp','enemy/enemyinfo_heavytank_kaiserpmc_cannon.webp','enemy/enemyinfo_heavytank_kaiserpmc_railgun.webp']
sheet('vehicles',[0,288,527,770,1024],256,reference_files=vehicle_refs)
for r,(actor,identity) in enumerate([('military','Kaiser PMC droid: green triangle optic/tan camouflage'),('tank','Kaiser heavy cannon tank: tan camouflage/green optics'),('railgun','Kaiser heavy railgun tank: long green railgun/tan camouflage')]):
 fs=[frame('vehicles',f'{actor}_{c}',r,c,.55) for c in range(4)]
 for state,idx,loop in [('idle',[0],True),('walk',[0,1],True),('attack',[2],False),('hurt',[2],False),('death',[3],False)]:anim(actor,state,'vehicles',[fs[i] for i in idx],6,loop)
 asset(actor,identity,'Original camouflage geometry, green systems and characteristic cannon/railgun shapes','vehicles')

boss_names=['Binah','Perorozilla','HOD','Goz','Chesed']
sheet('bosses',[0,217,434,631,831,1024],96,reference_files=['raid/Boss_Portrait_'+n+'_Lobby.png' for n in boss_names])
for r,(actor,detail) in enumerate([('binah','White segmented mechanical serpent, orange seams/eyes, gold halo'),('perorodzilla','White Peroro mascot, circular eye, yellow beak, long pink tongue, pale cyan dorsal motif'),('hod','White/gray angular machine, black structure, orange core and heavy articulated limbs'),('goz','Gray magician cat, green eyes, toothy grin, spade top hat, black bow tie, white fur collar'),('chesed','Dark armored sphere, white circular front, orange eye/rings and flanking panels')]):
 fs=[frame('bosses',f'{actor}_{c}',r,c) for c in range(4)]
 for state,idx,loop in [('idle',[0],True),('warn',[1],False),('attack',[1],False),('phase2',[2],True),('hurt',[2],False),('death',[3],False)]:anim(actor,state,'bosses',[fs[i] for i in idx],5,loop)
 asset(actor,'Blue Archive '+boss_names[r],detail,'bosses')

sheet('support',[0,285,562,841,1122],256,reference_files=['raid/Boss_Portrait_Perorozilla_Lobby.png','enemy/enemyinfo_hod_colour_temporarytower.webp','raid/Boss_Portrait_Goz_Lobby.png','enemy/enemyinfo_droid_decagram_ar.webp'])
for r,(actor,identity) in enumerate([('peroro','Small Peroro mascot raid add'),('pillar','HOD color-variant temporary tower, purple segmented cylinder/white eye band'),('goz_clone','Canonical Goz costume with blue illusion outline'),('chesed_drone','Decagrammaton white/orange infantry droid')]):
 fs=[frame('support',f'{actor}_{c}',r,c,.6 if actor=='chesed_drone' else .5) for c in range(4)]
 for state,idx in [('idle',0),('attack',1),('hurt',2),('death',3)]:anim(actor,state,'support',[fs[idx]],6,state=='idle')
 asset(actor,identity,'Reference geometry and palette; newly illustrated idle/attack/hit/defeat states','support')

sheet('props',[0,211,405,587,788,984,1188,1331,1536],192,reference_files=['background/BG_FishingVillage2_Collection.jpg','background/BG_Abydos_Collection.jpg','background/BG_Millennium_Collection.jpg','enemy/enemyinfo_droid_kaiserpmc_ar.webp'])
prop_names=['ammo_crate','fuel_drum','concrete_barricade','machine_box','rail_platform','harbor_platform','radio_case','school_foundation','industrial_wall','road_barrier','reactor_control','debris']
for i,name in enumerate(prop_names):
 intact=frame('props',f'prop_{i:02}_0',i//2,(i%2)*2);broken=frame('props',f'prop_{i:02}_1',i//2,(i%2)*2+1);alias('props',f'prop_{i:02}',intact)
 for state,key in [('intact',intact),('damaged',intact),('breaking',intact),('destroyed',broken)]:anim(f'prop_{i:02}',state,'props',[key],6,False)
 asset(name,'Kivotos/Kaiser grounded equipment and reference environment materials','Practical colors and utilitarian geometry; no invented ornate fantasy props','props')

sheet('effects',[0,318,532,810,1086],256,reference_files=['weapon/weapon_icon_10004.webp','raid/Boss_Portrait_Chesed_Lobby.png','enemy/enemyinfo_droid_kaiserpmc_ar.webp'])
names=[['muzzle_0','muzzle_1','hit','special_fx'],['friendly_bullet','robot_bullet','enemy_bullet','missile'],['ex_0','ex_1','ex_2','ex_3'],['score_pickup','special_pickup','blue_hit','raid_blast']]
for r,row in enumerate(names):
 for c,name in enumerate(row):frame('effects',name,r,c)
alias('effects','muzzle','muzzle_0');alias('effects','special_bullet','friendly_bullet')
anim('effects','explosion','effects',[f'ex_{i}' for i in range(4)],10,False);anim('effects','special','effects',['muzzle_1','special_fx','special_fx','hit'],4,False)
for name in ['score_pickup','special_pickup','friendly_bullet','enemy_bullet','robot_bullet','special_bullet','muzzle','hit']:asset(name,'Hina violet weapon, Kaiser green electronics, Decagrammaton orange core and credit/ammo motifs','Reference-matched effect color and supply-case style','effects')
asset('explosion','Blue Archive-like smooth battle VFX','Illustrated flash/fire/smoke progression','effects')

sheet('backgrounds',[0,164,327,483,639,768],64,columns=1,reference_files=['background/BG_FishingVillage2_Collection.jpg','background/BG_Abydos_Collection.jpg','background/BG_Millennium_Collection.jpg','background/BG_Gehenna_Collection.jpg'])
for i,identity in enumerate(['Kivotos fishing harbor/coastal village','Abydos modern school and sand','Millennium industrial railway service facility','Gehenna night academy plaza','Decagrammaton white/gray/orange industrial reactor']):
 frame('backgrounds',f'stage_{i}',i,0);d,im,b=definitions['backgrounds'];y0,y1=b[0][i:i+2];d['frames'].append(f'ground_{i}');d['frameRects'].append(dict(x=0,y=im.height-y1,width=im.width,height=14,pivotX=.5,pivotY=0))
 asset(f'background_{i+1}',identity,'Reference-style architecture, practical materials and original-like atmosphere','backgrounds')

(OUT/'atlas_manifest.json').write_text(json.dumps(m,ensure_ascii=False,indent=2));(ROOT/'DESIGN_SOURCES.json').write_text(json.dumps({'reference_method':'Actual Blue Archive source images and weapon/enemy/boss/background information were retrieved from the public SchaleDB repository and visually inspected. Newly illustrated game poses prioritize original design fidelity. Reference rasters are not included as runtime assets.','reference_repository':'https://github.com/lonqie/SchaleDB','reference_repository_head_at_inspection':'70a2c4b8982ca860687898e61848847a60ffe3b8','sprites':records},ensure_ascii=False,indent=2))
with (ROOT/'ASSET_REQUIREMENTS.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=requirements[0].keys());w.writeheader();w.writerows(requirements)
print(json.dumps({'atlases':len(m['atlases']),'animation_mappings':len(m['animations']),'reference_applied_groups':len(requirements),'frames':sum(len(a['frames']) for a in m['atlases'])}))
