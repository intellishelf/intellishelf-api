#!/usr/bin/env python3
"""Report a single endpoint replay; never select a candidate using held-out results."""
import argparse
import collections
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('split', choices=['calibration', 'held-out'])
parser.add_argument('results', type=Path)
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent / 'Fixtures'
corpus = json.loads((root / 'thematic-corpus.json').read_text())
results = json.loads(args.results.read_text())
books = {b['id']: b for b in corpus['books']}
queries = [q for q in corpus['queries'] if q['split'] == args.split]
assert set(results) == {q['id'] for q in queries}
counts = collections.defaultdict(lambda: [0, 0, 0]) # TP, returned clear, expected clear
for q in queries:
    returned = results[q['id']]
    assert len(set(returned)) == len(returned) and set(returned) <= books.keys()
    relevant, irrelevant = set(q['relevant']), set(q['irrelevant'])
    for group, eligible in [('all', set(books)), (f"category:{q['category']}", set(books)),
                            (f"language:{q['language']}", set(books)),
                            *[(f'metadata:{kind}', {i for i, b in books.items() if b['metadata'] == kind}) for kind in ('sparse', 'enriched')]]:
        counts[group][0] += len(set(returned) & relevant & eligible)
        counts[group][1] += len(set(returned) & (relevant | irrelevant) & eligible)
        counts[group][2] += len(relevant & eligible)
    missed = sorted(relevant - set(returned)); false = sorted(set(returned) & irrelevant)
    order_fail = q['rank_relevant_before_borderline'] and any(returned.index(a) > returned.index(b) for a in returned if a in relevant for b in returned if b in q['borderline'])
    print(q['id'], q['category'], q['language'], 'missed=', missed, 'irrelevant=', false, 'no-match=', ('pass' if not returned else 'FAIL') if not relevant and not q['borderline'] else 'n/a', 'order=', 'FAIL' if order_fail else 'pass')
for group, (tp, displayed, expected) in sorted(counts.items()):
    print(f'{group}: precision {tp}/{displayed} ({tp/displayed:.3f}) recall {tp}/{expected} ({tp/expected:.3f})' if displayed and expected else f'{group}: precision {tp}/{displayed}, recall {tp}/{expected}')
