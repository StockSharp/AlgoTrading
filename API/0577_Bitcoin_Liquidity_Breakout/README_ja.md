# Bitcoin 流動性ブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は流動性と変動性が高く、短期トレンドが強気のときにロングポジションをエントリーします。高流動性とは出来高がその移動平均に閾値を掛けた値を上回る状態を指します。ATRがその移動平均を超えたときに変動性が確認されます。

## 詳細

- **エントリー条件**:
  - `出来高 > SMA(出来高) * LiquidityThreshold`
  - `価格変動(%) > PriceChangeThreshold`
  - `高速SMA > 低速SMA`
  - `RSI < 65`
  - `ATR > SMA(ATR,10)`
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 高速SMAが低速SMAを下抜け、またはRSI > 70。
- **ストップ**: オプションのストップロスとテイクプロフィットの割合。
- **デフォルト値**:
  - `LiquidityThreshold` = 1.3
  - `PriceChangeThreshold` = 1.5
  - `VolatilityPeriod` = 14
  - `LiquidityPeriod` = 20
  - `FastMaPeriod` = 9
  - `SlowMaPeriod` = 21
  - `RsiPeriod` = 14
  - `StopLossPercent` = 0.5
  - `TakeProfitPercent` = 7
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: ロング
  - インジケーター: SMA, RSI, ATR
  - ストップ: はい
  - 複雑さ: 中程度
  - 時間軸: 1h
