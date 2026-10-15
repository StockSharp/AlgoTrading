# ボリューム支援線形回帰トレンド修正戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、14期間RSIがエントリーレベルを上回ったときにロングポジションを建て、RSIがエグジットレベルを超えたときにポジションを決済します。

## パラメーター
- **RSI Period** – RSI計算の振り返り期間。
- **Entry Level** – ロングエントリーをトリガーするRSI値。
- **Exit Level** – ポジションを決済するRSI値。
- **Candle Type** – 処理するローソク足の時間軸。
