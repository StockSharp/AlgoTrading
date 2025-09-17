# MA Cross Method PriceMode Strategy

## Overview
The **MA Cross Method PriceMode** strategy is a direct StockSharp port of the MetaTrader 4 expert "MA_cross_Method_PriceMode". It combines two configurable moving averages and reacts whenever the fast average crosses the slow average. Both lines expose the original MetaTrader inputs: period, smoothing method (SMA, EMA, SMMA, LWMA), applied price (close, open, high, low, median, typical, weighted) and horizontal shift. The strategy works with any instrument that provides regular time-based candles.

## Indicators
- **Fast Moving Average** – configurable length, method and price source. The MetaTrader shift parameter is reproduced by buffering the completed indicator values and reading the value `FirstShift` bars back.
- **Slow Moving Average** – configurable length, method and price source with the same shift emulation via buffering.

## Trading Logic
1. The strategy subscribes to the selected candle type and processes only finished candles to avoid intra-bar repainting.
2. For every closed bar it feeds both moving averages with their respective applied prices.
3. When both averages produce final values, the strategy evaluates two conditions:
   - **Bullish cross** – the fast MA was below or equal to the slow MA on the previous bar and moves above it on the current bar.
   - **Bearish cross** – the fast MA was above or equal to the slow MA on the previous bar and moves below it on the current bar.
4. On a bullish cross the strategy buys `OrderVolume` contracts. If a short position is open, the order size is increased automatically to both cover the short and establish the new long exposure.
5. On a bearish cross the strategy sells `OrderVolume` contracts. If a long position is open, the order size is increased to close it before establishing the short.
6. `StartProtection()` is invoked so that StockSharp protective modules can be added if desired (for example, stop-loss or break-even assistants).

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `FirstPeriod` | Period of the fast moving average. | `3` |
| `SecondPeriod` | Period of the slow moving average. | `13` |
| `FirstMethod` | Smoothing method used for the fast moving average (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Simple` |
| `SecondMethod` | Smoothing method used for the slow moving average. | `LinearWeighted` |
| `FirstPriceMode` | Applied price for the fast moving average (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Close` |
| `SecondPriceMode` | Applied price for the slow moving average. | `Median` |
| `FirstShift` | Horizontal shift (in bars) applied to the fast moving average. | `0` |
| `SecondShift` | Horizontal shift (in bars) applied to the slow moving average. | `0` |
| `OrderVolume` | Base order volume used for new positions. | `0.1` |
| `CandleType` | Candle type/timeframe processed by the strategy. | 5-minute candles |

## Differences Compared to the MQL Version
- MetaTrader order iteration (`OrdersTotal`, `OrderSelect`, `OrderClose`) is replaced by direct usage of the StockSharp `Strategy.Position` property and market orders sized to reverse exposure when required.
- The MetaTrader "new bar" flag is not necessary: `ProcessCandle` is executed exactly once per finished candle, ensuring the same once-per-bar behaviour without tick-level polling.
- MA shift handling is implemented with compact buffers that hold the last `shift + 2` values for each average. This mirrors the indicator displacement without relying on forbidden indicator back-references (`GetValue`).
- The strategy is broker-agnostic; risk management helpers can be attached via `StartProtection()` instead of the fixed MetaTrader stop/limit arguments.

## Usage Notes
- Choose candle duration that matches the original timeframe (for example, M5 or H1). Custom time frames can be supplied by editing `CandleType` in the strategy parameters.
- Setting `FirstShift` or `SecondShift` to a positive value delays the effective crossover by that many completed bars, just like the horizontal shift input in MetaTrader.
- The `Weighted` price mode reproduces MetaTrader’s `(High + Low + 2 * Close) / 4` formula. Median and typical modes follow the standard `(High + Low) / 2` and `(High + Low + Close) / 3` definitions.
- Because every order is a market order, ensure that the account configuration tolerates the requested volume and slippage.
