# 安値ブレイク計算戦略の説明
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 戦略の概要

「安値ブレイク計算」戦略は、高値・安値インジケーターの組み合わせを使用して、市場における潜在的なブレイクアウトポイントを特定します。この戦略の目的は、価格が指定期間内に計算された安値を下回ったときに取引を実行し、潜在的な下降トレンドを狙うことです。

[![schema](schema.png)](schema_easter_egg.png)

## 戦略の詳細

### コンポーネント

- **ローソク足の形成**: 重要な市場の動きを捉えるため、1時間の時間枠で[ローソク足](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)を生成します。
- **高値・安値インジケーター**:
  - **Highest 25**: 過去25期間の[最高価格](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)を追跡します。
  - **Lowest 45**: 過去45期間の[最安値](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)を監視します。
- **計算ロジック**: 現在の価格をインジケーターから算出した高値・安値レベルと[比較](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)することで、取引実行ポイントを決定します。

### 取引の実行

- **エントリーシグナル**: 現在の価格が「Lowest 45」インジケーターで計算された最安値を[下回る]()と、[買い](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)注文が発動します。
- **エグジットシグナル**: 特定の計算パラメーターで定義された条件において、その後の価格行動が下降トレンドの継続を支持しない場合に、[売り](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)注文が発動します。

### 可視化

- **チャート表示**: 「Highest 25」と「Lowest 45」のインジケーター値が価格ローソク足とともに[チャート](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)にプロットされ、潜在的なブレイクアウトポイントを視覚的に表示します。

## 実装の詳細

- **プラットフォーム**: StockSharp プラットフォーム上に実装されており、リアルタイムデータ処理とインジケーター計算の機能を活用しています。
- **インジケーターの使用**: 高値・安値インジケーターを組み合わせて、戦略がブレイクアウトポイントを探す価格レンジを設定します。

## 結論

「安値ブレイク計算」戦略は、確立された高値や安値からの価格ブレイクアウトに基づく機会を探すトレーダー向けに設計されています。技術的インジケーターと高度な計算ロジックを組み合わせ、潜在的な市場の動きを特定して活用します。
