# Turtle Trader 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Turtle TraderはドンチャンチャネルとATRベースの資金管理を使用したクラシックなTurtleブレイクアウトシステムに従う。価格が直近高値を上抜けると買い、直近安値を下抜けると売る。価格が有利な方向に動くにつれてピラミッディングで勝ち筋のポジションを積み増す。

## 詳細

- **エントリー条件**: `S1`または`S2`の直近高値/安値のブレイクアウト
- **ロング/ショート**: 両方
- **エグジット条件**: 逆方向のブレイクアウトまたはATRストップ
- **ストップ**: ATRベース
- **デフォルト値**:
  - `RiskPercent` = 1
  - `AtrPeriod` = 20
  - `StopMultiplier` = 1.5
  - `PyramidProfit` = 0.5
  - `S1Long` = 20
  - `S2Long` = 55
  - `S1LongExit` = 10
  - `S2LongExit` = 20
  - `S1Short` = 15
  - `S2Short` = 55
  - `S1ShortExit` = 7
  - `S2ShortExit` = 20
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: ATR, Highest, Lowest
  - ストップ: ATR
  - 複雑さ: 中級
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
