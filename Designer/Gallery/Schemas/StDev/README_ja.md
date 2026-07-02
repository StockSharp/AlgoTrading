# StDevStrategy の説明
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 戦略の概要

「StDevStrategy」は、[StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) 向けに、Standard Deviation インジケーターを使用して統計的な変動パターンを活用するよう設計されています。この戦略は、平均価格からの乖離に基づいて潜在的な取引機会を特定し、買われすぎまたは売られすぎの状態をシグナルとして活用します。

![schema](schema.png)

## 戦略の詳細

### コンポーネント

- **Standard Deviation インジケーター**: 短期・長期の変動性を捉えるために複数の期間を使用します。
  - **Std Dev 20**: [20期間](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)にわたる変動性を測定します。
  - **Lowest 15 と Highest 15**: ブレイクアウト条件を検出するために15期間の最低値と最高値を追跡します。
  - **Lowest 50**: 長期的な市場状況を評価するために長期の価格安値を捉えます。

### 取引の実行

- **注文タイプ**: シグナル変化への迅速な対応を確保するために[成行注文](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)を使用して取引を実行します。
- **エントリーとエグジット**:
  - **買い**: 価格行動が売られすぎ状態からの反発を示唆したときに発動します。
  - **売り**: 価格行動が買われすぎ状態からの潜在的な下落を示したときに開始します。
- **ポジション管理**: 市場の変動性とリスクパラメーターに基づいて調整する動的なポジションサイジング戦略を採用します。

### リスク管理

- **ストップロスとテイクプロフィット**:
  - リスクを最小化するために、エントリー価格の1%下に[ストップロス](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)を設定します。
  - [テイクプロフィット](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)は2%に設定し、利益を確保しながら潜在的な上昇を捉えます。

## 実装の詳細

- **プラットフォーム**: リアルタイムデータ分析と注文管理のための包括的なツールを活用して、StockSharp プラットフォーム内に実装されています。
- **テクニカルインジケーター**: 取引精度を高めるために、最高値・最安値の追跡とともに複数の Standard Deviation インスタンスを統合します。

## 結論

「StDevStrategy」は、テクニカル分析を好み、変動性に駆動される価格変動を捉えることに注力するトレーダー向けに設計されています。高度なインジケーターを使用してエントリー・エグジットポイントを効果的に管理する、構造化された取引アプローチを提供します。
