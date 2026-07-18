# X2MA JJRSX戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

デュアル移動平均トレンドフィルターとRSIベースのエントリートリガーを組み合わせた戦略です。
トレンドは、高い時間軸で高速と低速の移動平均を比較することで定義されます。
エントリーは、RSIがトレンドの方向に沿って売られすぎまたは買われすぎのゾーンを抜けた際に、低い時間軸で実行されます。

## 詳細

- **エントリー条件**:
  - ロング: 上昇トレンドかつRSIが`Oversold`を上抜け
  - ショート: 下降トレンドかつRSIが`Overbought`を下抜け
- **ロング/ショート**: 両方
- **エグジット条件**: 逆方向のRSI閾値またはトレンド転換
- **ストップ**: なし
- **デフォルト値**:
  - `TrendCandleType` = 4h足
  - `SignalCandleType` = 30m足
  - `FastMaPeriod` = 12
  - `SlowMaPeriod` = 5
  - `RsiPeriod` = 8
  - `Overbought` = 70
  - `Oversold` = 30
