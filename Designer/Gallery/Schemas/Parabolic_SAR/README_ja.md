# Parabolic SAR 戦略の説明
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 戦略の概要

「Parabolic SAR」戦略は、[StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) 内でパラボリック・ストップ・アンド・リバース（SAR）インジケーターを使用して、トレンドの反転と継続パターンを捉えるように設計されています。この戦略は、価格と Parabolic SAR ポイントとの相対的な動きに基づいて明確なエントリー・エグジットシグナルを提供します。

![schema](schema.png)

## 戦略の詳細

### コンポーネント

- **ローソク足の形成**: 短期的な市場の動きを効果的に捉えるため、5分間の[時間枠](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)を使用して価格行動を分析します。
- **Parabolic SAR インジケーター**: 初期加速係数0.02、加速ステップ0.02、最大加速0.2で[設定](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)されています。これらの設定により、インジケーターが市場の変動性に適応できます。

### 取引の実行

- **エントリーシグナル**: 価格が Parabolic SAR ポイントを[上抜ける](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)と買いシグナルが生成され、潜在的な上昇トレンドを示します。
- **エグジットシグナル**: 価格が Parabolic SAR ポイントを[下回る](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)と売りシグナルが発行され、潜在的な下降トレンドを示唆します。

### 可視化

- **チャート表示**: Parabolic SAR ポイントが価格ローソク足とともに[チャート](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)にプロットされ、トレンドと潜在的な取引シグナルを視覚的に表示します。

## 実装の詳細

- **プラットフォーム**: StockSharp プラットフォーム上に実装されており、リアルタイムデータの取得、インジケーター計算、取引実行のための包括的な機能を活用しています。
- **インジケーターの適用**: Parabolic SAR は価格チャートに直接適用され、トレンド変化と取引設定の有効性を即座に視覚的に評価できます。

## 結論

「Parabolic SAR」戦略は、トレンドの反転パターンに基づいた正確で自動化された取引シグナルを必要とするトレーダーに最適です。Parabolic SAR の動的な性質を活用して適時のエントリーとエグジットを提供し、急速に動く市場での利益ポテンシャルを高めます。
