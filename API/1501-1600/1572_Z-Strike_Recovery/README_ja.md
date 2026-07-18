# Z-Strike Recovery戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格変化のZ-Scoreが閾値を超えるとロングエントリーし、固定バー数後に決済する。

## 詳細

- **エントリー条件**: 価格変化のZ-Score > 閾値
- **ロング/ショート**: ロングのみ
- **エグジット条件**: 時間ベースの決済
- **ストップ**: なし
- **デフォルト値**:
  - `ZLength` = 16
  - `ZThreshold` = 1.3
  - `ExitPeriods` = 10
- **フィルター**:
  - カテゴリ: 統計的
  - 方向: ロング
  - インジケーター: SMA, StandardDeviation
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
