#!/usr/bin/env python3
"""Calibration-only vector admission sweep; ranking and keyword effects require endpoint replay."""
import base64, gzip, hashlib, json, math, re, struct
from pathlib import Path
P=Path(__file__).resolve().parent.parent/'Fixtures'
c=json.loads((P/'thematic-corpus.json').read_text()); spec=json.loads((P/'thematic-variant-spec.json').read_text())
old=json.loads((P/'thematic-vectors.json').read_text())['Vectors']; new=json.loads((P/'thematic-variant-vectors.json').read_text())['vectors']; vectors={**old,**new}
books={b['id']:b for b in c['books']}; qs=[q for q in c['queries'] if q['split']=='calibration']
n=lambda s:re.sub(r'\s+',' ',(s or '').strip())
def booktext(b,context):
 d=(b['description'] or '')+'\nBibliographic context: '+spec['contexts'][b['id']] if context else b['description']
 return f"Title: {n(b['title'])}\nAuthors: {', '.join(map(n,b['authors']))}\nTags: \nPublisher: \nDescription: {n(d)}\nAnnotation: "
def vec(s):
 raw=base64.b64decode(vectors[hashlib.sha256(s.encode()).hexdigest().upper()]); assert len(raw)==6144; return struct.unpack('<1536f',raw)
def cosine(a,b):return (1+sum(x*y for x,y in zip(a,b))/math.sqrt(sum(x*x for x in a)*sum(y*y for y in b)))/2
# Narrow user-raised adjacency examples; frozen labels remain unchanged. No-match requirements still apply.
adjacent={'q02':{'gilgamesh'}, 'q03':{'gilgamesh','aeneid','metamorphoses','meditations'}, 'q06':{'antigone'}, 'q07':{'cosmos'}, 'q08':{'gilgamesh','metamorphoses'}}
required={'q01':(['philebus','antigone'],['disgrace']),'q02':(['philebus','antigone'],['disgrace']),'q03':(['philebus','antigone'],['gilgamesh','disgrace']),'q04':(['philebus'],['gatsby']),'q05':(['disgrace'],['philebus','antigone']),'q06':(['philebus'],['antigone']),'q07':([],list(books)),'q08':([],list(books))}
originalkeywords=json.loads((P/'thematic-keyword-results.json').read_text());contextkeywords=json.loads((P/'thematic-variant-context-keywords.json').read_text())
all_output={}
for context in (False,True):
 bvec={i:vec(booktext(b,context)) for i,b in books.items()}; keywords=contextkeywords if context else originalkeywords
 for querymode in ('original',*spec['query_templates'],'hybrid_original_matching','hybrid_both_templates'):
  name=('context' if context else 'original')+'__'+querymode
  def query_vectors(q):
   if querymode=='original': return [vec(q['prompt'])]
   if querymode=='hybrid_original_matching': return [vec(q['prompt']),vec(spec['query_templates']['matching_book'].replace('{query}',q['prompt']))]
   if querymode=='hybrid_both_templates': return [vec(t.replace('{query}',q['prompt'])) for t in spec['query_templates'].values()]
   return [vec(spec['query_templates'][querymode].replace('{query}',q['prompt']))]
  scores={}
  for q in qs:
   embeddings=query_vectors(q);scores[q['id']]={i:sum(cosine(a,b) for a in embeddings)/len(embeddings) for i,b in bvec.items()}
  def assess(cutoff):
   groups={}; details={}; failures=[]
   for q in qs:
    id=q['id'];hits={i for i,s in scores[id].items() if s>=cutoff}|set(keywords[id]);rel=set(q['relevant']);irr=set(q['irrelevant']);tp=hits&rel;fp=hits&irr;near=fp&adjacent.get(id,set())
    details[id]={'missed':sorted(rel-hits),'irrelevant':sorted(fp),'adjacent':sorted(near),'other_irrelevant':sorted(fp-near),'borderline_returned':sorted(hits&set(q['borderline'])),'no_match_ok': not hits if not rel and not q['borderline'] else None}
    if id in required:
     yes,no=required[id]
     if not set(yes)<=hits or set(no)&hits:failures.append(id)
    for group,eligible in [('all',set(books)),('language:'+q['language'],set(books)),('category:'+q['category'],set(books)),('metadata:sparse',{i for i,b in books.items() if b['metadata']=='sparse'}),('metadata:enriched',{i for i,b in books.items() if b['metadata']=='enriched'})]:
     vals=groups.setdefault(group,[0,0,0,0]); vals[0]+=len(tp&eligible);vals[1]+=len((tp|fp)&eligible);vals[2]+=len(rel&eligible);vals[3]+=len(near&eligible)
   tp,returned,expected,_=groups['all']; f1=2*tp/(returned+expected) if returned+expected else 0
   return {'cutoff':cutoff,'groups_tp_returned_expected_adjacent':groups,'named_failures':failures,'f1':f1,'details':details}
  ss=sorted({x for m in scores.values() for x in m.values()});breakpoints=[0]+[(x+y)/2 for x,y in zip(ss,ss[1:])]+[1]; assessed=[assess(x) for x in breakpoints];passing=[x for x in assessed if not x['named_failures']]
  best=(max(passing,key=lambda x:(x['f1'],x['groups_tp_returned_expected_adjacent']['all'][0],x['cutoff'])) if passing else min(assessed,key=lambda x:(len(x['named_failures']),-x['f1'],-x['cutoff'])))
  ranks={q['id']:sorted(scores[q['id']],key=scores[q['id']].get,reverse=True) for q in qs}
  coverage={str(k):[sum(len(set(q['relevant'])&set(ranks[q['id']][:k])) for q in qs),sum(len(q['relevant']) for q in qs)] for k in (5,10,20,36)}
  all_output[name]={'passes_named_admission':bool(passing),'number_breakpoints':len(breakpoints),'number_passing':len(passing),'best_calibration':best,'default':assess(0.7285),'tolerant_probe':assess(0.6585),'coverage':coverage,'scores':scores,'sweep':assessed}
  print(name,'passing',len(passing),'best cutoff',round(best['cutoff'],6),'named_fail',best['named_failures'],'counts',best['groups_tp_returned_expected_adjacent']['all'],'coverage',coverage)
(P/'thematic-variant-summary.json').write_text(json.dumps({k:{a:b for a,b in v.items() if a not in ('scores','sweep')} for k,v in all_output.items()},indent=2)+'\n')
with gzip.open(P/'thematic-variant-analysis.json.gz','wt') as output: json.dump(all_output,output,indent=2)
