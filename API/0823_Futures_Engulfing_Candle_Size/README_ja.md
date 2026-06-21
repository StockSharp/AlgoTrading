# 先物エングルフィングローソク足サイズ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

選択した時間帯内でローソク足のレンジがティック閾値を超えると、1日に1回取引します。方向はローソク足の実体に従い、テイクプロフィットとストップロスで決済します。

## 詳細

- **エントリー条件**: 取引セッション内のローソク足レンジ（ティック数）。
- **ロング/ショート**: 両方。
- **エグジット条件**: テイクプロフィットまたはストップロス。
- **ストップ**: テイクプロフィットとストップロス。
- **デフォルト値**:
  - `CandleType` = 1 minute
  - `CandleSizeThresholdTicks` = 25
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 40
  - `StartHour` = 7
  - `StartMinute` = 0
  - `EndHour` = 9
  - `EndMinute` = 15
- **フィルター**:
  - カテゴリ: パターン
  - 方向: 両方
  - インジケーター: Candlestick
  - Stops: はい
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
