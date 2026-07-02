# StockSharp Strategy Designer における日付と時刻の処理例
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

StockSharp Strategy Designer のこの例は、日付と時刻の処理を取引戦略に統合した高度な設定を示しています。この戦略は時間固有の条件を使用して、ローソク足データと時刻に基づいた取引判断を行います。時間に敏感な取引シナリオの実践的な例として最適です。

![schema](schema.png)

## スキーマの説明

JSON ファイルで提示されるスキーマは、取引アクションをトリガーするために時間ベースのデータを処理するさまざまなノード間の複雑なやり取りを概説しています。

1. **TimeFrameCandle ノード**: 指定した時間枠の[ローソク足データ](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)を処理します。歴史的な価格変動を基に将来のトレンドを予測する戦略に不可欠です。

2. **OpenTime と CloseTime ノード**: ローソク足データから開始時刻と終了時刻を[抽出](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)します。取引条件を評価する特定の時間帯を決定するために重要です。

3. **比較ノード（Equals、Greater Than）**: ローソク足データから抽出した現在時刻と特定の時刻（14:00:00 や 15:00:00 など）を[比較](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)します。この設定により、指定した時刻に一致するかどうかに基づいて戦略を有効化または無効化できます。

4. **チャートパネルノード**: 取引データとインジケーターをわかりやすい形式で表示する[可視化コンポーネント](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)を実装し、リアルタイムの意思決定と戦略調整を支援します。

5. **取引ノード（買い、売り）**: 特定の時間条件が満たされたときに有効化され、比較結果と戦略内に定義された取引ロジックに基づいて[買い・売り注文](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)を実行できます。

## ワークフロー

- **TimeFrameCandle ノード**が定期的な間隔でローソク足データを収集・処理します。
- **OpenTime と CloseTime ノード**がこのデータを解析して特定の時刻ポイントを抽出します。
- **比較ノード**がこれらの時刻を事前定義した値（エントリー条件に14:00:00、エグジット条件に15:00:00など）と照合します。
- 条件が満たされると（例：現在時刻が14:00:00と一致）、取引ノード（買いまたは売り）が有効化されて、戦略のロジックに基づいて取引を実行します。
- **チャートパネルノード**がこれらの取引とローソク足データを視覚的に表示し、戦略の運用状況と市場状況を明確に示します。

## 実際の応用

この設定は、特定の時刻に取引を実行する必要がある戦略に特に有用です。例えば：
- **オープニングレンジ・ブレイクアウト**: 市場セッションの開始時に取引を配置する戦略。
- **クロージング・オークション戦略**: 取引セッションの終了時に発生する価格変動と流動性の変化を狙う戦略。

## 結論

StockSharp Strategy Designer のこの例は、事前定義した時刻に自動的に取引を実行できる時間敏感型取引戦略を開発するための堅牢なフレームワークを示しています。これは、トレーダーが Strategy Designer の機能を活用して、リアルタイムの市場データと特定の時間条件に動的に対応する複雑なルールベースの取引戦略を作成する方法の優れたデモンストレーションです。
