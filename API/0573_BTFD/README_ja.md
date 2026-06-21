# BTFD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

出来高とRSIに基づく押し目買い戦略で、5段階のテイクプロフィットと保護ストップを持ちます。

## 詳細

- **エントリー条件**: 出来高がSMAを上回り、RSIが売られすぎゾーンを下回る。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 5段階のテイクプロフィット目標またはストップロス。
- **ストップ**: あり。
- **デフォルト値**:
  - `VolumeLength` = 70
  - `VolumeMultiplier` = 2.5
  - `RsiLength` = 20
  - `RsiOversold` = 30
  - `Tp1` = 0.4
  - `Tp2` = 0.6
  - `Tp3` = 0.8
  - `Tp4` = 1.0
  - `Tp5` = 1.2
  - `Q1` = 20
  - `Q2` = 40
  - `Q3` = 60
  - `Q4` = 80
  - `Q5` = 100
  - `StopLossPercent` = 5
  - `CandleType` = TimeSpan.FromMinutes(3)
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: ロングのみ
  - インジケーター: RSI, SMA
  - ストップ: あり
  - 複雑さ: 基本
  - 時間軸: イントラデイ (3m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
