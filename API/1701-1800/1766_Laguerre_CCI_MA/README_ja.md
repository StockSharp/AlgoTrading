# Laguerre CCI MA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Laguerreフィルター、商品チャンネル指数（CCI）、指数移動平均を組み合わせた戦略。

## 概要
- Laguerreフィルターは0-1のスケールで買われすぎと売られすぎの極端値を強調します。
- CCIは同方向のモメンタムを確認します。
- EMAの傾きにより、優勢なトレンドに沿った取引をフィルタリングします。

## エントリールール
- Laguerre値が0、EMAが上昇中、CCIが負の`CciLevel`閾値を下回るときに**ロング**。
- Laguerre値が1、EMAが下降中、CCIが正の`CciLevel`閾値を上回るときに**ショート**。

## エグジットルール
- Laguerreが0.9を超えたときにロングポジションを決済。
- Laguerreが0.1を下回ったときにショートポジションを決済。

## パラメーター
- `LagGamma` – Laguerreフィルターのガンマ値。
- `CciPeriod` – CCI計算の期間。
- `CciLevel` – エントリーに使用するCCIの絶対レベル。
- `MaPeriod` – 移動平均の期間。
- `TakeProfit` – 絶対価格単位でのテイクプロフィット（オプション）。
- `StopLoss` – 絶対価格単位でのストップロス（オプション）。
- `CandleType` – インジケーターに使用するロウソク足の種類。

この戦略は完成したロウソク足のみを処理し、インジケーターにはStockSharpの高レベルAPIバインディングを使用します。
