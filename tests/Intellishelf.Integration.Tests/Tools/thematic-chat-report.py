#!/usr/bin/env python3
"""Compare calibration judgments with frozen labels; never reads held-out judgments."""
import base64, hashlib, json, math, re, struct
from pathlib import Path
P=Path(__file__).resolve().parent.parent/'Fixtures'
c=json.loads((P/'thematic-corpus.json').read_text())
qs={q['id']:q for q in c['queries'] if q['split']=='calibration'}
books={b['id']:b for b in c['books']}
records=json.loads((P/'thematic-chat-feasibility.json').read_text())
assert all(r['query_id'] in qs for r in records)
invalid=[r['query_id'] for r in records if not r['valid']]
records=[r for r in records if r['valid']]
qs={k:v for k,v in qs.items() if k in {r['query_id'] for r in records}}
assert len({r['query_id'] for r in records})==len(records) and qs
base=json.loads((P/'thematic-baseline-results.json').read_text()); cut=json.loads((P/'thematic-overlap-results.json').read_text())
vectors=json.loads((P/'thematic-vectors.json').read_text())['Vectors']
def norm(s):return re.sub(r'\s+',' ',(s or '').strip())
def booktext(b):return f"Title: {norm(b['title'])}\nAuthors: {', '.join(map(norm,b['authors']))}\nTags: \nPublisher: \nDescription: {norm(b['description'])}\nAnnotation: "
def vec(s):return struct.unpack('<1536f',base64.b64decode(vectors[hashlib.sha256(s.encode()).hexdigest().upper()]))
def score(a,b):return (1+sum(x*y for x,y in zip(a,b))/math.sqrt(sum(x*x for x in a)*sum(x*x for x in b)))/2
bv={i:vec(booktext(b)) for i,b in books.items()}
ranks={}
for id,q in qs.items():
 a=vec(q['prompt']); ranks[id]=sorted(books,key=lambda i:-score(a,bv[i]))
plain={}; context={}
for r in records:
 groups={}
 for j in r['response']['judgments']:
  group=j['group']; work=r['id_map'][j['id']]; groups.setdefault(group,{})[work]=j['relevance']
 plain[r['query_id']]=groups['plain']
 if 'context' in groups:context[r['query_id']]=groups['context']
methods={'baseline':base,'cutoff_0.6585':cut,'chat_relevant':{q:[i for i,v in js.items() if v=='relevant'] for q,js in plain.items()},'chat_relevant_or_borderline':{q:[i for i,v in js.items() if v!='irrelevant'] for q,js in plain.items()}}
for k in (10,20):
 methods[f'chat_relevant_top{k}']={q:[i for i,v in js.items() if v=='relevant' and i in ranks[q][:k]] for q,js in plain.items()}
def evaluate(method):
 totals=[0,0,0]; groups={}; details={}
 for id,q in qs.items():
  hits=set(method[id]); rel=set(q['relevant']); irr=set(q['irrelevant']);tp=hits&rel;fp=hits&irr;miss=rel-hits
  details[id]={'missed':sorted(miss),'irrelevant':sorted(fp),'borderline_returned':sorted(hits & set(q['borderline'])), 'no_match_ok': not hits if not rel and not q['borderline'] else None}
  for group,eligible in [('all',set(books)),('language:'+q['language'],set(books)),('category:'+q['category'],set(books)),('sparse',{i for i,b in books.items() if b['metadata']=='sparse'}),('enriched',{i for i,b in books.items() if b['metadata']=='enriched'})]:
   a=groups.setdefault(group,[0,0,0]); a[0]+=len(tp&eligible);a[1]+=len((tp|fp)&eligible);a[2]+=len(rel&eligible)
 return {'counts':groups,'details':details}
out={'valid_queries':sorted(qs), 'invalid_queries':invalid, 'attempts':json.loads((P/'thematic-chat-attempts.json').read_text()), 'methods':{m:evaluate(v) for m,v in methods.items()}}
# The user explicitly raised a tolerance for this adjacent ancient non-Greek work; labels stay unchanged.
out['adjacent_tolerance']={m: {'q03_gilgamesh_returned':'gilgamesh' in hits['q03'], 'strict_label':'irrelevant', 'interpretation':'ancient Mesopotamian, adjacent antiquity but not Greek'} for m,hits in methods.items()} if 'q03' in qs else {}
out['candidate_coverage']={}
for k in (5,10,20,36):
 n=sum(len(set(q['relevant']) & set(ranks[id][:k])) for id,q in qs.items());d=sum(len(q['relevant']) for q in qs.values());out['candidate_coverage'][str(k)]=[n,d]
out['required_pair_ranks']={id:{work:ranks[id].index(work)+1 for work in ['philebus','antigone','disgrace','gilgamesh']} for id in list(qs)[:6]}
out['paired_sparse_changes']={id:{work:{'plain':plain[id][work],'context':context[id][work],'frozen':'relevant' if work in qs[id]['relevant'] else 'borderline' if work in qs[id]['borderline'] else 'irrelevant'} for work in ('philebus','antigone','disgrace')} for id in context}
(P/'thematic-chat-report.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
for m,res in out['methods'].items():print(m, 'tp/clear_returned/expected',res['counts']['all'],'no-match',[(i,d['no_match_ok']) for i,d in res['details'].items() if d['no_match_ok'] is not None])
print('valid',len(qs),'invalid',invalid,'attempted without saved response',sorted(set(out['attempts'])-{r['query_id'] for r in records}-set(invalid)));print('candidate_coverage',out['candidate_coverage']);print('pair_ranks',out['required_pair_ranks']);print('paired_sparse_changes',out['paired_sparse_changes'])
