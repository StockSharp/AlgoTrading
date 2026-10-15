# Uptrick X PineIndicators: Z-Score Flow戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Z-Score、EMA、RSIフィルターを使用したトレンドフォロー戦略。

## 詳細

- **エントリー条件**: Z-ScoreがトレンドおよびRSI確認とともに買い/売り閾値をクロス
- **ロング/ショート**: 両方
- **エグジット条件**: 選択モードに基づく逆シグナル
- **ストップ**: いいえ
- **デフォルト値**:
  - `ZScorePeriod` = 100
  - `EmaTrendLen` = 50
  - `RsiLen` = 14
  - `RsiEmaLen` = 8
  - `ZBuyLevel` = -2
  - `ZSellLevel` = 2
  - `CooldownBars` = 10
  - `SlopeIndex` = 30
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA, EMA, RSI, StandardDeviation
  - ストップ: いいえ
  - 複雑さ: 上級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
