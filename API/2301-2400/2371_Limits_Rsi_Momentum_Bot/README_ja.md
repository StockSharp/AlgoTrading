# Limits RSI Momentumボット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は相対力指数（RSI）とモメンタムインジケーターに基づいて指値注文を出します。市場注文の代わりに指値注文を使用することで、割引価格で買い、プレミアム価格で売ることを目指します。

## 取引ルール
- 指定された時間帯のみ動作します。
- 完成した各ローソク足でRSIとモメンタムの値を計算します。
- RSIとモメンタムの両方が買いの閾値を下回っているとき、ローソク足の始値を下回る位置に**買い指値注文**を出します。
- RSIとモメンタムの両方が売りの閾値を上回っているとき、ローソク足の始値を上回る位置に**売り指値注文**を出します。
- ポジションが建てられると、反対方向の指値注文がキャンセルされます。
- ストップロスとテイクプロフィットは`StartProtection`によって自動的に管理されます。

## パラメーター
- `Volume` – 注文数量。
- `LimitOrderDistance` – 指値注文を出すためのローソク足始値からの価格ステップ距離。
- `TakeProfit` – 価格ステップでの利益目標。
- `StopLoss` – 価格ステップでの損失限界。
- `RsiPeriod` – RSI計算の期間。
- `RsiBuyRestrict` / `RsiSellRestrict` – ロングまたはショートエントリーを許可するRSI閾値。
- `MomentumPeriod` – モメンタム計算の期間。
- `MomentumBuyRestrict` / `MomentumSellRestrict` – ロングまたはショートエントリーのモメンタム閾値。
- `StartTime` / `EndTime` – 取引セッションの境界。
- `CandleType` – インジケーター計算に使用するローソク足の間隔。

## 注意事項
この戦略はMQL4スクリプト「The Limits Bot with RSI & Momentum」から変換されており、StockSharpの高レベルAPIを使用しています。
