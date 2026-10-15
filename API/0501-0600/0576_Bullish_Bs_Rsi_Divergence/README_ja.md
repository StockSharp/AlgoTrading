# 強気B's RSIダイバージェンス
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIとピボットポイントを使って通常および隠れた強気ダイバージェンスを検出します。ダイバージェンス発生時にロングトレードをオープンし、弱気シグナル、RSIターゲット到達、またはトレーリングストップでクローズします。

## 詳細

- **エントリー条件**:
  - **ロング**: 通常または隠れた強気RSIダイバージェンス。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 弱気ダイバージェンス、RSIがターゲットを上抜け、またはトレーリングストップ。
- **ストップ**: ATRまたは割合に基づくオプションのトレーリングストップ。
- **デフォルト値**:
  - `RsiPeriod` = 9
  - `PivotLookbackRight` = 3
  - `PivotLookbackLeft` = 1
  - `TakeProfitRsiLevel` = 80
  - `RangeUpper` = 60
  - `RangeLower` = 5
  - `StopType` = None
  - `StopLoss` = 5
  - `AtrLength` = 14
  - `AtrMultiplier` = 3.5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: ダイバージェンス
  - 方向: ロング
  - インジケーター: RSI, ATR
  - ストップ: オプションのトレーリングストップ
  - 複雑さ: 上級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい
  - リスクレベル: 中
