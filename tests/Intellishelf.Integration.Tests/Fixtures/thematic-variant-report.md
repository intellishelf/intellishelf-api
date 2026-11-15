# Calibration-only relevance variants — 2026-11-15

Frozen specification SHA-256 `72c5d40e668b79826c9b91381ea488b8f4fbb7873e1793a7da617bcbad653123`; corpus SHA-256 `0d36fae51ac228f2e7a1b4913086d56d4b25fa8614f8fb919391fdaf9c128cc3`. No held-out query was recorded or scored. The model is `text-embedding-3-large` (1536 dimensions). One input per request, 72 actual attempts, zero retries; no additional paid requests or private data. The context strings are independently authored illustrative bibliographic facts, not verified catalog records, and do not establish availability for real books.

Counts below are true positives / clearly judged returned (precision), and true positives / all relevant pairs (recall); the denominator is 83 relevant calibration pairs. Borderline pairs are excluded from both. Per-query, category, language, sparse/enriched and false-positive counts are in `thematic-variant-summary.json` and `thematic-variant-relative-summary.json`. Full global scores/sweeps and relative summaries are preserved compressed. Global and relative sweeps choose the fewest named-case failures first, then maximum calibration F1; they are exploratory, not selected held-out candidates. Offline scores approximate Atlas cosine; endpoint order and keyword matches require real replay.

| Variant (books / query) | Global cutoff | Global precision | Global recall | Named failures | Relative floor / margin | Relative precision | Relative recall | Named failures |
|---|---:|---:|---:|---|---:|---:|---:|---|
| original__original | 0.649060 | 45/83 | 45/83 | q02, q03, q05, q07 | 0.621 / 0.065 | 64/103 | 64/83 | q02, q03, q07 |
| original__library_request | 0.674678 | 53/93 | 53/83 | q02, q05 | 0.675 / 0.040 | 49/62 | 49/83 | q02, q05 |
| original__matching_book | 0.694623 | 50/73 | 50/83 | q02, q05 | 0.675 / 0.045 | 66/85 | 66/83 | q03, q07 |
| original__hybrid_original_matching | 0.678164 | 34/55 | 34/83 | q02, q05, q07 | 0.647 / 0.045 | 67/90 | 67/83 | q02, q03, q07 |
| original__hybrid_both_templates | 0.684452 | 52/80 | 52/83 | q02, q05 | 0.668 / 0.045 | 65/86 | 65/83 | q03, q07 |
| context__original | 0.628540 | 63/148 | 63/83 | q02, q03, q06, q07 | 0.628 / 0.050 | 63/81 | 63/83 | q02, q03, q07 |
| context__library_request | 0.687531 | 39/55 | 39/83 | q02, q05 | 0.661 / 0.035 | 60/74 | 60/83 | q07, q08 |
| context__matching_book | 0.676449 | 66/112 | 66/83 | q03, q06, q07 | 0.677 / 0.050 | 64/80 | 64/83 | q03, q07 |
| context__hybrid_original_matching | 0.682192 | 33/48 | 33/83 | q02, q03, q05 | 0.653 / 0.045 | 65/78 | 65/83 | q02, q03, q07 |
| context__hybrid_both_templates | 0.691909 | 42/56 | 42/83 | q02, q05 | 0.672 / 0.030 | 54/62 | 54/83 | q02, q07 |

`q01` is Ukrainian antiquity, `q02` English classical antiquity, `q03` Greek works, `q04` philosophy, `q05` modern prose, `q06` Greek dialogues, `q07` unrelated quantum physics, and `q08` unsupported medieval Japanese poetry. The full sweep includes every evaluated breakpoint and relative combination. Zero global or relative variants met every named admission and empty-result requirement. Relaxing only the Greek-query exclusion for Gilgamesh to an adjacent result still produced zero passing variants; no-match outcomes remain strict and original labels remain intact. The report's narrow adjacency set also identifies other nearby antiquity/science distractors separately from the remaining clear false positives; this does not relabel the corpus or turn an unsupported category into a correct no-match.

Two global probes were run through the authenticated Pro endpoint on Atlas Local 8.0 with the frozen vectors and the actual modified metadata/keyword index; membership matched offline predictions exactly:

- Original metadata + `matching_book` at 0.694623: 50/73 precision, 50/83 recall. English 44/60 and 44/60; Ukrainian 6/13 and 6/23; sparse 8/14 and 8/10. Both no-match queries were empty, but required Philebus and Disgrace misses remained and the focused-dialogue ordering failed.
- Added context + `matching_book` at 0.6764492339469844: 66/112 precision, 66/83 recall. English 58/95 and 58/60; Ukrainian 8/17 and 8/23; sparse 10/17 and 10/10. It returned Gilgamesh and Roman works for Greek works, Antigone and other off-topic works for Greek dialogues, and Cosmos for quantum physics. It passed the curated ordering in this replay but failed the required exclusions and no-match.

For comparison, the original endpoint baseline at 0.7285 had 10/11 precision and 10/83 recall; English 10/11 and 10/60, Ukrainian 0/0 and 0/23, sparse 0/0 and 0/10. At that same cutoff, extra context alone returned 13/14 and 13/83, including 2/2 and 2/10 for sparse pairs. This shows an improvement on some sparse records, not a general fix. The original matching-book wrapper global probe alone required no enriched book vectors, but still failed named cases and Ukrainian results. Across all variants top-20 vector candidates covered all 83 calibration positives in this small library; top-5 covered 44–47 and top-10 65–71. A fixed quota is neither tested as a production behavior nor justified by this dataset.

The two `hybrid` modes average two recorded query-to-book cosine scores offline; they were exploratory combinations added after single-vector measurements, not predeclared templates or endpoint-verified implementations. The best context + original/matching hybrid relative example has 65/78 precision and 65/83 recall, with English 57/64 and 57/60, Ukrainian 8/14 and 8/23, and sparse 9/13 and 9/10. It still misses Philebus for English classical antiquity and fails Greek exclusion and the unrelated empty case; its ranking was not verified. Implemented naively it would require two query embedding calls per search and a new ranking design.

A query wrapper could use the existing single provider call without reindexing, but changes all future query inputs and was not robust here. Added metadata requires obtaining and maintaining suitable data, choosing what is sent to the provider, and regenerating existing vectors; no enrichment source or reindexing path was authorized or implemented. Scores, cutoffs, generic wrappers and context are calibration-specific and remain unvalidated on the untouched held-out set. No candidate is recommended for delivery on this evidence.

Reproduce from API root:

```sh
python3 tests/Intellishelf.Integration.Tests/Tools/thematic-variant-analysis.py
python3 tests/Intellishelf.Integration.Tests/Tools/thematic-variant-relative.py
python3 tests/Intellishelf.Integration.Tests/Tools/thematic-variant-tolerance.py
python3 tests/Intellishelf.Integration.Tests/Tools/thematic-report.py calibration tests/Intellishelf.Integration.Tests/Fixtures/thematic-variant-context-matching-endpoint.json
```

The endpoint probes can be rerun key-free with `ThematicVariantsReplayTests`, an absolute `THEMATIC_VARIANT_OUTPUT`, query mode `matching_book`, the documented cutoff and, for added context, `THEMATIC_DOC_MODE=context`. The paid recorder is deliberately opt-in and its transport ledger prevents repeated inputs and exceeding the authorized 72 attempts.
