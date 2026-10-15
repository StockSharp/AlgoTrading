# Omega Galsky 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ブレークイーブン・ストップロジックを備えた EMA クロスオーバー戦略。

## 詳細

- **エントリー条件**: 速いEMAが遅いEMAをクロスし、EMA89による価格確認がある。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ストップロス、テイクプロフィット、または逆シグナル。
- **ストップ**: はい。
- **デフォルト値**:
  - `Ema8Period` = 8
  - `Ema21Period` = 21
  - `Ema89Period` = 89
  - `FixedRiskReward` = 1.0m
  - `SlPercentage` = 0.001m
  - `TpPercentage` = 0.0025m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
