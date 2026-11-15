#!/usr/bin/env python3
"""Vector-only comparisons; raw query vectors are reused, no network or held-out scoring."""
import base64,hashlib,json,math,re,struct
from pathlib import Path
p=Path(__file__).resolve().parent.parent/'Fixtures'
c=json.loads((p/'thematic-corpus.json').read_text());spec=json.loads((p/'thematic-tag-spec.json').read_text());old=json.loads((p/'thematic-vectors.json').read_text())['Vectors'];new=json.loads((p/'thematic-tag-vectors.json').read_text())['vectors'];books={b['id']:b for b in c['books']};queries=[q for q in c['queries'] if q['split']=='calibration'];assert len(queries)==18
norm=lambda s:re.sub(r'\s+',' ',(s or '').strip())
def booktext(b,tagged):
 tags=', '.join(norm(t) for t in spec['tags'][b['id']]) if tagged else ''
 return f"Title: {norm(b['title'])}\nAuthors: {', '.join(map(norm,b['authors']))}\nTags: {tags}\nPublisher: \nDescription: {norm(b['description'])}\nAnnotation: "
def vec(d,s):
 raw=base64.b64decode(d[hashlib.sha256(s.encode()).hexdigest().upper()]);assert len(raw)==6144;return struct.unpack('<1536f',raw)
def score(a,b):return (1+sum(x*y for x,y in zip(a,b))/math.sqrt(sum(x*x for x in a)*sum(y*y for y in b)))/2
def tokens(s):return set(re.findall(r'[^\W_]+',s.casefold(),flags=re.UNICODE))
def overlap(q,id):return bool(tokens(q['prompt']) & tokens(' '.join(spec['tags'][id])))
pairs={}
for q in queries:
 a=vec(old,q['prompt']); rows={}
 for id,b in books.items():
  label='relevant' if id in q['relevant'] else 'borderline' if id in q['borderline'] else 'irrelevant'
  rows[id]={'label':label,'overlap':overlap(q,id),'sparse':b['metadata']=='sparse','language':q['language'],'before':score(a,vec(old,booktext(b,False))),'tagged':score(a,vec(new,booktext(b,True)))}
 for mode in ('before','tagged'):
  for i,id in enumerate(sorted(rows,key=lambda id:-rows[id][mode])):rows[id][mode+'_rank']=i+1
 pairs[q['id']]=rows
out={'overlap_definition':'At least one exact case-folded Unicode alphanumeric token occurs in both the original prompt and one of the three English tag phrases; no stemming, translation or stopword removal. This is a lexical diagnostic, not Atlas analyzer behavior.', 'pairs':pairs,'metrics':{}}
for mode in ('before','tagged'):
 for cutoff in (0.7285,0.67):
  groups={};details={}
  for q in queries:
   id=q['id'];r=pairs[id];hits={i for i,x in r.items() if x[mode]>=cutoff};rel=set(q['relevant']);irr=set(q['irrelevant']); details[id]={'returned':sorted(hits),'missed':sorted(rel-hits),'irrelevant':sorted(hits&irr),'no_match_ok':not hits if not rel and not q['borderline'] else None}
   for bookid,x in r.items():
    if x['label']=='borderline':continue
    member=[('all',True),('language:'+q['language'],True),('metadata:'+books[bookid]['metadata'],True),('tag_overlap' if x['overlap'] else 'no_tag_overlap',True),('language:'+q['language']+('/tag_overlap' if x['overlap'] else '/no_tag_overlap'),True),('category:'+q['category'],True)]
    for group,_ in member:
     a=groups.setdefault(group,[0,0,0]);positive=x['label']=='relevant';returned=bookid in hits;a[0]+=positive and returned;a[1]+=returned;a[2]+=positive
  coverage={}
  for k in (5,10,20):
   groups2={}
   for q in queries:
    for id,x in pairs[q['id']].items():
     if x['label']!='relevant':continue
     for group in ('all','language:'+q['language'],'metadata:'+books[id]['metadata'],'tag_overlap' if x['overlap'] else 'no_tag_overlap'):
      a=groups2.setdefault(group,[0,0]);a[0]+=x[mode+'_rank']<=k;a[1]+=1
   coverage[str(k)]=groups2
  out['metrics'][f'{mode}_{cutoff}']={'groups_tp_returned_expected':groups,'details':details,'coverage':coverage}
(p/'thematic-tag-analysis.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
for k,v in out['metrics'].items():
 print(k,{key:v['groups_tp_returned_expected'][key] for key in ('all','tag_overlap','no_tag_overlap','language:en','language:uk','metadata:sparse','metadata:enriched')},'top', {t:v['coverage'][t]['all'] for t in ('5','10','20')},'no-match',[(q,d['no_match_ok']) for q,d in v['details'].items() if d['no_match_ok'] is not None])
