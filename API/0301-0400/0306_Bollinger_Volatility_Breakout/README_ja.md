# Bollinger Volatility Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

確定した5分足を使用し、指標の形成と取引許可を待ちます。移動統計にはシグナル足を含めます。`C`は終値、`M = SMA(C, 20)`、`D = 母標準偏差(C, 20)`、`U = M + 2 × D`、`L = M − 2 × D`、`A = ATR(14)`で、真の値幅をWilder方式で平滑化します。

高ボラティリティ条件は `A >= SMA(A, 14) + 2 × 母標準偏差(A, 14)` です。価格水準はボリンジャーバンドの上限、中央、下限です。

建玉がないとき、高ボラティリティが確認され `C >= U` なら買い、`C <= L` なら売ります。新規数量は `Volume` です。待機期間が終了し、未完了注文がない場合のみ新規エントリーできます。 エントリーには `A > 0` が必要です。

`k = StopLossMultiplier`. エントリー時のシグナル足の終値とATRを固定します。買い建玉のストップは `C_entry − k × A_entry`、売り建玉は `C_entry + k × A_entry` です。各足の確定時に終値が固定ストップに達した場合、買いで `C <= M`、売りで `C >= M` の場合、または高ボラティリティ条件が失効した場合、実際の残存建玉を全量成行で決済します。

注文後の次の84本の確定足では新規エントリーを停止しますが、決済判定は継続します。ストップは足の終値で判定し、成行注文は利用可能な価格で約定します。保有中のストップ距離は固定です。利食い注文とトレーリングストップはありません。

## 既定パラメーター

```text
BollingerPeriod = 20
BollingerDeviation = 2
AtrPeriod = 14
AtrDeviationMultiplier = 2
StopLossMultiplier = 2
CooldownBars = 84
CandleType = TimeSpan.FromMinutes(5).TimeFrame()
```
