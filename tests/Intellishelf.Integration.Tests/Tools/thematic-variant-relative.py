#!/usr/bin/env python3
"""Offline bounded relative admission sweep on calibration scores; no ranking claim."""
import csv,gzip,json
from pathlib import Path
P=Path(__file__).resolve().parent.parent/'Fixtures';v=json.load(gzip.open(P/'thematic-variant-analysis.json.gz','rt'));c=json.loads((P/'thematic-corpus.json').read_text());qs=[q for q in c['queries'] if q['split']=='calibration'];ids={b['id'] for b in c['books']}
orig=json.loads((P/'thematic-keyword-results.json').read_text());context=json.loads((P/'thematic-variant-context-keywords.json').read_text())
required={'q01':(['philebus','antigone'],['disgrace']),'q02':(['philebus','antigone'],['disgrace']),'q03':(['philebus','antigone'],['gilgamesh','disgrace']),'q04':(['philebus'],['gatsby']),'q05':(['disgrace'],['philebus','antigone']),'q06':(['philebus'],['antigone']),'q07':([],list(ids)),'q08':([],list(ids))}
summary={}
with gzip.open(P/'thematic-variant-relative.csv.gz','wt',newline='') as f:
 w=csv.writer(f);w.writerow(['variant','floor','margin','true_positive','clearly_returned','expected','named_failures'])
 for variant,data in v.items():
  scores=data['scores'];keywords=context if variant.startswith('context__') else orig;best=None;passes=0
  for i in range(151):
   floor=0.6+i/1000
   for mi in range(0,151,5):
    margin=mi/1000;tp=ret=expected=0;fails=[]
    for q in qs:
     id=q['id'];d=scores[id]; threshold=max(floor,max(d.values())-margin);hit={k for k,s in d.items() if s>=threshold}|set(keywords[id]);r=set(q['relevant']);z=set(q['irrelevant']);tp+=len(hit&r);ret+=len(hit&(r|z));expected+=len(r)
     if id in required:
      yes,no=required[id]
      if not set(yes)<=hit or set(no)&hit:fails.append(id)
    f1=2*tp/(ret+expected) if ret+expected else 0;row={'floor':floor,'margin':margin,'tp':tp,'returned':ret,'expected':expected,'named_failures':fails,'f1':f1}
    passes+=not fails;w.writerow([variant,floor,margin,tp,ret,expected,'|'.join(fails)])
    if best is None or (len(fails),-f1,-tp,-floor,margin)<(len(best['named_failures']),-best['f1'],-best['tp'],-best['floor'],best['margin']):best=row
  summary[variant]={'passing':passes,'best':best};print(variant,'passing',passes,'best',best)
adjacent={'q02':{'gilgamesh'},'q03':{'gilgamesh','aeneid','metamorphoses','meditations'},'q06':{'antigone'},'q07':{'cosmos'},'q08':{'gilgamesh','metamorphoses'}}
books={b['id']:b for b in c['books']}
for variant,entry in summary.items():
 best=entry['best'];keywords=context if variant.startswith('context__') else orig;groups={};details={}
 for q in qs:
  id=q['id'];scores=v[variant]['scores'][id];threshold=max(best['floor'],max(scores.values())-best['margin']);hits={i for i,s in scores.items() if s>=threshold}|set(keywords[id]);rel=set(q['relevant']);irr=set(q['irrelevant']);tp=hits&rel;fp=hits&irr;near=fp&adjacent.get(id,set())
  details[id]={'missed':sorted(rel-hits),'irrelevant':sorted(fp),'adjacent':sorted(near),'other_irrelevant':sorted(fp-near),'borderline_returned':sorted(hits&set(q['borderline'])),'no_match_ok':not hits if not rel and not q['borderline'] else None}
  for group,eligible in [('all',ids),('language:'+q['language'],ids),('category:'+q['category'],ids),('metadata:sparse',{i for i,b in books.items() if b['metadata']=='sparse'}),('metadata:enriched',{i for i,b in books.items() if b['metadata']=='enriched'})]:
   a=groups.setdefault(group,[0,0,0,0]);a[0]+=len(tp&eligible);a[1]+=len((tp|fp)&eligible);a[2]+=len(rel&eligible);a[3]+=len(near&eligible)
 entry['groups_tp_returned_expected_adjacent']=groups;entry['details']=details
(P/'thematic-variant-relative-summary.json').write_text(json.dumps(summary,indent=2)+'\n')
