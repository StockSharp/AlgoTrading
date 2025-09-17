# Killer Sell 2.0 (C#)

## Overview
Killer Sell 2.0 is a short-only MetaTrader 4 expert advisor that times entries after
extended overbought readings and locks in profits when momentum swings into
oversold territory. This port rewrites the original logic on top of the StockSharp
high-level strategy API. All indicator processing is event driven through
`SubscribeCandles().BindEx(...)`, and money-management rules are encapsulated
inside the strategy class.

## Trading logic
The converted logic follows the original signal chain while using the net position
model of StockSharp. Every completed candle of the configured timeframe executes
the following steps:

1. **Data preparation.** The strategy updates a MACD (12/120/9), Williams %R
   (period 350 for both filters) and two Stochastic oscillators (10/1/3 for entry,
   90/7/1 for exits). Indicator values are consumed only when the new bar is
   finished and the inputs are fully formed.
2. **Entry filter.** A short setup is valid when all conditions below are met:
   - Williams %R rises above −10, signalling an overbought market.
   - The MACD main line is greater than `0.0014`.
   - The entry Stochastic %K crosses **below** the configurable entry level
     (default 90). Cross detection is performed on consecutive %K readings.
3. **Order placement.** Once the filters align, the strategy sends a market sell
   using the current martingale lot size. Orders inherit a take-profit set `N`
   pips away (default 100 pips) via `StartProtection`.
4. **Exit management.** While a short exposure exists, the strategy computes the
   arithmetic mean of the open tickets' profit in pips. Depending on momentum:
   - If the average profit is **below** 10 pips and Williams %R falls under −80,
     all shorts are closed immediately.
   - If the average profit is **above** 15 pips and the exit Stochastic %K drops
     under 12, the position is closed to secure the gain.

## Money management
Killer Sell 2.0 uses a martingale ladder similar to the original EA. The StockSharp
implementation keeps an internal list of open short lots in order to mimic the
per-ticket calculations from MetaTrader:

- The first trade uses `InitialVolume` (default 0.05 lots).
- After a profitable or breakeven cycle the volume resets to the initial lot size.
- After a losing cycle the next order is multiplied by `MartingaleMultiplier`
  (default ×1.2). A safety cap `MaxVolume` prevents uncontrolled growth.

The helper also tracks realized PnL on fills to decide whether the previous cycle
was profitable.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Primary timeframe that feeds every indicator. |
| `EntryWprPeriod` / `ExitWprPeriod` | Williams %R lengths for entry and exit confirmations. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD configuration. |
| `MacdThreshold` | Minimum MACD main-line value required for a sell. |
| `StochasticEntryKPeriod`, `StochasticEntryDPeriod`, `StochasticEntrySlow` | Entry Stochastic parameters. |
| `EntryStochasticLevel` | Level that %K must cross from above to validate a signal. |
| `StochasticExitKPeriod`, `StochasticExitDPeriod`, `StochasticExitSlow` | Exit Stochastic parameters. |
| `ExitStochasticLevel` | Oversold bound checked before locking profits. |
| `EntryWprThreshold` / `ExitWprThreshold` | Williams %R thresholds for entries/exits. |
| `LossExitPips` / `ProfitExitPips` | Average profit bounds (in pips) controlling defensive and target exits. |
| `TakeProfitPips` | Protective take profit assigned to each sell order. |
| `InitialVolume` | First martingale step volume. |
| `MartingaleMultiplier` | Factor applied after losses. |
| `MaxVolume` | Absolute cap applied to the next lot size. |

## Conversion notes
- MetaTrader keeps individual tickets; StockSharp works with a net position.
  The strategy therefore stores every filled short (volume + price) to reproduce
  average-profit calculations and to evaluate martingale resets.
- The MT4 "martingale" block exposed many additional modes (fixed, percent risk,
  1326, Fibonacci, etc.). The original configuration used the simple martingale
  branch; only that behaviour is replicated here.
- Emergency stop loss was disabled in the source project. The port mirrors that
  setup by only attaching a take-profit and handling other exits internally.

## Usage tips
1. Attach the strategy to a portfolio and security, then set the same timeframe
   used in the MT4 backtests (the defaults assume H1).
2. Ensure that market data delivers completed candles; indicators rely on
   `CandleStates.Finished` events.
3. Review account leverage and permissible lot sizes. The default martingale cap
   (5 lots) should be adjusted to your broker requirements.
4. Backtest thoroughly—martingale strategies amplify risk when markets trend
   strongly against the short bias.

