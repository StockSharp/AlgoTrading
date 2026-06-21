# Supertrend 目標・ストップロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格がSupertrendラインを上抜けで買い、下抜けで売る戦略。固定パーセンテージの目標値とストップロスでポジションをクローズします。

## 詳細

- **エントリー条件**: 価格がSupertrendを交差。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 目標またはストップロスのパーセンテージ。
- **ストップ**: はい、固定パーセンテージ。
- **デフォルト値**:
  - `Period` = 14
  - `Multiplier` = 3m
  - `TargetPct` = 0.01m
  - `StopPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ATR, Supertrend
  - ストップ: 固定
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
