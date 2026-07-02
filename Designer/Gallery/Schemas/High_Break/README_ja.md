# StockSharp Strategy Designer における High Break 戦略の例
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

提供された JSON スキーマに示された「High Break」戦略は、StockSharp Strategy Designer を使用して、価格動向と時間枠に関連する特定の条件に基づいて取引を実行するように設計されています。この例は、証券の価格が一定期間の所定の高値を上抜けたときに潜在的な買いの機会を識別する取引戦略をどのように設定するかを示しています。

![schema](schema.png)

## スキーマの説明

スキーマは、リアルタイム市場データを取得、分析、および対応するために設計された一連の相互接続されたコンポーネントを概説しています：

1. **Security ノード**：戦略が適用される[証券](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)（例：株式、先物）を指定する基盤として機能します。このノードは戦略のデータ入力を決定するため、重要です。

2. **TimeFrameCandle ノード**：入力される市場データを処理し、指定された時間枠に基づいて[ローソク足に](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)まとめます。このノードは取引判断のために歴史的な価格分析に依存する戦略に不可欠です。

3. **Highest ノード**：ローソク足データを分析し、指定された時間（例：60分）にわたって[達成された最高値を決定](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)します。この値は重要な価格ブレイクを識別するためのベンチマークを設定します。

4. **比較ノード**：現在の価格を Highest ノードで決定した歴史的高値と[比較](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)します。現在の価格がこの高値を超えると、潜在的な取引シグナルをトリガーします。

5. **Chart Panel ノード**：価格データと戦略のアクションを[視覚化](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)し、戦略の動作のグラフ表現を提供して監視と調整を支援します。

6. **取引実行ノード（買い/売り）**：戦略の条件が満たされたときに[取引を実行](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)する役割を担います。例えば、価格が歴史的高値を上回ったときに買い注文が実行される場合があります。

## ワークフロー

- **Security ノード**が市場データを **TimeFrameCandle ノード**に供給し、構造化された時間ベースのローソク足データセットを作成します。
- **Highest ノード**がこれらのローソク足から定義された期間にわたる最高値を計算します。
- **比較ノード**が現在の価格をこの高値と継続的に比較します。現在の価格が歴史的高値を超えると、強気のブレイクアウトを示し、買いシグナルをトリガーする可能性があります。
- **Chart Panel ノード**がリアルタイム可視化を提供し、戦略のパフォーマンスと市場状況に関する即時の視覚的フィードバックを可能にします。
- 買い条件が満たされると、**取引実行ノード**（買い）が取引を開始し、予想される上昇モメンタムを活用します。

## 実際の応用

この設定は、特定のしきい値を超える価格動向を認識し対応することで利益を生む可能性があるブレイクアウト戦略を専門とするトレーダーに特に有用です。このような戦略は、価格ブレイクアウトが強いトレンドを示す可能性がある変動性の高い市場で人気があります。

## 結論

StockSharp Strategy Designer 内の「High Break」戦略の例は、識別された価格動向に基づいて取引判断を自動化するための市場データの高度な活用を示しています。リアルタイムデータ処理と視覚化ツールを活用することで、戦略はトレーダーが価格ブレイクアウトによって提示される市場機会を効率的に活用するのを支援します。この例は、動的な取引戦略を開発する際の StockSharp プラットフォームの力を示すだけでなく、個々の取引要件と市場条件に基づいたさらなるカスタマイズと最適化の基盤となります。
