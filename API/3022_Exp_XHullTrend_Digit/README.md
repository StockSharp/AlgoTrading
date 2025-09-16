# Exp XHullTrend Digit Strategy

## Overview
- Conversion of the MQL5 expert `Exp_XHullTrend_Digit.mq5` located in `MQL/22117`.
- Uses the high-level StockSharp API with the custom `XHullTrendDigitIndicator` that replicates the original XHullTrend Digit logic.
- Focused on medium-term trend following on the configured indicator timeframe (default 8 hours).

## Indicator Logic
1. Price is taken from the selected candle source (close by default).
2. Two moving averages are calculated with lengths `BaseLength` and `BaseLength / 2` using the chosen smoothing method (simple, exponential, smoothed, or weighted).
3. A Hull-style projection `2 * shortMA - longMA` is smoothed twice: first by `SignalLength`, then by `sqrt(BaseLength)`.
4. Both resulting lines are rounded to the nearest multiple of the instrument step scaled by `10^RoundingDigits` to mimic the digit rounding of the MQL5 version.
5. When rounding produces equal values while the raw values differ, the faster line is nudged by one step in the direction of the difference so that the cross remains detectable.

## Trading Rules
- Signals are evaluated on closed candles only.
- `SignalBar` defines how many bars back are used for the cross detection (1 = use the previous completed bar against the bar before it).
- Long entry: previous fast line above slow line **and** the selected bar fast line at or below the slow line (up-cross). Short positions are optionally closed at the same time.
- Short entry: previous fast line below slow line **and** the selected bar fast line at or above the slow line (down-cross). Long positions are optionally closed simultaneously.
- Long exit: whenever the previous fast line falls below the slow line.
- Short exit: whenever the previous fast line rises above the slow line.
- If a reversal signal appears while holding the opposite position, the strategy sends the close order followed by an order sized to flip the position into the new direction.

## Parameters
- `OrderVolume` – volume for market entries.
- `StopLoss` / `TakeProfit` – optional protection distances in price steps (converted to StockSharp `UnitTypes.Step`).
- `EnableBuyEntry`, `EnableSellEntry` – allow or block new positions in each direction.
- `EnableBuyExit`, `EnableSellExit` – control automatic exits for long and short sides.
- `CandleType` – timeframe used for indicator calculations (default 8-hour time frame).
- `BaseLength` – base smoothing length for the indicator (maps to `XLength` in MQL5).
- `SignalLength` – length of the intermediate Hull smoothing (`HLength` in MQL5).
- `PriceSource` – candle price used for calculations (close/open/high/low/typical/weighted/median/average).
- `SmoothMethod` – moving average type for all smoothing stages (simple, exponential, smoothed, weighted).
- `Phase` – kept for compatibility; no effect with the supported smoothing types.
- `RoundingDigits` – number of additional digit adjustments applied during rounding.
- `SignalBar` – bar offset for signal evaluation (0 = current closed bar, 1 = previous bar, etc.).

## Risk Management
- Optional stop loss and take profit handled by the built-in `StartProtection` helper using step-based distances.
- Volume can be tuned via `OrderVolume` to match the target instrument size.

## Notes
- The custom indicator reproduces the rounding behaviour of the original script; ensure `Security.PriceStep` is configured for accurate rounding.
- Only SMA, EMA, SMMA (RMA) and LWMA smoothing are implemented because the StockSharp standard library provides these out of the box. Other exotic smoothing modes from the MQL5 source can be added later if required.
- Works on any instrument that delivers candles for the selected timeframe. Adjust rounding digits and base length when switching between assets with different tick sizes.
