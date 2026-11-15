#!/usr/bin/env python3
"""Allow only Gilgamesh as an explicitly adjacent exception to the Greek-only named exclusion."""
import csv,gzip,json
from pathlib import Path
p=Path(__file__).resolve().parent.parent/'Fixtures'
d=json.load(gzip.open(p/'thematic-variant-analysis.json.gz','rt'));out={}
for name,v in d.items():
 passed=[]
 for row in v['sweep']:
  fails=row['named_failures'];q=row['details']['q03']
  tolerated='q03' in fails and 'philebus' not in q['missed'] and 'antigone' not in q['missed'] and 'disgrace' not in q['irrelevant']
  if not [i for i in fails if not (i=='q03' and tolerated)]:passed.append(row)
 best=max(passed,key=lambda x:(x['f1'],x['cutoff'])) if passed else None
 out[name]={'number_passing':len(passed),'best':best}
 print(name,'tolerant passing',len(passed))
original=json.loads((p/'thematic-keyword-results.json').read_text());context=json.loads((p/'thematic-variant-context-keywords.json').read_text())
for name in out:out[name]['relative_number_passing']=0
with gzip.open(p/'thematic-variant-relative.csv.gz','rt') as f:
 for row in csv.DictReader(f):
  name=row['variant'];fails=row['named_failures'].split('|') if row['named_failures'] else []
  if 'q03' in fails:
   scores=d[name]['scores']['q03'];threshold=max(float(row['floor']),max(scores.values())-float(row['margin']));keywords=(context if name.startswith('context__') else original)['q03'];hits={i for i,s in scores.items() if s>=threshold}|set(keywords)
   if {'philebus','antigone'}<=hits and 'disgrace' not in hits:fails.remove('q03')
  if not fails:out[name]['relative_number_passing']+=1
for name,v in out.items():print(name,'relative tolerant passing',v['relative_number_passing'])
(p/'thematic-variant-tolerance.json').write_text(json.dumps(out,indent=2)+'\n')
