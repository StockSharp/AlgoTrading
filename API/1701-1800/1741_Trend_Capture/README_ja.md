# トレンド・キャプチャー
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Parabolic SARとADXフィルターを組み合わせたトレンドフォロー戦略です。価格がSAR値を上回って終値を付け、ADXが閾値を下回っているときにロング取引が発生し、新興トレンドを示します。ショート取引は逆の条件で開かれます。

## 詳細

- **エントリー条件**: Parabolic SARより価格が上/下、ADXが`AdxLevel`未満。
- **ロング/ショート**: 両方。
- **エグジット条件**: ストップロス、テイクプロフィット、または逆シグナル。
- **ストップ**: 固定ストップロス、テイクプロフィット、ブレークイーブン調整。
- **デフォルト値**:
  - `SarStep` = 0.02
  - `SarMax` = 0.2
  - `AdxPeriod` = 14
  - `AdxLevel` = 20
  - `StopLoss` = 1800 ポイント
  - `TakeProfit` = 500 ポイント
  - `BreakEven` = 50 ポイント
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Parabolic SAR, ADX
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
