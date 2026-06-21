# カスタム買い BID戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Custom Buy BID戦略はSupertrendインジケーターを使用して強気のリバーサルを識別する。価格がSupertrend線を上抜けした際にロングポジションを開き、リスク管理のために設定可能な利益目標と損失目標を適用する。

## 詳細

- **エントリー条件**: 価格がSupertrendを上抜ける。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: テイクプロフィットまたはストップロス。
- **ストップ**: あり。
- **デフォルト値**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `TakeProfitPercent` = 5m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StartDate` = 2018-09-01
  - `EndDate` = 9999-01-01
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングのみ
  - インジケーター: Supertrend
  - ストップ: あり
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
