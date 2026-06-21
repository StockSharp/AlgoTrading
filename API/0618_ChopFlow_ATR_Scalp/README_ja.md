# ChopFlow ATRスキャルプ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ChopFlow ATR Scalpは市場がチョッピーな状態から抜け出し、OBVがそのEMAを突破した時にエントリーします。エグジットはATRベースの対称的なストップとターゲットを使用します。

目的はトレンド形成の初期における素早い動きを捉えることです。

## 詳細

- **エントリー条件**: `Choppiness < ChopThreshold` かつ OBVがそのEMAの上/下にある。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ATRストップまたはテイクプロフィット距離。
- **ストップ**: はい。
- **デフォルト値**:
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `ChopLength` = 14
  - `ChopThreshold` = 60
  - `ObvEmaLength` = 10
  - `SessionInput` = "1700-1600"
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: スキャルピング
  - 方向: 両方
  - インジケーター: ATR, Choppiness, OBV
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
