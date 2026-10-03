import sys,runpy
from pathlib import Path
P=Path(__file__).resolve().parents[1]
runpy.run_path(str(P/'Scripts/05_validate.py'))
sys.argv=['render_job.py','--',str(P/'Audit/revision_render_job.json')]
runpy.run_path(str(P/'Scripts/render_job.py'))
runpy.run_path(str(P/'Scripts/07_export_verify.py'))
