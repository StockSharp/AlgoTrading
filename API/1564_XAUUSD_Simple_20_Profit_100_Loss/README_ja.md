# XAUUSD シンプル 20 利益 100 損失戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、ポジションが保有されておらず、両方のクールダウンタイマーが非アクティブな場合にロングポジションを建てます。
未実現利益が $20 に達するか、損失が $100 に達した時点でポジションを決済します。
利益確定後は 15 分間待ってから再エントリーし、損切り後は 12 時間待ちます。

## パラメーター

- `ProfitTarget` – USD での利益目標。
- `LossLimit` – USD での最大損失。
- `TradeCooldown` – 損失後の待機時間。
- `EntryCooldown` – 利益後の待機時間。
- `CandleType` – ローソク足の時間軸。
