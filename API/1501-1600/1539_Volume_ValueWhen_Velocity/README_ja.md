# 出来高 ValueWhen ベロシティ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、出来高が拡大し、RSIに基づいて市場が売られすぎの状態にあり、ATRで測定されたボラティリティが収縮しており、直近のSMAブレイクアウト間の距離が指定値を超えたときにロングエントリーを探します。すべての条件が満たされると、成行買い注文が発行されます。

## パラメーター
- **RSI Length** – RSIの期間。
- **RSI Oversold** – 売られすぎの閾値。
- **ATR Small / ATR Big** – ATR比較のための期間。
- **Distance** – ブレイクアウト価格間の最小差。
- **Candle Type** – 入力ローソク足の時間軸。
