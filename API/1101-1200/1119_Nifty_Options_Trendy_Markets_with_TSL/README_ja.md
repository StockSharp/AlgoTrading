# Nifty Options トレンド市場戦略（TSL付き）
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Bollinger BandsにADXとSupertrendフィルターを組み合わせたブレイクアウト戦略。エントリーには出来高スパイクが必要。ポジションはMACDクロス、ADXの弱体化、またはATRベースのトレーリングストップで決済される。

## 詳細

- **エントリー条件**:
  - ロング: 価格がBollinger Band上限を上抜け && ADX > 閾値 && 出来高スパイク && 価格がSupertrendより上
  - ショート: 価格がBollinger Band下限を下抜け && ADX > 閾値 && 出来高スパイク && 価格がSupertrendより下
- **ロング/ショート**: 両方
- **エグジット条件**: MACDクロス、ADX低下またはATRトレーリングストップ
- **ストップ**: ATRトレーリングストップ
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2m
  - `AdxLength` = 14
  - `AdxEntryThreshold` = 25m
  - `AdxExitThreshold` = 20m
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5m
  - `VolumeSpikeMultiplier` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Bollinger Bands, ADX, Supertrend, MACD, ATR
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
