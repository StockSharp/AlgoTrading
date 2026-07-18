# 逆ケルトナーチャネル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格がケルトナーチャネルの外側から内側に再突入した際にエントリーし、反対のバンドを目標とする戦略。ADXフィルターはオプション。

価格が下限バンドを下から上に抜けた際にロングエントリーし、上限バンドまたはチャネル幅の半分のストップで決済する。ショートは対称的。ADXフィルターにより弱いトレンドまたは強いトレンドのみに取引を限定できる。

## 詳細

- **エントリー条件**: 価格がケルトナーの外側バンドからチャネル内へクロス、オプションのADXフィルター。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のバンドまたはストップ。
- **ストップ**: あり。
- **デフォルト値**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 2m
  - `StopLossFactor` = 0.5m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `UseAdxFilter` = true
  - `WeakTrendOnly` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: Keltner, ADX
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
