# Tipu EA Multi-Timeframe Strategy

## Overview
This strategy recreates the core logic of the Tipu Expert Advisor in StockSharp. It replaces the proprietary Tipu Trend and Tipu Stops indicators with a combination of exponential moving averages (EMA), Average Directional Index (ADX) filtering and Average True Range (ATR) risk controls. The system looks for trend alignment between a higher timeframe (default 1 hour) and a signal timeframe (default 15 minutes), then manages the position with a break-even pyramiding module, trailing stop logic and optional fixed take profit.

The implementation focuses on liquid, trending instruments where multi-timeframe momentum signals are reliable. The higher timeframe defines context and filters out ranging phases, while the signal timeframe supplies actual entries.

## Data Subscriptions
- Higher timeframe candles (default 1 hour) for EMA trend and ADX range detection.
- Signal timeframe candles (default 15 minutes) for entry signals, ATR stop placement and trade management updates.

## Trading Logic
1. **Higher timeframe context**
   - Calculate fast and slow EMAs and detect crossovers. A bullish crossover produces an uptrend signal; a bearish crossover produces a downtrend signal.
   - Measure trend strength with ADX. If ADX is below the configured threshold, the market is marked as ranging and no new trades are allowed.
   - Store the timestamp of the last higher timeframe signal. Signal validity expires after a configurable number of minutes.
2. **Signal timeframe entries**
   - Wait for an EMA crossover on the signal timeframe **and** a fresh higher timeframe signal in the same direction while the higher timeframe is not ranging.
   - Long entries require the fast EMA to cross above the slow EMA; short entries require the opposite.
   - Before sending a new order the strategy optionally closes the opposite position (reverse-on-signal behaviour) and respects the hedging flag.
   - Initial stop distance is set to `ATR * AtrMultiplier` and capped by the `MaxRiskPips` parameter. Orders are skipped if the required risk exceeds this threshold.
3. **Risk management**
   - **Take profit**: optional fixed target based on `TakeProfitPips`.
   - **Trailing stop**: once price moves by `TrailingStartPips` in favour, the stop trails the market with a `TrailingCushionPips` offset.
   - **Risk-free mode**: when enabled the strategy moves the stop to break-even after `RiskFreeStepPips` profit and adds additional volume in `PyramidIncrementVolume` steps until `PyramidMaxVolume` is reached. Each pyramid step also tightens the protective stop.
   - Positions are closed immediately on the opposite signal if `CloseOnReverseSignal` is true.

## Parameters
- `AllowHedging` – Allow adding positions without first closing the opposite side.
- `CloseOnReverseSignal` – Flatten the current position when an opposite signal arrives.
- `EnableTakeProfit`, `TakeProfitPips` – Enable and configure the fixed take profit distance in pips.
- `MaxRiskPips` – Maximum stop distance allowed in pips. Prevents entries with excessive initial risk.
- `TradeVolume` – Base order size for the first position.
- `EnableRiskFreePyramiding`, `RiskFreeStepPips`, `PyramidIncrementVolume`, `PyramidMaxVolume` – Control the risk-free pyramiding logic.
- `EnableTrailingStop`, `TrailingStartPips`, `TrailingCushionPips` – Configure trailing stop behaviour.
- `HigherFastLength`, `HigherSlowLength`, `LowerFastLength`, `LowerSlowLength` – EMA lengths for trend detection on both timeframes.
- `AdxLength`, `AdxThreshold` – ADX parameters used to filter range-bound markets on the higher timeframe.
- `AtrLength`, `AtrMultiplier` – ATR parameters for initial stop calculation.
- `HigherSignalWindowMinutes` – Validity period for the higher timeframe signal.
- `HigherCandleType`, `LowerCandleType` – Candle types/timeframes for context and signal processing.

## Behaviour Notes
- The average entry price is recalculated whenever new volume is added, ensuring trailing stops and the risk-free module reference the actual position cost basis.
- All trading decisions are taken on completed candles only; unfinished candles are ignored to avoid premature signals.
- The strategy issues market orders (`BuyMarket`/`SellMarket`) and performs position management internally without relying on pending stop orders.
- Because the original Tipu indicators are proprietary, EMA/ADX/ATR combinations are used as a faithful approximation while keeping the original trade management features (reverse-on-signal, break-even pyramiding and trailing stop).

## Usage Tips
- Optimise EMA lengths, ATR multiplier and ADX threshold for the targeted instrument; the provided defaults work as a generic starting point for FX majors.
- Set `HigherSignalWindowMinutes` close to the higher timeframe duration to require near-synchronous alignment, or increase it to allow more lag between higher and lower timeframe signals.
- When pyramiding is disabled, the strategy still moves the stop to break-even once the `RiskFreeStepPips` distance is reached, providing basic risk protection.
- Disable `CloseOnReverseSignal` if you prefer to manage exits manually or to allow the trailing stop to manage the entire trade.
