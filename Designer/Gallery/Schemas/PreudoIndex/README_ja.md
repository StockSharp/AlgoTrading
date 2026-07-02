# PseudoIndex 戦略の説明
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 戦略の概要

「PseudoIndex」戦略は、Binance 取引所で取引されている2つの主要な暗号通貨、具体的には Ethereum と Bitcoin の価格比率から合成インデックスを作成するように設計されています。この戦略は、価格変動に基づいてリアルタイムのインデックスを計算することで、これらの暗号通貨の相対的なパフォーマンスを監視します。

![schema](schema.png)

## 戦略の詳細

### コンポーネント

- **データソース**: Binance から ETHUSDT と BTCUSDT の[リアルタイム価格](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)データを使用します。
- **価格計算**:
  - ETHUSDT と BTCUSDT の両方の[終値](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)を追跡します。
  - これらの価格の比率を計算して合成インデックスを形成し、Bitcoin に対する Ethereum の相対的なパフォーマンスを表します。

### インデックスの計算

- **ローソク足の形成**: ETH と BTC の両方に[5分間の時間枠](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)を使用して、短期的な価格変動を捉えます。
- **比率の計算**: インデックスは ETH の価格を BTC の価格で[割った](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/formula.html)値として計算され、Bitcoin に対する Ethereum の価値のトレンドを示します。

### 可視化

- **チャート表示**: 結果として得られるインデックスは視覚的な分析のために[チャート](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)にプロットされ、インデックスの動きに基づいてトレンドや潜在的な取引シグナルを特定するのに役立ちます。

## 実装の詳細

- **プラットフォーム**: リアルタイムデータの取得と処理のための高度な機能を持つ StockSharp プラットフォーム内に実装されています。
- **テクニカルインジケーター**: この戦略は、追加のテクニカルインジケーターを使用せずに基本的な価格情報に依存し、意思決定に価格比率を重視しています。

## 結論

「PseudoIndex」戦略は、2つの主要な暗号通貨のパフォーマンスを比較することで取引に新しいアプローチを提供し、トレーダーが Ethereum と Bitcoin の相対的な強さに基づいて市場センチメントを評価し、情報に基づいた意思決定を行えるようにします。これは、これらの知見を基に暗号通貨保有のヘッジや分散を検討するトレーダーにとって特に有用です。
