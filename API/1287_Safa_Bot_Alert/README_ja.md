# Safa Bot Alert 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Safa Bot Alert 戦略は、短期 SMA と ADX フィルターを使用して価格クロスオーバーを取引します。価格が SMA を上抜け、かつトレンド強度が強いときにロングエントリーし、下抜け時にショートエントリーします。固定のテイクプロフィット、ストップロス、およびトレーリングストップでポジションを管理し、指定されたセッション時刻にすべてのトレードをクローズします。

## 詳細

- **エントリー条件**: 価格が SMA をクロスし、ADX > `AdxThreshold`。
- **ロング/ショート**: 両方。
- **エグジット条件**: テイクプロフィット、ストップロス、トレーリングストップ、またはセッションクローズ。
- **ストップ**: 固定およびトレーリング。
- **デフォルト値**:
  - `SmaLength` = 3
  - `TakeProfitPoints` = 80m
  - `StopLossPoints` = 35m
  - `TrailPoints` = 15m
  - `AdxLength` = 15
  - `AdxThreshold` = 15m
  - `SessionCloseHour` = 16
  - `SessionCloseMinute` = 0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SMA, ADX
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
