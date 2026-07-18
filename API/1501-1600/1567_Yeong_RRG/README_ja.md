# Yeong RRG 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

正規化された相対強度とモメンタム比率（RRG）に基づく戦略です。

JDK RS と JDK RoC の両方が 100 を超えたときにロングエントリーし、両方が 100 を下回ったときにエグジットします。

## 詳細

- **エントリー条件**: JDK RS と JDK RoC が 100 超。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: JDK RS と JDK RoC が 100 未満。
- **ストップ**: なし。
- **デフォルト値**:
  - `Length` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: Relative Strength
  - 方向: Long
  - インジケーター: SMA, ROC, StandardDeviation
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

