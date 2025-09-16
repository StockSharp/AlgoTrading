# Fractal ZigZag Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a direct port of the MetaTrader 4 expert advisor **Fractal ZigZag Expert.mq4**. It rebuilds the Bill Williams
fractal sequence and interprets the most recent confirmed extremum as the active market leg. When the latest valid fractal is a
swing low, the system opens a long position; when a swing high is confirmed, it opens a short. The implementation keeps the
original parameters — fractal depth, take profit, initial stop and trailing stop distances — while adapting the order routing to
the high-level StockSharp API.

The strategy is best suited for H1 candles, replicating the default chart used in the MetaTrader version. Nevertheless, the
`CandleType` parameter allows switching to any other timeframe supported by the data feed. All distances are expressed in price
points (instrument price steps), which mirrors the way MetaTrader uses the `Point` constant.

## Trading rules

- **Signal detection**
  - The algorithm scans each finished candle and builds a rolling window with `2 * Level + 1` elements.
  - A high fractal is confirmed when the middle candle has the highest high inside that window; a low fractal requires the lowest
    low.
  - Only the latest confirmed fractal controls the direction: a low sets the internal trend to `2` (bullish), a high sets it to
    `1` (bearish).
- **Entries**
  - When the internal trend equals `2` and there is no open position, a market buy is sent using the `Lots` volume.
  - When the trend equals `1` with no position, a market sell is sent.
  - The strategy will re-enter in the same direction after a position closes if the trend has not flipped.
- **Exits & risk management**
  - Every entry receives an initial stop loss and a fixed take profit defined in points. A stop value of `0` disables the
    respective protection.
  - Optional trailing stop (also in points) activates once price moves by the configured distance. The stop is then moved to
    maintain the same offset from the closing price, never crossing the initial protective stop.
  - Protective orders are emulated by monitoring candle highs/lows to approximate intrabar touches, closely matching the original
    MQL4 logic.

## Default parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Level` | `2` | Number of candles on each side required to confirm a fractal. |
| `TakeProfitPoints` | `25` | Distance to the take profit target in price points. |
| `InitialStopPoints` | `20` | Distance to the initial stop loss in price points. |
| `TrailingStopPoints` | `10` | Trailing stop distance in price points (set to `0` to disable). |
| `Lots` | `1` | Order volume used for market entries. |
| `CandleType` | `H1` | Timeframe of candles processed by the strategy. |

## Notes

- The strategy calls `StartProtection()` once at startup so that StockSharp can manage emergency position liquidation if needed.
- All logs and comments are provided in English, while descriptions follow the language of each README variant as required by the
  conversion guidelines.
- The implementation avoids indicator buffers and mimics the MetaTrader approach by keeping only the minimal rolling window
  necessary to evaluate a fractal.
