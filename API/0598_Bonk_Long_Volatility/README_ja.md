# BONK ロング・ボラティリティ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この純ロング戦略は、移動平均、ボラティリティ、出来高フィルターを組み合わせた強い強気条件でエントリーします。市場が上昇トレンドにあり、ボラティリティが拡大し、モメンタム指標が強さを確認したときに買います。決済には固定テイクプロフィット、ストップロス、ATRベースのトレーリングストップを使用します。

## 詳細

- **エントリー条件**: 速いMAが遅いMAを上回る、価格レンジがATR * `AtrMultiplier`を超える、RSIが`RsiOversold`と`RsiOverbought`の間、MACDラインがシグナルとゼロを上回る、出来高がSMA * `VolumeThreshold`を上回る、終値が速いMAを上回る、ローソク足が最後の`LookbackDays`以内。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: テイクプロフィット、ストップロス、またはATRトレーリングストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `ProfitTargetPercent` = 5.0m
  - `StopLossPercent` = 3.0m
  - `AtrLength` = 10
  - `AtrMultiplier` = 1.5m
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeSmaLength` = 20
  - `VolumeThreshold` = 1.5m
  - `MaFastLength` = 5
  - `MaSlowLength` = 13
  - `LookbackDays` = 30
  - `CandleType` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: SMA, ATR, RSI, MACD, 出来高
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

