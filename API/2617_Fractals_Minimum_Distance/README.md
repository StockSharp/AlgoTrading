# Fractals Minimum Distance

## Overview
Fractals Minimum Distance replicates the MetaTrader expert advisor "Fractals minimum distance" using StockSharp's high level strategy API. The system scans the configured candle series for Bill Williams style five-bar fractal patterns. Each time a new confirmed fractal appears at the specified signal bar offset, the strategy measures the gap between the most recent up and down fractals. A market order is allowed only when this distance exceeds the required threshold expressed in pips.

The conversion keeps the original behaviour of closing any opposite exposure immediately before reversing. Unlike the MQL version, the position size is taken from the strategy's `Volume` property instead of performing account-based risk calculations. No stop-loss or take-profit orders are submitted, matching the source expert.

## Signal Logic
1. Subscribe to the candle type defined by `CandleType` and build rolling buffers of highs and lows that always contain the bar located `SignalBar` candles in the past together with two neighbours on each side.
2. Detect an **upper fractal** when the high of the centre bar is strictly greater than the highs of the two preceding and the two following candles. Detect a **lower fractal** analogously for lows.
3. Convert the `DistancePips` parameter to a price distance using the symbol's `PriceStep`. Symbols with three or five decimal digits are automatically adjusted to treat 0.001/0.00001 quotes as one pip.
4. When an upper fractal is confirmed:
   - Store the new upper level and close existing long positions.
   - If both the latest upper and lower fractals are known and their absolute difference is at least the distance threshold, submit a market sell order using `Volume`.
5. When a lower fractal is confirmed:
   - Store the new lower level and close existing short positions.
   - If the distance condition is satisfied, submit a market buy order using `Volume`.

Trades are placed only after the candle that finalises the fractal is closed, ensuring that unfinished bars never trigger entries. The strategy relies on `IsFormedAndOnlineAndAllowTrading()` to avoid placing orders before the environment is ready.

## Parameters
| Name | Description | Notes |
| --- | --- | --- |
| `DistancePips` | Minimum spacing between the last up and down fractals measured in pips. | Converted internally to price units using the instrument's tick size. |
| `SignalBar` | Number of fully closed bars that must pass after the bar hosting the fractal. | Minimum effective value is 2, matching the two-bar confirmation used by Bill Williams fractals. |
| `CandleType` | Data series that feeds the calculations. | Default is one-minute time frame; change to work on other resolutions. |
| `Volume` | Standard StockSharp strategy property defining the trade size. | Replace the original risk-based sizing from the MetaTrader expert. |

## Position Management and Differences vs. MQL
- Positions are always flattened before reversing direction, exactly as the source `ClosePositions` helper did.
- The original expert called `RefreshRates()` and performed explicit slippage settings. Those aspects are delegated to the StockSharp infrastructure in this port.
- Stop-loss and take-profit orders were not part of the MQL logic and remain absent here.
- `DistancePips` uses integer precision like the `ushort` input, while `SignalBar` mirrors the MQL `uchar` input.
- Because StockSharp works with net positions, opening an order in the opposite direction automatically flips the exposure, matching the MetaTrader netting behaviour.

## Usage Tips
- Start with the same signal bar offset (`SignalBar = 3`) from the original code and calibrate the distance threshold according to the instrument's volatility.
- Increase `SignalBar` to wait for more candles after a fractal appears, which can filter out rapid oscillations.
- Combine with external risk management such as the built-in `StartProtection()` helper if a protective stop is required.
