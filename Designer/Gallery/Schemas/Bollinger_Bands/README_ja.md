# Bollinger Bands 戦略の説明
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 戦略の概要

「Bollinger Bands」戦略は [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) 向けに設計されており、Bollinger Bands を活用してボラティリティパターンから利益を得ることに焦点を当てています。この戦略は価格がバンドを横切る動きを検出し、市場への参入および退出ポイントを決定します。

![schema](schema.png)

## 戦略の詳細

### コンポーネント

1. **ローソク足の生成**：5分間の時間足を使用して[ローソク足](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)を生成し、各ローソク足の終値でトリガーされる分析を行います。
2. **Bollinger Bands インジケーター**：期間32、標準偏差乗数2.0を使用して [Bollinger Bands](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) の上下バンドを計算します。
3. **取引シグナル**：
   - **買いシグナル**：ローソク足の[安値](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)が Bollinger Bands の下限バンドを下方に[クロス](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)したとき、売られすぎの状態を示す買いシグナルが生成されます。
   - **売りシグナル**：ローソク足の[高値](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)が Bollinger Bands の上限バンドを上方に[クロス](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)したとき、買われすぎの状態を示す売りシグナルがトリガーされます。

### 取引実行

- **注文タイプ**：迅速な実行を確保するために、エントリーとエグジット両方で[成行注文](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)が使用されます。
- **ポジション管理**：クロスシグナルに基づいてポジションを開き、逆方向のクロスまたは事前定義のストップロスもしくはテイクプロフィット条件に基づいて決済します。

### リスク管理

- **ストップロスとテイクプロフィット**：設定可能なパラメーターにより、効果的なリスク管理のための固定または割合ベースの[ストップロスとテイクプロフィット](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)水準を設定できます。
- **資金管理**：戦略には、利用可能な口座残高とリスク水準に基づいて取引規模を調整するパラメーターが含まれています。

## 結論

「Bollinger Bands」戦略は、ボラティリティと市場状況に基づいた取引への体系的なアプローチを提供し、StockSharp プラットフォーム内で堅牢な自動取引システムを求めるトレーダーに適しています。テクニカルインジケーターと精密な取引実行ルールを組み合わせ、様々な市場環境での取引パフォーマンスを向上させます。
