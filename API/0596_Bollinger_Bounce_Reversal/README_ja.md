# Bollinger バウンス・リバーサル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格がBollinger Bandsから反発するときに、MACDと出来高の確認を使ってリバーサルを捉える戦略です。システムは1日あたり最大5取引にエントリーを制限し、固定パーセントのストップロスとテイクプロフィットを適用します。

## 詳細

- **エントリー条件**:
  - ロング: `Close[1] < LowerBand[1] && Close > LowerBand && MACD > Signal && Volume >= AvgVolume * VolumeFactor`
  - ショート: `Close[1] > UpperBand[1] && Close < UpperBand && MACD < Signal && Volume >= AvgVolume * VolumeFactor`
- **ロング/ショート**: 両方
- **ストップ**: パーセントのテイクプロフィットとストップロス
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BbStdDev` = 2m
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `VolumePeriod` = 20
  - `VolumeFactor` = 1m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: Bollinger Bands, MACD, 出来高
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
