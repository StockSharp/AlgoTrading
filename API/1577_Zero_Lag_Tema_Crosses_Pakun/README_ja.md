# Zero-Lag TEMAクロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ゼロラグ3重EMAのクロスオーバーシステム。ポジションは直近の高値・安値をストップとして使用し、リスクリワード比に基づく目標を設定する。

## 詳細

- **エントリー条件**: 速いTEMAが遅いTEMAをクロス。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 直近の極値でのストップまたは比率によるターゲット。
- **ストップ**: あり。
- **デフォルト値**:
  - `Lookback` = 20
  - `FastPeriod` = 69
  - `SlowPeriod` = 130
  - `RiskReward` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: TEMA
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
