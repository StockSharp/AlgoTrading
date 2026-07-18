# CVDダイバージェンス出来高HMA RSI MACD戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はHull Moving Average、RSI、MACD、出来高フィルター、および累積出来高デルタ（CVD）のダイバージェンスを組み合わせてトレンドの機会を識別する。

HMA20がHMA50を上回り、RSIが強気モメンタムを示し、MACDヒストグラムが上昇し、出来高が平均を超え、CVDが強気ダイバージェンスを形成または増加するときにロングポジションを開く。ショートポジションはこれらの条件を逆にしたもの。

## 詳細
- **エントリー条件**:
  - **ロング**: HMA20 > HMA50 かつ価格 > HMA20; RSIが40と`RsiOverbought`の間; MACDラインがシグナルの上でヒストグラムが上昇中; 出来高 > SMA * `VolumeMultiplier`; 強気CVDダイバージェンスまたはCVD増加。
  - **ショート**: HMA20 < HMA50 かつ価格 < HMA20; RSIが`RsiOversold`と60の間; MACDラインがシグナルの下でヒストグラムが下落中; 出来高 > SMA * `VolumeMultiplier`; 弱気CVDダイバージェンスまたはCVD減少。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: 価格 < HMA20 または RSI > `RsiOverbought` またはMACDラインがシグナルを下抜け。
  - **ショート**: 価格 > HMA20 または RSI < `RsiOversold` またはMACDラインがシグナルを上抜け。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `Hma20Length` = 20
  - `Hma50Length` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5
  - `CvdLength` = 14
  - `DivergenceLookback` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 混合
  - 方向: 両方
  - インジケーター: HMA、RSI、MACD、出来高、CVD
  - ストップ: いいえ
  - 複雑さ: 上級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい
  - リスクレベル: 中
