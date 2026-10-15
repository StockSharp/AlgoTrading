# Geedo戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

特定の時間帯に2本の過去のバーの始値を比較する時間ベースの戦略です。古いバーが最近のバーより閾値分高い場合、ショート取引が開始されます。最近のバーが古いバーより高い場合、ロング取引が開始されます。各ポジションは固定のストップロスとテイクプロフィットを使用し、最大保有時間後にクローズされます。

## 詳細

- **エントリー条件**: `TradeTime` に `T1` と `T2` 本前のバーの始値を比較する。`Open[T1] - Open[T2]` が `DeltaShort` を超えた場合は売り；`Open[T2] - Open[T1]` が `DeltaLong` を超えた場合は買い。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ストップロス、テイクプロフィット、またはエントリーから `MaxOpenTime` 時間後。
- **ストップ**: ポイント単位の固定ストップロスとテイクプロフィット。
- **デフォルト値**:
  - `TakeProfitLong` = 39
  - `StopLossLong` = 147
  - `TakeProfitShort` = 15
  - `StopLossShort` = 6000
  - `TradeTime` = 18
  - `T1` = 6
  - `T2` = 2
  - `DeltaLong` = 6
  - `DeltaShort` = 21
  - `Volume` = 0.01
  - `MaxOpenTime` = 504
- **フィルター**:
  - カテゴリ: 時間ベース
  - 方向: 両方
  - インジケーター: なし
  - ストップ: 固定
  - 複雑さ: 初心者
  - 時間軸: イントラデイ (1h)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
