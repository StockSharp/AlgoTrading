# VWAP Pro V21 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、速いEMAと遅いEMAをVWAPおよびATRベースのリスク管理と組み合わせます。上位時間軸のEMAフィルター（1時間、長さ50）がトレンドを確認します。価格がトレンドと一致したときに取引が開き、ATRベースの利益確定または損切りレベルで閉じます。

## 詳細

- **エントリー条件**: 価格が速いEMA、VWAP、トレンドフィルターより上/下にある。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ATR利益確定または損切り。
- **ストップ**: あり。
- **デフォルト値**:
  - `EmaFastPeriod` = 9
  - `EmaSlowPeriod` = 21
  - `AtrPeriod` = 14
  - `TakeProfitAtrMultiplier` = 0.7
  - `StopLossAtrMultiplier` = 1.4
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: EMA, VWAP, ATR
  - ストップ: あり
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
