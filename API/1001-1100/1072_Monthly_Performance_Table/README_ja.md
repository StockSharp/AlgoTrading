# 月次パフォーマンステーブル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ADXが+DIと-DIの間にあり、ADXとの両差分が設定可能な閾値を超えたときに取引します。

## 詳細

- **エントリー条件**:
  - ロング：|+DI - ADX| ≥ `LongDifference` かつ |-DI - ADX| ≥ `LongDifference` で、ADXが+DIと-DIの間にある場合。
  - ショート：|+DI - ADX| ≥ `ShortDifference` かつ |-DI - ADX| ≥ `ShortDifference` で、ADXが-DIと+DIの間にある場合。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `Length` = 14
  - `LongDifference` = 10
  - `ShortDifference` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ADX, DMI
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
