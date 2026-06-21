# CVDダイバージェンス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は累積出来高デルタ（CVD）のダイバージェンスをHull Moving Average、RSI、MACD、出来高フィルターと組み合わせる。トレンド、モメンタム、出来高が一致し、CVDがダイバージェンスまたはトレード方向への継続を示したときにポジションを開く。逆シグナルまたはインジケーターのクロスでポジションを閉じる。

## 詳細

- **エントリー条件**: HMAによるトレンドの整合、RSIとMACDの確認、高出来高とCVDダイバージェンス/継続。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはインジケーターのクロス。
- **ストップ**: 明示的なストップなし。
- **デフォルト値**:
  - `HmaFastLength` = 20
  - `HmaSlowLength` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5m
  - `CvdLength` = 14
  - `DivergenceLookback` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: ダイバージェンス
  - 方向: 両方
  - インジケーター: HMA、RSI、MACD、出来高
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい
  - リスクレベル: 中
