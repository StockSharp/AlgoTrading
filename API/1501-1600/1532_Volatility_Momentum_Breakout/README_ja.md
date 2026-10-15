# ボラティリティ・モメンタム・ブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ATRベースのブレイクアウトレベルをEMAトレンドフィルターとRSIモメンタムと組み合わせ、強いトレンドを捉えます。

## 詳細

- **エントリー条件**: EMAとRSIの確認を伴い、価格がATRブレイクアウトレベルを上回る/下回る終値をつけること
- **ロング/ショート**: 両方
- **エグジット条件**: ATRベースのストップロスと1:2リスク・リワードのテイクプロフィット
- **ストップ**: ATR
- **デフォルト値**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `Lookback` = 20
  - `EmaPeriod` = 50
  - `RsiPeriod` = 14
  - `RsiLongThreshold` = 50
  - `RsiShortThreshold` = 50
  - `RiskReward` = 2
  - `AtrStopMultiplier` = 1
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: ATR, EMA, RSI, Highest, Lowest
  - ストップ: ATR
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
