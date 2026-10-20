# Bias Sentiment Strength

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 取引ルール

確定した 15 分足で MACD(12,26,9)、Wilder RSI(14)、ストキャスティクス(21,14,14)、Awesome Oscillator(5,34)、Alligator、出来高による偏りを合成します。生の K は 21 本の高安、`Ks` はその 14 値 SMA、`D` はさらに 14 値 SMA です。Alligator は中央値の SMMA で、jaw は 13 期間・8 本シフト、teeth は 8・5、lips は 5・3 です。

`BASS = 2 * (MACD - Signal) + (RSI - 50) / 5 + 0.15 * (Ks - D) + 0.6 * AO + (Lips - Teeth) + (Teeth - Jaw) + VWMA(30) - SMA(30)`。全成分の形成を待ち、既定では最低 47 本の確定足が必要です。不正な設定は明示的に失敗します。

`BassThreshold` より大きければ買い `Volume`、`-BassThreshold` より小さければ売り `Volume` が目標です。既定の閾値はゼロ。水準を評価するため、損切り後の再エントリーに新たなゼロ交差は不要です。反転数量は `Volume + abs(Position)` とし、未完了注文は次の注文を抑止します。原生ローカル成行ストップは実約定建玉を `StopLossPercent`%（既定 1）で全決済します。利確は設定しません。

ネイティブのローカル保護は銘柄の足と購読した価格更新を使用し、距離は実際の新規約定を基準にします。決済は成行で、約定価格は市場の状況と呼値に依存します。

## パラメーター

| パラメーター | 既定値 |
|---|---|
| `MacdFast` | `12` |
| `MacdSlow` | `26` |
| `MacdSignal` | `9` |
| `RsiPeriod` | `14` |
| `StochK` | `21` |
| `StochSmooth` | `14` |
| `StochD` | `14` |
| `AoShort` | `5` |
| `AoLong` | `34` |
| `VolumeLength` | `30` |
| `BassThreshold` | `0` |
| `StopLossPercent` | `1` |
| `CandleType` | `15m` |
