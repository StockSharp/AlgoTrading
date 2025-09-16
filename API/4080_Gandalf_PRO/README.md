# Gandalf PRO Strategy

## Overview
The Gandalf PRO strategy is a StockSharp port of the MetaTrader 4 expert advisor *Gandalf_PRO*. The original robot builds an
adaptive smoothing filter from a weighted moving average and a recursive trend component. When the projected price moves at
least 15 pips beyond the current market price, the EA enters in that direction with a distant stop-loss and a take-profit at the
projected level. The StockSharp conversion reproduces the same filter and decision logic while relying on the high-level candle
API so every calculation is performed on finished bars.

## Trading logic
1. Subscribe to the timeframe selected by `CandleType` (default: 1-hour candles) and process only completed candles.
2. Maintain a rolling history of closing prices large enough to cover the maximum of `CountBuy` and `CountSell` plus one extra bar.
3. Recreate the MetaTrader `Out()` function: compute linear-weighted and simple moving averages (using a one-bar shift), derive the
   recursive `s` and `t` components with the configured price and trend factors, and obtain the projected price `s[1] + t[1]`.
4. For long setups (`EnableBuy`):
   - Check that the projected price is at least `15` pips above the latest close (`Bid + 15*x*Point` in MT4).
   - If no long position is open, buy the configured volume (see `BaseVolume` and `BuyRiskMultiplier`).
   - Store the projected price as take-profit and compute the stop-loss by subtracting `BuyStopLossPips` converted to price steps.
5. For short setups (`EnableSell`):
   - Require the projected price to sit at least `15` pips below the last close.
   - If no short position is open, sell the configured volume (reversing an existing long if necessary).
   - Save the projected price as take-profit and set the stop-loss `SellStopLossPips` pips above the market.
6. While a position exists, monitor every finished candle:
   - Exit longs if the candle low crosses the stored stop or the high reaches the take-profit.
   - Exit shorts if the candle high crosses the stop or the low hits the target.
   - Exits use `ClosePosition()` which flattens the net exposure in StockSharp.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `EnableBuy` | `bool` | `true` | Allow the strategy to open long positions. |
| `CountBuy` | `int` | `24` | Length of the smoothing filter used for long projections. |
| `BuyPriceFactor` | `decimal` | `0.18` | Weight of the current close in the long recursive filter. |
| `BuyTrendFactor` | `decimal` | `0.18` | Weight applied to the trend term when building the long projection. |
| `BuyStopLossPips` | `int` | `62` | Stop-loss distance for long positions, measured in pips. |
| `BuyRiskMultiplier` | `decimal` | `0` | Multiplier applied to `BaseVolume` before sending a long order (0 keeps the base volume). |
| `EnableSell` | `bool` | `true` | Allow the strategy to open short positions. |
| `CountSell` | `int` | `24` | Length of the smoothing filter used for short projections. |
| `SellPriceFactor` | `decimal` | `0.18` | Weight of the current close in the short recursive filter. |
| `SellTrendFactor` | `decimal` | `0.18` | Weight applied to the trend term when building the short projection. |
| `SellStopLossPips` | `int` | `62` | Stop-loss distance for short positions, measured in pips. |
| `SellRiskMultiplier` | `decimal` | `0` | Multiplier applied to `BaseVolume` before sending a short order (0 keeps the base volume). |
| `BaseVolume` | `decimal` | `1` | Base order size used when both risk multipliers are zero. |
| `CandleType` | `DataType` | 1-hour time frame | Candle series processed by the strategy. |

## Differences from the original MetaTrader EA
- MetaTrader can hold independent buy and sell tickets simultaneously. StockSharp uses net positions, so the port closes or
  reverses an existing position before opening the opposite side.
- The MT4 lot function used account free margin. The conversion exposes `BaseVolume` and two risk multipliers; when they are zero
  the base volume is used as-is, otherwise the volume is simply scaled (`BaseVolume * RiskMultiplier`).
- Stop-loss and take-profit levels are executed by monitoring completed candles. Intrabar fills may therefore differ from MetaTrader
  where protective orders are managed by the broker.
- The five-digit `Digits`/`Point` adjustment is emulated by inspecting `Security.Decimals` and `Security.PriceStep` to convert pip
  distances into absolute prices.
- All indicator calculations are performed in managed code without calling `iMA`; the recursive filter is recreated in
  `CalculateTarget` using the same coefficients as the MQL function.

## Usage notes
- Assign the desired instrument to `Strategy.Security` before starting. The strategy throws an exception if no security is attached.
- Configure `BaseVolume` to match the contract size expected by your venue; adjust the risk multipliers only if you want to scale
  the exposure relative to the base volume.
- The candle history must contain at least `max(CountBuy, CountSell) + 1` bars before any trade can be generated. Provide sufficient
  warm-up data or start the strategy with historical candles loaded.
- The 15-pip entry buffer is fixed (just like in the EA). Increase `CountBuy`/`CountSell` to smooth the projection or tweak the
  price/trend factors to match the behaviour observed in MetaTrader.
- Because exits depend on candle extremes, enable a timeframe that suits your execution latency. Lower timeframes will react sooner
  but require more historical data and may generate more signals.

## Implementation details
- Uses `SubscribeCandles()` with `Bind(ProcessCandle)` so every decision is based on finalized candles.
- Keeps a compact list of recent closes and rebuilds the recursive `s`/`t` filter on demand, mimicking the `Out()` routine.
- Converts pip-based offsets via the instrument tick size and decimal precision to replicate the MetaTrader `x * Point` scaling.
- `ClosePosition()` is invoked when protective levels are breached, ensuring the net position is flattened before another entry is
  considered.
