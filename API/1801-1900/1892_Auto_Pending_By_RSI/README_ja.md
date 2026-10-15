# RSIによる自動ペンディング注文戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、相対力指数（RSI）が数本連続したローソク足にわたって極端なゾーンに留まった後、ペンディング指値注文を発注します。

RSIが`MatchCount`本のローソク足にわたって売られすぎレベルを下回り続けると、ローソク足の終値より`PendingOffset`価格ポイント下に買い指値注文が登録されます。RSIが同じ本数のローソク足にわたって買われすぎレベルを上回り続けると、同じオフセットで終値より上に売り指値注文が配置されます。

## パラメーター
- `RsiPeriod` – RSI計算期間。
- `RsiOverbought` – 買われすぎゾーンを定義するレベル。
- `RsiOversold` – 売られすぎゾーンを定義するレベル。
- `PendingOffset` – ペンディング注文を配置するための終値からの距離（価格ポイント）。
- `MatchCount` – 注文を配置する前に必要な連続ローソク足の数。
- `CandleType` – 分析に使用するローソク足の時間軸。

デフォルト値は元のMQLスクリプトを模倣しており、4時間足のローソク足を使用します。
