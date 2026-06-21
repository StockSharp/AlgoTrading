# TradingViewTo 動的アラート付き戦略テンプレート
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIレベルに基づいてポジションを開設し、パーセンテージベースのストップロスとテイクプロフィットでトレードを管理するテンプレート戦略。

## 詳細
- **エントリー条件**:
  - **ロング**: RSI > `UpperLevel`
  - **ショート**: RSI < `LowerLevel`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ストップロスまたはテイクプロフィット
- **ストップ**: パーセンテージストップロスとテイクプロフィット
- **デフォルト値**:
  - `RsiLength` = 14
  - `UpperLevel` = 60
  - `LowerLevel` = 40
  - `StopLossPct` = 2m
  - `TakeProfitPct` = 4m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: 両方
  - インジケーター: RSI
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
