# Synthetic tag comparison — calibration only

Three short English tags per work were frozen in `thematic-tag-spec.json` (SHA-256 `4fa6d4cbbf0fb2fa671a9f915609c06785d93bf54958f471d7c5a7496c9b5bbd`) before recording. The same 36 synthetic bibliographic works, 18 calibration queries, existing descriptions and query vectors were used in both conditions; only the production `Tags` field changed. There were 36 actual new embedding attempts, zero retries and no new query embeddings. The corpus and labels were not changed and no held-out query was recorded or scored. Tags were independently authored as brief work-form, place/era and subject descriptors; prior familiarity with the works and research cases was unavoidable, and the text is not verified catalog metadata or a blinded test.

**Result:** tags improved some semantic scores and relevant admissions, including some with no exact query/tag word in common. They did not reliably solve the named cases, multilingual performance, no-match behavior or ranking. This small, familiar calibration set cannot establish production quality or the availability of similar tags for real books.

## Vector-only admission

Counts are clearly relevant returned / all clearly judged returned (precision) and clearly relevant returned / clearly relevant expected (recall). Borderline pairs are excluded. These results are cosine scores before the API adds keyword matches; values use Atlas-equivalent `(1 + cosine) / 2`.

| Cutoff | Condition | Precision | Recall | EN recall | UK recall | Sparse recall |
|---|---|---:|---:|---:|---:|---:|
| 0.7285 | Before | 9/10 | 9/83 | 9/60 | 0/23 | 0/10 |
| 0.7285 | Tags | 12/13 | 12/83 | 12/60 | 0/23 | 2/10 |
| 0.67 | Before | 22/38 | 22/83 | 19/60 | 3/23 | 3/10 |
| 0.67 | Tags | 33/53 | 33/83 | 30/60 | 3/23 | 6/10 |

Both missing-category queries were empty at 0.7285 in both conditions. At 0.67 the unrelated quantum-physics query returned *Cosmos* in both conditions; the close unsupported medieval-Japanese-poetry query stayed empty. At 0.67 the tagged Greek-works query also returned Roman *Aeneid*, *Metamorphoses* and *Meditations*, despite admitting the two required sparse Greek works.

Exact overlap here means at least one case-folded Unicode alphanumeric token occurs in the prompt and one tag, without stemming, translation or stopword removal. This is a simple lexical diagnostic, not the Atlas analyzer, and it does not prove whether a match was caused by a word rather than its meaning. No Ukrainian positive pair had exact overlap with the English-only tags.

| Cutoff | Condition | Overlap precision | Overlap recall | No-overlap precision | No-overlap recall |
|---|---|---:|---:|---:|---:|
| 0.7285 | Before | 2/3 | 2/15 | 7/7 | 7/68 |
| 0.7285 | Tags | 4/5 | 4/15 | 8/8 | 8/68 |
| 0.67 | Before | 7/14 | 7/15 | 15/24 | 15/68 |
| 0.67 | Tags | 14/24 | 14/15 | 19/29 | 19/68 |

The vector top-5 relevant coverage changed 45/83 → 44/83; top-10 65/83 → 67/83; top-20 83/83 → 83/83. The top-20 observation is specific to this 36-work library and does not justify a fixed production result quota. Language, category, original sparse/enriched metadata, overlap intersections, all pair scores/ranks, and exact false-positive/miss lists are in `thematic-tag-analysis.json`.

## Named examples and negatives

- `ancient Greek works`: *Philebus* rose 0.6585 → 0.6977 and *Antigone* 0.6665 → 0.6908, both sharing the exact token `ancient` with their tags. At 0.67 both become vector matches, but three Roman works also appear. At 0.7285 both remain absent.
- `philosophy` × *Philebus*: 0.6519 → 0.6723 with no exact prompt/tag token overlap; it crosses 0.67 but not 0.7285. This is consistent with a semantic gain in this pair, not proof the change generalizes.
- `modern prose` × *Disgrace*: 0.6219 → 0.6368 with no exact prompt/tag overlap. Its score and rank improve, but it remains absent at both tested cutoffs.
- `античність`: *Philebus* 0.6669 → 0.6697 and *Antigone* 0.7073 → 0.7031, with no exact overlap. At 0.67 *Philebus* is still just below the cutoff; at 0.7285 both remain absent. For `classical antiquity`, *Philebus* rises 0.6043 → 0.6466 with token overlap `classical`, but remains below both cutoffs.
- The unrelated quantum-physics prompt scores *Cosmos* 0.6790 → 0.6755, so it remains an incorrect 0.67 match. For Greek works, *Gilgamesh* moves 0.6615 → 0.6632 and falls just below 0.67, but Roman distractors do not.

## Normal endpoint and keyword behavior

The authenticated Pro Explore endpoint was replayed against isolated Atlas Local 8.0 with original query vectors and matching book vectors/tags. Baseline at 0.7285 was 10/11 precision and 10/83 recall; tags gave 13/14 and 13/83. At 0.67 baseline was 22/38 and 22/83; tags gave 33/53 and 33/83. At 0.67 endpoint membership matched vector-only membership for all 18 queries. At 0.7285 a keyword-only *Descartes* match added one relevant item to both vector-only lists. Endpoint output lists and per-query details are retained in `thematic-tag-{baseline-067,endpoint-067,endpoint-07285}.json` and the earlier `thematic-baseline-results.json`.

The tagged keyword-only replay (cutoff 1.0) added *Aeneid* for `Roman epic poetry` compared with baseline keyword-only output; it was already retrieved by its vector at both tested cutoffs, so it did not add a new endpoint result there. Tagged vector changes did change shared-result order in five queries at 0.67, including moving *Philebus* first for Greek dialogues; the curated comparison still fails because borderline *Apology* precedes relevant *Republic*. Neither tested corpus contains a query exactly equal to one whole tag.

In the earlier direct API observation relayed from the conversation, `modern prose` returned *Synthetic Essays* (separate `modern history` and `short prose` tags) before *Disgrace* (whole tag `modern prose`, plus `literary fiction`). That observation was not rerun in this experiment. Source inspection shows the exact priority tier compares title or author, not whole tags; tags are included in general keyword matching and ranking, so an exact whole-tag match does not receive a dedicated priority tier.

Reproduce offline with `python3 tests/Intellishelf.Integration.Tests/Tools/thematic-tag-analysis.py` and endpoint result breakdowns with `python3 tests/Intellishelf.Integration.Tests/Tools/thematic-report.py calibration <absolute-or-relative-result-json>`. Endpoint tests use `ThematicTagsReplayTests`, `THEMATIC_TAG_MODE=on` for tags, `THEMATIC_CUTOFF`, and an absolute `THEMATIC_TAG_OUTPUT`; no key is needed. Do not rerun the opt-in recorder: its 36-attempt authorization is exhausted.
