# Turtle Trading システム
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ドンチャンチャネルのブレイクアウトとATRベースのリスク管理を使用したクラシックなTurtle Tradingシステム。

## 詳細

- **エントリー条件**: ドンチャンチャネルの上限/下限バンドのブレイクアウト
- **ロング/ショート**: 両方
- **エグジット条件**: より短いドンチャンチャネルのクロスまたはトレーリングストップ
- **ストップ**: ATRベースの初期ストップとトレーリングストップ
- **デフォルト値**:
  - `EntryLength` = 20
  - `ExitLength` = 10
  - `EntryLengthMode2` = 55
  - `ExitLengthMode2` = 20
  - `AtrPeriod` = 14
  - `RiskPerTrade` = 0.02
  - `InitialStopAtrMultiple` = 2
  - `PyramidAtrMultiple` = 0.5
  - `MaxUnits` = 4
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: DonchianChannels, ATR
  - ストップ: ATR
  - 複雑さ: 上級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
