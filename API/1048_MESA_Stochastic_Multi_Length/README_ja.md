# MESA Stochastic マルチレングス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は異なる振り返り期間を持つ4つのMESA Stochasticオシレーターを使用します。4つすべてのオシレーターが移動平均トリガーを上回ったときにロングポジションを建てます。4つすべてがトリガーを下回ったときにショートポジションを建てます。

## パラメーター
- `Length1` – 第1オシレーターの振り返り期間。
- `Length2` – 第2オシレーターの振り返り期間。
- `Length3` – 第3オシレーターの振り返り期間。
- `Length4` – 第4オシレーターの振り返り期間。
- `TriggerLength` – トリガー移動平均の平滑化期間。
- `CandleType` – ローソク足の時間軸。
