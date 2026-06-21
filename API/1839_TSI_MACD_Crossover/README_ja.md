# TSI MACDクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

True Strength Index（TSI）とその指数移動平均シグナルラインに基づくクロスオーバーシステムを実装します。

戦略はデフォルトで4時間足を購読し、設定可能な短期・長期の平滑化期間を使用してTSIを計算します。追加のEMAがシグナルラインを生成します。TSIがシグナルラインを上抜けするとロングポジションをオープンします。TSIがシグナルラインを下抜けするとショートポジションをオープンします。逆クロスでは反対ポジションが自動的にクローズされます。

- インジケーター: True Strength Index, Exponential Moving Average
- パラメーター:
  - `CandleType` – 処理するローソク足シリーズ。
  - `LongLength` – TSIの長期平滑化期間。
  - `ShortLength` – TSIの短期平滑化期間。
  - `SignalLength` – EMAシグナルラインの期間。
