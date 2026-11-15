#!/usr/bin/env python3
"""Inspect individually present sparse pairs in globally invalid paired responses."""
import collections, json
from pathlib import Path
p=Path(__file__).resolve().parent.parent/'Fixtures'
r=json.loads((p/'thematic-chat-feasibility.json').read_text())
c=json.loads((p/'thematic-corpus.json').read_text())
qs={x['id']:x for x in c['queries'] if x['split']=='calibration'}
out={}
for x in r:
 if x['query_id'] not in {'q02','q03','q04','q05','q06'}:continue
 m=collections.defaultdict(list)
 for j in x['response']['judgments']:
  if j['id'] in x['id_map']:m[j['group'],x['id_map'][j['id']]].append(j['relevance'])
 q=qs[x['query_id']];out[x['query_id']]={}
 for book in ('philebus','antigone','disgrace'):
  a=m['plain',book];b=m['context',book]
  label='relevant' if book in q['relevant'] else 'borderline' if book in q['borderline'] else 'irrelevant'
  out[x['query_id']][book]={'plain':a,'context':b,'frozen':label,'comparable':len(a)==1 and len(b)==1}
(p/'thematic-chat-paired-partial.json').write_text(json.dumps(out,indent=2)+'\n')
