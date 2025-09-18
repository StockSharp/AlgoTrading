# Farhad Hill Version 2 Strategy (C#)

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor “Farhad Hill Version 2”.
It combines multiple indicator filters to trade trend reversals on forex symbols. The
converted logic retains the original indicator stack (MACD, Stochastic, Parabolic SAR,
Momentum, and optional moving-average cross) and its money-management plus trailing
behaviour.

The strategy works on a single timeframe (default 30-minute candles) and opens only one
position at a time. Protective stop-loss, take-profit, and three trailing-stop styles are
supported to mirror the MQL version. All comments in the code are provided in English as
requested.

## Trading Logic
- **MACD filter** – when enabled, longs require MACD main line below the signal line;
  shorts require MACD main above the signal line.
- **Stochastic level filter** – longs demand %K below the lower threshold, shorts demand
  %K above the upper threshold. When the optional cross filter is enabled, a bullish
  %K/%D cross (from below to above) is required for longs and a bearish cross for shorts.
- **Parabolic SAR filter** – longs require SAR below price with a downward step
  (previous SAR higher than current); shorts require SAR above price with an upward
  step. The conversion uses closed candle prices as the reference.
- **Momentum filter** – calculated on candle open prices, matching the MQL settings.
  Longs need momentum below the lower threshold, shorts need momentum above the upper
  threshold.
- **Moving-average cross (optional)** – configurable MA type, applied price and periods.
  Longs need the fast MA above the slow MA; shorts need the inverse relationship.

The strategy only evaluates signals on finished candles and skips new entries when an
open position exists. Entries are placed with market orders using the calculated lot
size.

## Position Management
- **Stop-loss / Take-profit** – specified in pips. A pip is derived from the instrument’s
  `PriceStep`, falling back to `0.0001` if unavailable.
- **Trailing stop types**
  1. Immediate – once price moves beyond the stop distance, the stop follows the price.
  2. Delayed – waits for price to move by the trailing distance from the entry before
     trailing at a fixed offset.
  3. Three-stage – reproduces the original three-level logic with two break-even steps
     and a final trailing distance.
- Protective orders are placed with `SellStop`/`BuyStop` (for stop-loss) and
  `SellLimit`/`BuyLimit` (for take-profit) so they remain visible on the exchange.

## Money Management
- **Fixed lot** – trades the configured fixed volume. If `AccountIsMini` is enabled, lots
  are converted to mini-lot sizing with a minimum of 0.1.
- **Percentage risk** – replicates the original formula
  `floor(FreeMargin * percent / 10000) / 10`, clamped by the `MaxLots` limit and adjusted
  for mini accounts when required. If the portfolio value is unavailable, the strategy
  falls back to the fixed lot.

## Parameters
All parameters are exposed through `StrategyParam<T>` objects and can be optimised or
changed from the UI. Key groups:

| Group | Parameter | Description |
| --- | --- | --- |
| General | `CandleType` | Timeframe of candles used for signals |
| Money Management | `AccountIsMini`, `UseMoneyManagement`, `TradeSizePercent`, `FixedVolume`, `MaxLots` |
| Risk | `StopLossPips`, `TakeProfitPips`, `UseTrailingStop`, `TrailingStopType`, `TrailingStopPips`, `FirstMovePips`, `TrailingStop1`, `SecondMovePips`, `TrailingStop2`, `ThirdMovePips`, `TrailingStop3` |
| Indicators | `UseMacd`, `UseStochasticLevel`, `UseStochasticCross`, `UseParabolicSar`, `UseMomentum`, `UseMovingAverageCross`, `MacdFast`, `MacdSlow`, `MacdSignal`, `StochasticK`, `StochasticD`, `StochasticSlowing`, `StochasticHigh`, `StochasticLow`, `MomentumPeriod`, `MomentumHigh`, `MomentumLow`, `SlowMaPeriod`, `FastMaPeriod`, `MaMode`, `MaPrice` |

## Notes and Assumptions
- Parabolic SAR comparisons use the candle close price to approximate bid/ask checks
  from MT4. This keeps the logic deterministic on historical data.
- Money management requires a connected portfolio to obtain current equity; otherwise
  the fixed volume is used.
- Indicator combinations are processed on completed candles only, avoiding premature
  signals on partial data.

## Files
- `CS/FarhadHillVersion2Strategy.cs` – C# implementation of the strategy.
- `README.md` – This document.
- `README_ru.md` – Russian translation.
- `README_cn.md` – Chinese translation.
