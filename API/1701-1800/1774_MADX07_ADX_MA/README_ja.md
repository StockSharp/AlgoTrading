# MADX-07 ADX MA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMQL4エキスパートアドバイザーMADX-07から変換されました。H4ローソク足で取引し、2本の移動平均線とフィルターとしてのAverage Directional Index (ADX)を組み合わせています。

## ロジック

- ロングエントリー: 価格が遅いMAの上にあり、速いMAが遅いMAの上にあり、直近2本のローソク足で価格が速いMAより少なくとも`MaDifference`ポイント上にあり、ADXが`AdxMainLevel`を上回って上昇し、+DIが上昇して-DIが下落している。
- ショートエントリー: 逆の条件。
- ポジションは、ポイント単位の利益が`CloseProfit`に達したとき、または`TakeProfit`距離での指値注文が約定したときに決済されます。

## パラメーター

- `BigMaPeriod` (25) – 遅いMAの期間。
- `BigMaType` – 遅いMAの種類。
- `SmallMaPeriod` (5) – 速いMAの期間。
- `SmallMaType` – 速いMAの種類。
- `MaDifference` (5) – 価格と速いMAの間の最小距離（ポイント単位）。
- `AdxPeriod` (11) – ADXの計算期間。
- `AdxMainLevel` (13) – ADXの最小値。
- `AdxPlusLevel` (13) – +DIの最小値。
- `AdxMinusLevel` (14) – -DIの最小値。
- `TakeProfit` (299) – ポイント単位でのテイクプロフィット距離。
- `CloseProfit` (13) – 早期エグジットのためのポイント単位の利益。
- `Volume` (0.1) – 取引量。
- `CandleType` – ローソク足の時間軸（デフォルトはH4）。
