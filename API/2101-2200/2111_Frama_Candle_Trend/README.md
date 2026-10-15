# FrAMA Candle Trend Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy converts the MetaTrader *Exp_FrAMACandle* expert into a StockSharp high-level strategy.

## Strategy Logic

- Uses the **Fractal Adaptive Moving Average (FrAMA)** calculated separately for candle open and close prices.
- A bullish signal occurs when the FrAMA of the close price rises above the FrAMA of the open price. If the previous bar was not bullish, the strategy opens a long position and closes existing shorts.
- A bearish signal occurs when the FrAMA of the close price falls below the FrAMA of the open price. If the previous bar was not bearish, the strategy opens a short position and closes existing longs.
- Signals are evaluated on finished candles only. Historical color values are stored to respect the `SignalBar` offset.

## Parameters

| Name | Description |
| --- | --- |
| `CandleType` | Timeframe used for indicator calculation. Default: 4 hours. |
| `FramaPeriod` | Period of the FrAMA indicator. |
| `SignalBar` | Offset of the bar used for signal detection. |
| `BuyOpen` / `SellOpen` | Enable opening of long/short positions. |
| `BuyClose` / `SellClose` | Enable closing of long/short positions. |

## Notes

- The strategy relies solely on FrAMA crossovers and does not implement stop-loss or take-profit management.
- Position volume is controlled by the base `Volume` property of the strategy.
