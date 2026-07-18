# Cryptorhythmsによる時系列ラグ低減フィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAラグ低減フィルターに基づく戦略。

アルゴリズムは価格とラグ調整済みEMAを比較し、クロスオーバーでトレードします。

## 詳細

- **エントリー条件**: 価格がラグ低減EMAを交差するとき。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対方向のクロスオーバー。
- **ストップ**: なし。
- **デフォルト値**:
  - `LagReduction` = 20m
  - `EmaLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
