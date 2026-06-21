# ボリューム加重Supertrend戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、ボリューム加重移動平均とATRバンドに基づくSupertrendを計算します。トレンドの強さを確認するために、ボリュームに第2のSupertrendを適用します。ボリュームと価格のトレンドが共に上昇方向に揃ったときにロングポジションを建て、条件が反転したときに決済します。

## パラメーター
- **ATR Period** – 価格トレンド用のATR期間。
- **Volume Period** – VWAPとボリュームトレンドの期間。
- **Factor** – ATR乗数。
- **Candle Type** – 処理するローソク足の時間軸。
