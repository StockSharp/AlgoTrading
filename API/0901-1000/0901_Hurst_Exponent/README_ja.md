# Hurst Exponent 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

平滑化されたHurst Exponentに基づいて取引するシンプルな戦略です。  
Hurst値はEMAで平滑化され、市場レジームを決定するために閾値と比較されます。

## 詳細
- **エントリー条件**:
  - **ロング**: 平滑化Hurst > 閾値
  - **ショート**: 平滑化Hurst < 閾値
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - **ロング**: 平滑化Hurst < 閾値
  - **ショート**: 平滑化Hurst > 閾値
- **ストップ**: はい、パーセンテージストップロス。
- **デフォルト値**:
  - `HurstPeriod = 100`
  - `SmoothLength = 10`
  - `Threshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(5)`
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Hurst Exponent, EMA
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
