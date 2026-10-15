# VoVix実験戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

短期ATRと長期ATRの比率を分析する戦略です。この比率のZ-スコアが急上昇してローカル最大値に達すると、ローソク足の方向にエントリーします。Z-スコアがエグジット閾値を下回ると、ポジションを決済します。

## 詳細

- **エントリー条件**: VoVixのZ-スコアが`EntryZ`を上回り、ローカル最大値にある
- **ロング/ショート**: 両方
- **エグジット条件**: VoVixのZ-スコアが`ExitZ`を下回る
- **ストップ**: いいえ
- **デフォルト値**:
  - `FastAtrLength` = 13
  - `SlowAtrLength` = 26
  - `ZScoreWindow` = 81
  - `EntryZ` = 1.0
  - `ExitZ` = 1.4
  - `LocalMaxWindow` = 6
  - `SuperZ` = 2.0
  - `MinVolume` = 1
  - `MaxVolume` = 2
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: 両方
  - インジケーター: ATR, Highest, SMA, StdDev
  - ストップ: いいえ
  - 複雑さ: 上級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
