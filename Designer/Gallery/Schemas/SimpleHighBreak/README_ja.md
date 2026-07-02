# SimpleHighBreak 戦略の説明
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 戦略の概要

「SimpleHighBreak」戦略は、[StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) 内で事前に定義した高値を超えた価格ブレイクアウトを利用するように設計されています。この戦略は、価格が直近15期間の高値を上抜けする機会を特定することに焦点を当てており、上昇トレンドの継続可能性を示します。

![schema](schema.png)

## 戦略の詳細

### コンポーネント

- **ローソク足の形成**: 5分間の時間枠を使用して[ローソク足](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)を生成し、重要な価格変動を監視します。
- **高値インジケーター**: 直近15期間の[最高価格](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)を計算してブレイクアウトレベルを設定します。
- **ブレイクアウトの検出**: 現在価格が直近15期間の高値を[上抜ける](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)と、戦略が買い注文を発動します。

### 取引の実行

- **注文タイプ**: 成行[注文](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)。
- **エントリー**: 価格が15期間の高値を超えたときに買い注文を発注します。
- **エグジット戦略**: 設定した時間枠や反転パターンなど特定の条件に基づいてポジションをクローズし、戦略によって動的に管理されます。

### リスク管理

- **ポジションサイズ**: 事前定義したリスク管理ルールと現在の市場変動性に基づいてポジションサイズを調整します。
- **ストップロスとテイクプロフィット**: リスクを管理し利益を確保するために、エントリー直後に設定可能な[ストップロスとテイクプロフィット](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)レベルが設定されます。

## 実装の詳細

- **プラットフォーム**: StockSharp プラットフォーム内に実装されており、リアルタイムデータ処理と自動化された注文管理のための豊富な機能を活用しています。
- **インジケーター**: 主にエントリーポイントを決定するために、指定した期間数にわたる最高価格インジケーターを使用します。

## 結論

「SimpleHighBreak」戦略は、価格ブレイクアウト取引への簡潔かつ効果的なアプローチを提供しており、ボラティリティの高い市場で機会を探すトレーダーに最適です。技術的インジケーターと詳細なリスク管理を組み合わせ、リスクを最小化しながら潜在的なリターンを最大化します。
