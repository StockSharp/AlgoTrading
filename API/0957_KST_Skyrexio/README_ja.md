# KST戦略 Skyrexio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Know Sure Thing (KST)インジケーターがシグナルラインを上抜けし、価格が選択した移動平均とAlligatorのジョーラインを上回っているときにロングエントリーします。チョッピネスインデックスフィルターにより、レンジ相場でのエントリーを無効化できます。ポジションはATRベースのストップロスとテイクプロフィットレベルで決済されます。

- **エントリー条件**: KSTがシグナルを上抜け、価格がフィルターMAとAlligatorジョーを上回り、チョッピネスがしきい値以下。
- **エグジット条件**: 価格がATRストップロスまたはATRテイクプロフィットに到達。
- **インジケーター**: KST、ATR、Moving Average、Alligatorジョー、チョッピネスインデックス。

## パラメーター
- `CandleType` – ローソク足の時間軸。
- `AtrStopLoss` – ストップロス用ATR乗数。
- `AtrTakeProfit` – テイクプロフィット用ATR乗数。
- `FilterMaType` – トレンドフィルターMAの種類。
- `FilterMaLength` – トレンドフィルターMAの長さ。
- `EnableChopFilter` – チョッピネスフィルターを有効化。
- `ChopThreshold` – チョッピネスインデックスのしきい値。
- `ChopLength` – チョッピネスインデックスの期間。
- `RocLen1..4` – KST用ROCの長さ。
- `SmaLen1..4` – KST用SMAの長さ。
- `SignalLength` – KSTシグナルSMAの長さ。
