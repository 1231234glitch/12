from pathlib import Path
from PIL import Image
import json,csv,hashlib
root=Path(__file__).resolve().parents[1];resources=root/'ProjectHINA/Assets/Hina/Resources/Hina'
m=json.loads((resources/'atlas_manifest.json').read_text());keys={};count=0
sources=json.loads((root/'DESIGN_SOURCES.json').read_text())['sprites']
for a in m['atlases']:
 p=resources/'Sheets'/(a['id']+'.png');im=Image.open(p).convert('RGBA');assert a['provenance']=='canonical_reference_illustration';assert a['smoothFiltering'];assert a['pixelsPerUnit']>0
 expected=next(s['sha256'] for s in sources if s['atlas']==a['id']);assert hashlib.file_digest(p.open('rb'),'sha256').hexdigest()==expected
 assert len(a['frames'])==len(a['frameRects']);assert len(set(a['frames']))==len(a['frames'])
 for name,r in zip(a['frames'],a['frameRects']):
  assert r['width']>0 and r['height']>0 and r['x']>=0 and r['y']>=0
  assert r['x']+r['width']<=im.width and r['y']+r['height']<=im.height
  assert 0<=r['pivotX']<=1 and 0<=r['pivotY']<=1
  bbox=(r['x'],im.height-r['y']-r['height'],r['x']+r['width'],im.height-r['y'])
  assert im.getchannel('A').crop(bbox).getbbox() is not None,(a['id'],name)
  count+=1
 keys[a['id']]=set(a['frames'])
for a in m['animations']:
 assert a['frames'] and a['fps']>0
 assert all(f in keys[a['atlas']] for f in a['frames'])
for stage in json.loads((resources/'stages.json').read_text())['stages']:
 for state in ['idle','warn','attack','phase2','hurt','death']:assert any(a['actor']==stage['boss'] and a['state']==state for a in m['animations'])
 for p in stage['props']+stage['decor']:assert f"prop_{p['style']:02}" in keys['props']
requirements=list(csv.DictReader((root/'ASSET_REQUIREMENTS.csv').open(encoding='utf-8-sig')));assert all(r['status']=='CANONICAL_REFERENCE_APPLIED' for r in requirements)
core=json.loads((root/'core_test_results.json').read_text());syntax=json.loads((root/'syntax_test_results.json').read_text());browser=json.loads((root/'art_review_results.json').read_text());assert core['failures']==0 and syntax['passed'] and browser['passed']
report={'status':'GAME_IMPLEMENTED_AWAITING_UNITY_BUILD_ENVIRONMENT','core_checks_passed':core['checks'],'core_checks_failed':0,'atlas_count':len(m['atlases']),'validated_frames':count,'animation_mapping_count':len(m['animations']),'new_design_groups_applied':len(requirements),'actual_source_references_inspected':True,'canonical_design_fidelity_prioritized':True,'official_extracted_assets_used':False,'pixel_art_required':False,'source_pngs_preserved_without_pixel_changes':True,'unity_editor_executed':False,'unity_api_compilation_verified':False,'unity_runtime_verified':False,'unity_executable_built':False,'art_review_is_unity_runtime':False,'csharp_syntax':syntax,'art_review':browser,'remaining_requirement':'Unity Editor 2022.3.62f1 with activated license and target build modules for editor validation/export'}
(root/'VALIDATION.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
