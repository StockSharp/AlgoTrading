# NY 初回ローソク足ブレイクアンドリテスト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ニューヨークセッションの最初のローソク足のブレイクアウトをリテスト確認で取引する戦略。ストップ配置とリスクリワードターゲットにATRを使用し、オプションのEMAトレンドフィルターとトレーリングストップを備える。

## 詳細

- **エントリー条件**: 最初のセッションローソク足の高値または安値のブレイクに続き、`RetestThreshold` ATR以内のリテスト。
- **ロング/ショート**: 両方。
- **エグジット条件**: ATRベースのストップと`RewardRiskRatio`ターゲット。オプションのトレーリングストップ。
- **ストップ**: `AtrMultiplier` * ATR。
- **デフォルト値**:
  - `NyStartHour` = 9
  - `NyStartMinute` = 30
  - `SessionLength` = 4
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.2
  - `RewardRiskRatio` = 1.5
  - `MinBreakSize` = 0.15
  - `RetestThreshold` = 0.25
  - `UseEmaFilter` = true
  - `EmaLength` = 13
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: ATR, EMA
  - ストップ: ATR
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: はい
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
