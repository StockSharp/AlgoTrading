# Bullish8020 戦略の説明
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 戦略の概要

「Bullish8020」戦略は [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) 向けに開発されており、特定の強気ローソク足パターンを高精度で活用することを目的としています。この戦略は、ボリュームと価格行動を組み合わせた独自のパターン分析を使用して、強気のセンチメントが強い市場機会を識別することを目指しています。

![schema](schema.png)

## 戦略の詳細

### パターン検出：Bullish8020

- **説明**：この戦略は、[始値](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)が終値より低く、実体のサイズが両方の影の合計の4倍以上である強気シナリオを検出し、強い買い圧力を示します。
- **ローソク足パターン**：'Bullish8020' は `(O < C) && (B >= 4*(BS+TS))` を確認します。ここで `O` は始値、`C` は終値、`B` は実体サイズ、`BS` は下影、`TS` は上影です。

### 取引実行

- **注文タイプ**：成行[注文](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)
- **エントリー**：潜在的な上昇動向を示す 'Bullish8020' [パターン](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)が確認されたときに買います。
- **エグジット戦略**：
  - **ストップロス**：潜在的な損失を制限するため、エントリーポイントより0.5%下に設定。
  - **市場条件**：パターン認識への迅速な対応を確保するため、現在の市場価格で取引を実行します。

### リスク管理

- **ポジションサイジング**：戦略は現在の市場条件とトレーダーのリスクプロファイルに基づく動的なサイジングを使用します。
- **ストップロス戦略**：予期しない市場反転に備えて厳格な[ストップロス](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)を実装しています。

## 実装の詳細

- **プラットフォーム**：リアルタイムデータ処理と注文実行のための強力な API を活用した StockSharp プラットフォームで実装されています。
- **使用インジケーター**：取引シグナルの精度を高めるために、ローソク足パターン認識とボリューム分析を組み合わせています。

## 結論

「Bullish8020」戦略は、市場における特定の強気パターンを活用するためのトレーダーへの強力なツールを提供します。投資を守るための厳格なリスク管理プロトコルを採用しながら、強い強気セットアップからの利益を最大化するよう設計されています。
