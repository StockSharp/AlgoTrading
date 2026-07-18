# 最適化 Heikin Ashi 売買オプション戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Heikin Ashi ローソク足は価格データを滑らかにし、トレンド方向を強調します。この戦略は一度に一方向のみ取引します。ユーザー定義の日付範囲内で、緑のローソク足ではロング、赤のローソク足ではショートを取引します。オプションのストップロスとテイクプロフィットレベルによるリスク管理が可能です。

## 詳細

- **エントリー条件**: Heikin Ashi ローソク足の色の変化。
- **ロング/ショート**: 設定可能。
- **エグジット条件**: 逆シグナルまたはストップレベル。
- **ストップ**: オプション、パーセントベース。
- **デフォルト値**:
  - `CandleType` = 1 day
  - `StartDate` = 2023-01-01
  - `EndDate` = 2024-01-01
  - `TradeType` = BuyOnly
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 設定可能
  - インジケーター: Heikin-Ashi
  - ストップ: オプション
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: 日付範囲
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

