"""Package only final evidence and required native assets. Verify ZIP CRC and SHA256."""
from pathlib import Path
import hashlib,json,zipfile
from html.parser import HTMLParser
P=Path(__file__).resolve().parents[1]
for f in ['geometry_validation.json','fresh_reopen.json','module_append_validation.json']:
    assert json.loads((P/'Audit'/f).read_text())['pass'],f
assert json.loads((P/'Audit/geometry_validation.json').read_text())['removed_obsolete_object_count']==190
assert json.loads((P/'Audit/fresh_reopen.json').read_text())['scene_objects']==37318
assert json.loads((P/'Audit/revision_render_progress.json').read_text())['complete']
assert json.loads((P/'Audit/entrance_render_progress.json').read_text())['complete']
assert json.loads((P/'Audit/emblem_final_progress.json').read_text())['complete']
for n,digest in json.loads((P/'Audit/module_append_validation.json').read_text())['tested_library_sha256'].items():
    assert hashlib.sha256((P/'Exports'/n).read_bytes()).hexdigest()==digest
class Links(HTMLParser):
    def __init__(self):super().__init__();self.links=[]
    def handle_starttag(self,tag,attrs):
        for k,v in attrs:
            if k in ('src','href') and v and not v.startswith(('#','http')):self.links.append(v)
parser=Links();parser.feed((P/'Review.html').read_text())
assert all((P/n).exists() for n in parser.links),'Review links must resolve.'
def include(f):
    n=f.name
    if not f.is_file() or n.startswith('.') or '__pycache__' in f.parts:return False
    if n.endswith(('.blend1','.blend2','.log','.pyc')) or 'Draft' in n or n.startswith('draft_'):return False
    if 'progress' in n.lower() or n=='FILE_SHA256.json':return False
    return True
files=sorted(f for f in P.rglob('*') if include(f))
manifest={str(f.relative_to(P)):{'bytes':f.stat().st_size,'sha256':hashlib.sha256(f.read_bytes()).hexdigest()} for f in files}
(P/'FILE_SHA256.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2))
files.append(P/'FILE_SHA256.json')
zpath=P.parent/'MilanLibrary_Magic_V6_Delivery.zip'
with zipfile.ZipFile(zpath,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=4) as z:
    for f in files:z.write(f,str(Path(P.name)/f.relative_to(P)))
with zipfile.ZipFile(zpath) as z:assert z.testzip() is None
digest=hashlib.sha256(zpath.read_bytes()).hexdigest()
zpath.with_suffix('.zip.sha256').write_text(digest+'  '+zpath.name+'\n')
print(json.dumps({'zip':str(zpath),'bytes':zpath.stat().st_size,'files':len(files),'sha256':digest,'review_links_checked':len(parser.links)},ensure_ascii=False))
