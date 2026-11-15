#!/usr/bin/env python3
"""Calibration only: sweep thresholds from recorded scores plus endpoint keyword candidates."""
import base64, json, math, struct, sys
from pathlib import Path
root = Path(__file__).resolve().parent.parent / 'Fixtures'
c = json.loads((root / 'thematic-corpus.json').read_text())
r = json.loads((root / 'thematic-vectors.json').read_text())['Vectors']
keywords = json.loads(Path(sys.argv[1]).read_text())
import hashlib, re
def norm(x): return re.sub(r'\s+', ' ', (x or '').strip())
def booktext(b): return f"Title: {norm(b['title'])}\nAuthors: {', '.join(map(norm, b['authors']))}\nTags: \nPublisher: \nDescription: {norm(b['description'])}\nAnnotation: "
def vec(text):
    raw=base64.b64decode(r[hashlib.sha256(text.encode()).hexdigest().upper()]); return struct.unpack('<1536f',raw)
def score(a,b):
    dot=sum(x*y for x,y in zip(a,b)); return (1+dot/math.sqrt(sum(x*x for x in a)*sum(y*y for y in b)))/2
books={b['id']:vec(booktext(b)) for b in c['books']}
queries=[q for q in c['queries'] if q['split']=='calibration']
scores={q['id']:{i:score(vec(q['prompt']),b) for i,b in books.items()} for q in queries}
required={'q01':(['philebus','antigone'],['disgrace']),'q02':(['philebus','antigone'],['disgrace']),'q03':(['philebus','antigone'],['gilgamesh','disgrace']),'q04':(['philebus'],['gatsby']),'q05':(['disgrace'],['philebus','antigone']),'q06':(['philebus'],['antigone']),'q07':([],list(books)),'q08':([],list(books))}
def assess(floor, margin=None):
    tp=fp=fn=0; details={}; named=[]
    for q in queries:
        ss=scores[q['id']]; cutoff=floor if margin is None else max(floor,max(ss.values())-margin)
        hits={i for i,s in ss.items() if s>=cutoff} | set(keywords[q['id']])
        relevant=set(q['relevant']); irrelevant=set(q['irrelevant']); t=len(hits&relevant); f=len(hits&irrelevant); m=len(relevant-hits); tp+=t; fp+=f; fn+=m
        details[q['id']]={'missed':sorted(relevant-hits),'irrelevant':sorted(hits&irrelevant)}
        if q['id'] in required:
            yes,no=required[q['id']]
            if not set(yes)<=hits or set(no)&hits: named.append(q['id'])
    f1=2*tp/(2*tp+fp+fn) if 2*tp+fp+fn else 0
    return {'floor':floor,'margin':margin,'tp':tp,'fp':fp,'fn':fn,'f1':f1,'named_failures':named,'details':details}
# Midpoints avoid rounding at an actual score boundary.
all_scores=sorted({s for v in scores.values() for s in v.values()})
breaks=[0.0]+[(a+b)/2 for a,b in zip(all_scores, all_scores[1:])]+[1.0]
results=[assess(x) for x in breaks]
passing=[x for x in results if not x['named_failures']]
output={'global':results}
if passing:
    best=max(passing,key=lambda x:(x['f1'],x['tp'],-x['fp'],x['floor']))
    output['selected_global']=best
else:
    # A bounded relative policy is measured on the same calibration pairs only.
    candidates=[assess(floor, margin/1000) for floor in [0.60+i/1000 for i in range(151)] for margin in range(0,151,5)]
    output['relative']=candidates
    passing=[x for x in candidates if not x['named_failures']]
    output['selected_relative']=max(passing,key=lambda x:(x['f1'],x['tp'],-x['fp'],x['floor'],-x['margin'])) if passing else None
Path(sys.argv[2]).write_text(json.dumps(output,indent=2))
chosen=output.get('selected_global') or output.get('selected_relative')
print('global candidates',len(results),'passing',len([x for x in results if not x['named_failures']]))
print('selected', {k:v for k,v in chosen.items() if k!='details'} if chosen else 'none')
