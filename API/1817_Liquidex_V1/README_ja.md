# Liquidex V1戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Liquidex V1は元のMQLエキスパートアドバイザーから変換されたブレイクアウトスキャルピング戦略です。**レンジフィルター**と**加重移動平均（WMA）**を組み合わせて短期的な機会を特定します。

## トレードロジック
1. 完成した各ローソク足のレンジ（`high - low`）を計測します。
2. ローソク足のレンジが`RangeFilter`より小さい場合、そのローソク足は無視されます。
3. 終値を使用して期間`MaPeriod`のWMAを計算します。
4. ローソク足がWMAの下で始まりWMAの上で終わった場合、**買い**成行注文が送信されます。
5. ローソク足がWMAの上で始まりWMAの下で終わった場合、**売り**成行注文が送信されます。
6. 各ポジションは`StopLoss`で定義されたストップロスで保護されます。

## パラメーター
- `RangeFilter` – 取引に必要な価格単位での最小ローソク足レンジ。
- `MaPeriod` – 加重移動平均の期間数。
- `StopLoss` – ポイント単位の保護ストップロス。
- `CandleType` – 分析に使用するローソク足シリーズ。

戦略は注文サイズとして`Strategy.Volume`を使用し、逆のシグナルが現れるとポジションを反転させます。
