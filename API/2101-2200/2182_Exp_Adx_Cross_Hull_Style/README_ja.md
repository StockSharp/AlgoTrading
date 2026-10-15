# Exp ADX Cross Hull Style戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Average Directional Index（ADX）のクロスシグナルとHull Moving Average（HMA）フィルターを組み合わせています。+DIラインが-DIラインを上抜けるとロングエントリー、下抜けるとショートエントリーします。エグジットはHull移動平均線のペアで管理され、速いHMAが遅いHMAをクロスするとポジションが閉じられます。デフォルトでは4時間足で動作します。

## 詳細
- **エントリー条件**  
  - **ロング**: +DIが-DIを上抜け。  
  - **ショート**: -DIが+DIを上抜け。
- **エグジット条件**  
  - **ロング**: 速いHMAが遅いHMAを下抜け。  
  - **ショート**: 速いHMAが遅いHMAを上抜け。
- **インジケーター**  
  - AverageDirectionalIndex（期間14）。  
  - HullMovingAverage 速い長さ20。  
  - HullMovingAverage 遅い長さ50。
- **時間軸**: 4時間足（設定可能）。
- **ストップ**: デフォルトなし。
- **方向**: ロングとショート両方。

この戦略は過去のデータコレクションに依存せず、ストリーミングローソク足データに反応します。パラメーターは異なる市場向けに最適化できます。チャート出力は両方のHullとトレードマークを含む価格ローソク足を表示します。
