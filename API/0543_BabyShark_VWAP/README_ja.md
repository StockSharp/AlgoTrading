# BabyShark VWAP 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は出来高加重平均価格（VWAP）バンドとOBVベースのRSIフィルターを組み合わせます。価格が下方偏差バンドを下回り、RSIが売られすぎを示したときにロング取引が発生します。価格が上方バンドを上回り、RSIが買われすぎのときにショート取引が発火します。

ストップは小さなパーセンテージ損失を使用し、ポジションは再エントリー前にクールダウン期間を待ちます。

## 詳細

- **エントリー条件**: 価格が偏差バンドを越えてRSIで確認。
- **ロング/ショート**: 両方向。
- **エグジット条件**: VWAPへの回帰またはストップロス。
- **ストップ**: はい。
- **デフォルト値**:
  - `Length` = 60
  - `RsiLength` = 5
  - `HigherLevel` = 70
  - `LowerLevel` = 30
  - `Cooldown` = 10
  - `StopLossPercent` = 0.6m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: VWAP, RSI, OBV
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
