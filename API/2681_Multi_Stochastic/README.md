# Multi Stochastic Strategy

## Overview
The Multi Stochastic strategy is a high-level StockSharp implementation of the "Multi Stochastic (barabashkakvn's edition)" MetaTrader 5 expert advisor. It monitors up to four currency pairs simultaneously and relies on synchronized signals from Stochastic Oscillator readings (5, 3, 3). The strategy opens a single market position per symbol when an oversold or overbought crossover occurs and closes trades via fixed pip-based stop-loss and take-profit targets.

## Trading Logic
- Each configured symbol receives its own Stochastic Oscillator (length 5, %K smoothing 3, %D smoothing 3).
- A long signal is produced when the current %K is below the OversoldLevel (default 20), the previous bar had %K below %D, and the current bar closes with %K crossing above %D.
- A short signal is produced when the current %K is above the OverboughtLevel (default 80), the previous bar had %K above %D, and the current bar closes with %K crossing below %D.
- Only one open position per instrument is allowed. Additional signals are ignored until the existing position is closed.

## Risk Management
- Stop-loss and take-profit values are expressed in pips. The strategy automatically converts pips to absolute price distances by multiplying with the security price step and adjusting for 3- or 5-digit forex quotes (pip = step × 10 for those instruments).
- Long positions close when the candle low touches the stop-loss level or the candle high reaches the take-profit level.
- Short positions close when the candle high touches the stop-loss level or the candle low reaches the take-profit level.

## Parameters
- `CandleType` – time frame used for all subscribed candles (default: 1 hour).
- `StochasticLength` – base length of the Stochastic Oscillator (default: 5).
- `StochasticKPeriod` – smoothing period for %K (default: 3).
- `StochasticDPeriod` – smoothing period for %D (default: 3).
- `OversoldLevel` – threshold used to detect oversold conditions (default: 20).
- `OverboughtLevel` – threshold used to detect overbought conditions (default: 80).
- `StopLossPips` – distance to the protective stop in pips (default: 50).
- `TakeProfitPips` – distance to the profit target in pips (default: 10).
- `UseSymbol1` … `UseSymbol4` – enable trading for the respective symbol slot (default: true).
- `Symbol1` … `Symbol4` – securities traded by each slot. Symbol 1 falls back to the main strategy security when not specified.

## Implementation Notes
- Every symbol subscription is independent. Each uses `SubscribeCandles` with `BindEx` to receive `StochasticOscillatorValue` updates alongside candle data.
- Previous %K and %D values are cached per symbol to emulate the MT5 crossover detection logic.
- Risk parameters are recalculated for every entry, and stop/take levels reset after a position is closed or when no position exists.
- Orders are sent with `BuyMarket`/`SellMarket` using the shared `Volume` property, matching the single-position constraint from the original expert.

## Differences from the MT5 Version
- The StockSharp version leverages high-level subscriptions instead of manual rate refresh calls.
- Pip size detection relies on `Security.PriceStep` and `Security.Decimals`. If metadata is unavailable, stops and targets remain disabled to prevent incorrect risk calculations.
- Logging and chart drawing hooks are ready for extension but not required for the core behaviour.

## Usage Tips
1. Assign the desired securities to the symbol slots and adjust the candle timeframe to match your trading horizon.
2. Ensure that stop-loss and take-profit distances are compatible with the instrument tick size to avoid immediate closures.
3. Disable unused symbol slots to reduce resource consumption when monitoring fewer instruments.
