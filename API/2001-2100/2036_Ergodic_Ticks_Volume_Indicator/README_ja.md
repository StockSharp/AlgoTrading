# Ergodic Ticks出来高インジケーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はローソク足データにTrue Strength Index（TSI）を適用し、指数移動平均シグナルラインと比較します。TSIがシグナルラインを上抜けするとロングポジションが開かれ、下抜けするとショートポジションが開かれます。

## パラメーター

- **Candle Type** – 計算に使用するローソク足の時間軸。
- **Short Length** – TSIの速いスムージング期間。
- **Long Length** – TSIの遅いスムージング期間。
- **Signal Length** – シグナルラインとして使用するEMAの期間。

## ロジック

1. 選択した時間軸のローソク足を購読する。
2. 完成した各ローソク足のTSIを計算する。
3. EMAでTSIを処理してシグナルラインを得る。
4. TSIがシグナルラインを上抜けすると、ロングに入る（ショートポジションがあれば決済）。
5. TSIがシグナルラインを下抜けすると、ショートに入る（ロングポジションがあれば決済）。

この戦略はMQLサンプル"exp_ergodic_ticks_volume_indicator.mq5"の適応版であり、StockSharpの組み込みインジケーターのみを使用します。
