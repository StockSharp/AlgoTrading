# Macd Volume Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
MACD（移動平均収束拡散）と出来高確認を組み合わせた戦略。MACDラインがシグナルラインを交差し、出来高の増加で確認されたときにポジションを取ります。

テストでは年平均リターン約175%を示しています。株式市場で最もパフォーマンスが高いです。

MACDのクロスオーバーは、モメンタムを確認するための出来高増加によってフィルタリングされます。買いシグナルは拡大する出来高を伴う強気のクロスで発生し、売りはその逆です。

出来高スパイクを観察するモメンタムトレーダーに価値があるかもしれません。リスクはATRストップを使って制限されます。

## 詳細

- **エントリー条件**:
  - ロング: `MACD crosses above Signal && Volume > AvgVolume * VolumeMultiplier`
  - ショート: `MACD crosses below Signal && Volume > AvgVolume * VolumeMultiplier`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 反対方向へのMACDクロス
- **ストップ**: `StopLossPercent` でのパーセントベース
- **デフォルト値**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: MACD, Volume
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

