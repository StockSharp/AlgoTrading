# Liquidex Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Liquidex Keltner** 戦略は移動平均トレンドフィルターを使用してケルトナーチャネルのブレイクアウトを取引します。
取引は指定された時間帯のみ許可され、オプションでRSIの方向によって確認することができます。
ストップロスとテイクプロフィットは固定パーセンテージで管理されます。

## 詳細
- **エントリー条件**:
  - 価格が上部ケルトナーバンドを上抜けし、移動平均の上でクローズする。
  - 価格が下部ケルトナーバンドを下抜けし、移動平均の下でクローズする。
  - ローソク足の実体が `RangeFilter` を超えなければならない。
  - `UseRsiFilter` が有効な場合、RSIはロングで50以上、ショートで50未満でなければならない。
  - 現在時刻が `EntryHourFrom` と `EntryHourTo` の間にあり、金曜日は `FridayEndHour` 前でなければならない。
- **ロング/ショート**: 両方向。
- **エグジット条件**: ストップロスまたはテイクプロフィット。
- **ストップ**: はい、`StartProtection` によるパーセンテージベース。
- **デフォルト値**:
  - `MaPeriod = 7`
  - `RangeFilter = 10m`
  - `StopLoss = 1m`
  - `TakeProfit = 2m`
  - `UseKeltnerFilter = true`
  - `KeltnerPeriod = 6`
  - `KeltnerMultiplier = 1m`
  - `UseRsiFilter = false`
  - `RsiPeriod = 14`
  - `EntryHourFrom = 2`
  - `EntryHourTo = 24`
  - `FridayEndHour = 22`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: MA, Keltner, RSI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
