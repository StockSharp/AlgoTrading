# StockSharp Strategy Designer における Three White Soldiers パターン検出の例
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

この例は、「Three White Soldiers」ローソク足パターンを活用した取引戦略を StockSharp Strategy Designer で実装する方法を示しています。このパターンは多くの場合、強気反転シグナルとして解釈され、モメンタムの転換を活用したいトレーダーにとって重要な意味を持ちます。JSON スキーマで説明されているセットアップには、このパターンの検出とその出現に基づいた取引の開始が含まれます。

![schema](schema.png)

## スキーマの説明

スキーマは、「Three White Soldiers」[パターン](https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pattern.html)を検出し、それに応じて取引を実行するために設計された複雑なワークフローを概説しています。主なコンポーネントとその役割は以下の通りです：

1. **Security ノード**：戦略が適用される[証券](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)を指定します。主要なデータ入力ソースとして機能し、後続の分析に必要な市場データを提供します。

2. **TimeFrameCandle ノード**：指定された証券の[ローソク足データ](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)を生成します。このノードは、入力される市場データをパターン検出アルゴリズムが分析できる使用可能な形式（ローソク足）に処理するため、重要な役割を担います。

3. **パターン検出ノード**：[インジケーター](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)を介して「Three White Soldiers」[パターン](https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pattern.html)を検出するために専用に設定されています。このノードはローソク足データを分析し、パターンが識別されたときにアクションをトリガーします。

4. **Chart Panel ノード**：ローソク足パターンや戦略によって実行された取引を含む取引データを視覚化します。この[コンポーネント](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)は、戦略のパフォーマンスを監視し、パターンが取引判断にどう影響するかを理解するのに役立ちます。

5. **取引ノード（買い、売り）**：これらの[ノード](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)は、パターンが検出されたときに取引を実行するよう設定されています。アクションは、市場状況やその他のテクニカルインジケーターなど、戦略内に設定された追加条件に基づいて異なる場合があります。

## ワークフロー

- **Security ノード**が市場データを **TimeFrameCandle ノード**に供給し、そこでデータがローソク足に変換されます。
- これらのローソク足は次に**パターン検出ノード**に渡され、「Three White Soldiers」パターンを識別するよう設定されています。
- パターンが検出されると、ノードは戦略のデザインに応じて買い注文または売り注文を実行するために1つ以上の**取引ノード**をトリガーできます。
- **Chart Panel ノード**はローソク足と実行された取引のリアルタイム可視化を提供し、戦略の有効性の評価や必要に応じた調整を支援します。

## 実際の応用

このセットアップは、パターンを早期に認識することで大きな利益につながりうるモメンタムベース戦略を専門とするトレーダーに特に有用です。「Three White Soldiers」パターンは強気反転の強力な指標であり、この戦略は以下に適しています：
- モメンタムの転換が急激で明確な市場でのスイングトレード。
- 高ボラティリティ市場でのデイトレード。トレンド転換を早期に認識することで利益を生む取引につながります。

## 結論

StockSharp Strategy Designer のこの例は、アルゴリズム取引の文脈におけるローソク足パターン検出の高度な活用を示しています。「Three White Soldiers」などのパターン検出を自動化することで、トレーダーは歴史的な価格パターンの予測力を活かして、より効果的に市場に参入できます。詳細な可視化とリアルタイムデータ処理も、観察された市場状況と結果に基づいて戦略を改良するのに役立ちます。
