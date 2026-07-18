# Ehlers SwamiCharts RSI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

期間2〜48のRSI値を平均してカラーマップを構築します。平均色が緑のときにロング、赤のときにショートを取ります。

## 詳細

- **エントリー条件**: 平均色が緑（`Color1Avg` == 255 かつ `Color2Avg` > `LongColor`）でロング；赤（`Color1Avg` > `ShortColor` かつ `Color2Avg` == 255）でショート。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `LongColor` = 50
  - `ShortColor` = 50
  - `CandleType` = 5 minutes
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: RSI
  - ストップ: いいえ
  - 複雑さ: 上級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
