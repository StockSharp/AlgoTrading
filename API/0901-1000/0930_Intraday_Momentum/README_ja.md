# イントラデイ・モメンタム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

指定されたセッション内でEMAクロスオーバー、RISフィルター、VWAPの確認を使用してトレードします。高速EMAが低速EMAを上抜け、RSIが買われすぎレベル以下で価格がVWAP以上のときにロングポジションを取ります。逆の条件でショートポジション。固定のストップロスとテイクプロフィットのパーセンテージを適用し、セッション終了時にすべてのポジションをクローズします。

## パラメーター

- **EmaFastLength**: 高速EMAの長さ。
- **EmaSlowLength**: 低速EMAの長さ。
- **RsiLength**: RSIの期間。
- **RsiOverbought**: RSIの買われすぎレベル。
- **RsiOversold**: RSIの売られすぎレベル。
- **StopLossPerc**: ストップロスのパーセンテージ。
- **TakeProfitPerc**: テイクプロフィットのパーセンテージ。
- **StartHour**: セッション開始時間。
- **StartMinute**: セッション開始分。
- **EndHour**: セッション終了時間。
- **EndMinute**: セッション終了分。
- **CandleType**: ローソク足の種類。

