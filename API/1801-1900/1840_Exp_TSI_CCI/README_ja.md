# Exp TSI CCI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はCommodity Channel Index（CCI）に基づいてTrue Strength Index（TSI）を計算し、シグナルラインとのクロスオーバーで取引を行います。

## ロジック
- 指定された期間を使用してCCIを計算します。
- CCI値を短期・長期の平滑化期間を持つTrue Strength Indexに入力します。
- 結果のTSIをEMAで平滑化してシグナルラインを取得します。
- TSIがシグナルラインを上抜けしたらロングに入ります。
- TSIがシグナルラインを下抜けしたらショートに入ります。

## パラメーター
- `Candle Type` – 分析に使用するローソク足の時間軸。
- `CCI Period` – Commodity Channel Indexの期間。
- `TSI Short Length` – TSIの短期平滑化期間。
- `TSI Long Length` – TSIの長期平滑化期間。
- `Signal Length` – TSIシグナルラインのEMA期間。

## インジケーター
- Commodity Channel Index
- True Strength Index
- Exponential Moving Average

## 免責事項
この戦略は教育目的のみに提供されており、投資アドバイスを構成するものではありません。
