# Waindrops Makit0 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

カスタム期間の2つの半分のVWAPを比較するシンプルな戦略。

## 詳細

- **エントリー条件**: 右半分のVWAPと左半分のVWAPの比較。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `PeriodMinutes` = 60
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: 出来高
  - 方向: 両方
  - インジケーター: VWAP
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (1m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 低
