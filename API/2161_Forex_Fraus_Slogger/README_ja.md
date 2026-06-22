# Forex Fraus Slogger戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMetaTraderのエンベロープ・リバーサルシステムを再現したものです。

## ロジック

- 1期間のSMAをベース価格として計算します。
- 上下のエンベロープはベースから`EnvelopePercent`パーセントの位置に設定されます。
- 価格が上限バンドの上で終値をつけた後に下に戻った場合、ショートポジションに入ります。
- 価格が下限バンドの下で終値をつけた後に上に戻った場合、ロングポジションに入ります。
- ポジションはトレーリングストップで保護されます。

## パラメーター

- `EnvelopePercent` – エンベロープのパーセントオフセット（デフォルト 0.1）。
- `TrailingStop` – 価格単位でのトレーリングストップ距離（デフォルト 0.001）。
- `TrailingStep` – トレーリングストップを進めるために必要な最小価格変動（デフォルト 0.0001）。
- `ProfitTrailing` – ポジションが利益を出した後にのみトレーリングを有効化。
- `UseTimeFilter` – 指定した時間帯のみ取引。
- `StartHour` – 取引ウィンドウの開始。
- `StopHour` – 取引ウィンドウの終了。
- `CandleType` – 計算に使用するローソク足の時間軸。

## 注意事項

- この戦略は`BuyMarket`と`SellMarket`を通じて成行注文を使用します。
- トレーリングストップは価格がストップレベルを超えたときにポジションを決済します。
