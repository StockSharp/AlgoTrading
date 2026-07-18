# MACD 出来高 XAUUSD 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

XAUUSD 向けの 15 分足戦略で、MACD のゼロライン・クロスと出来高オシレーター・フィルター、固定リスクパラメーターを組み合わせます。

## 詳細

- **エントリー条件**: 出来高オシレーターが正で出来高比較を満たした状態で MACD がゼロラインを交差。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ストップロスまたはテイクプロフィット水準。
- **ストップ**: 固定ストップロスとテイクプロフィット乗数。
- **デフォルト値**:
  - `ShortLength` = 5
  - `LongLength` = 8
  - `FastLength` = 16
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Leverage` = 1.0
  - `StopLoss` = 10100
  - `TakeProfitMultiplier` = 1.1
  - `CandleType` = TimeSpan.FromMinutes(15)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: MACD、EMA、Volume
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
