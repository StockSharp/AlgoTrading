# マルチインジケーター・スイング
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Parabolic SAR、SuperTrend、ADX、およびボリュームデルタ確認を組み合わせたスイング戦略。

## 詳細

- **エントリー条件**: 有効なすべてのインジケーターが一致する。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のシグナルまたはストップロス/テイクプロフィットへの到達。
- **ストップ**: オプションのパーセンテージベースのレベル。
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromMinutes(2)
  - `PsarStart` = 0.02m
  - `PsarIncrement` = 0.02m
  - `PsarMaximum` = 0.2m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `DeltaLength` = 14
  - `DeltaSmooth` = 3
  - `DeltaThreshold` = 0.5m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: PSAR, SuperTrend, ADX, Volume
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (2m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
