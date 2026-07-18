# CCI Woodies戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略はWoodies CCIメソッドから派生した2本のコモディティチャンネルインデックス（CCI）ラインのクロスオーバーに基づいて取引します。指定された時間軸で高速CCIと低速CCIが計算されます。高速ラインが低速ラインを下抜けすると、ロングポジションが建てられ、ショートポジションがクローズされます。高速ラインが低速ラインを上抜けすると、ショートポジションが建てられ、ロングポジションがクローズされます。

## パラメーター
- **FastPeriod** – 高速CCIインジケーターの長さ。
- **SlowPeriod** – 低速CCIインジケーターの長さ。
- **CandleType** – 計算に使用するローソク足の時間軸。
- **InvertSignals** – 有効にすると、買いと売りのルールが入れ替わります。
- **TakeProfitPoints** – 価格ポイントでの利益目標。
- **StopLossPoints** – 価格ポイントでの損失制限。

## 注意事項
この戦略はStockSharpの高水準APIを使用します。インジケーターは `Bind` を通じて連結され、リスク管理はストップロスとテイクプロフィットレベルを使用した `StartProtection` で処理されます。
