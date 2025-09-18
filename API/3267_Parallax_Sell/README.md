# Parallax Sell Strategy

## Overview
Parallax Sell is a short-only martingale strategy converted from the MetaTrader expert advisor `parallax_sell`. The original robot traded JPY crosses (CAD/JPY and CHF/JPY) and relies on a confluence of Williams %R, MACD and stochastic oscillator filters to initiate shorts into overbought rallies. Position exits depend on momentum fading signs provided by Williams %R or a slow stochastic, while a martingale-like position sizing scheme increases exposure after losing sequences.

## Entry Logic
- Work on the configurable timeframe (default: 1-hour candles).
- Wait for a fresh candle close.
- Require Williams %R (entry lookback 350) to be above the overbought threshold (default -10).
- Require the MACD main line (12/120/9 settings) to stay above a bullish threshold (default 0.178) to confirm upward momentum before fading it.
- Detect a downward cross of the fast stochastic %K (length 10, slowing 3) below the entry trigger level (default 90). Only this cross event can produce a new short.
- Every qualified signal sends an additional market sell order. Multiple short orders can stack, following the martingale volume logic.

## Exit Logic
- Track the floating profit of all open shorts in pips using the instrument pip size.
- If only one short is open and the average profit exceeds the single-trade target (default 10 pips) **and** Williams %R drops below the exit threshold (default -80), close the position.
- If more than one short is open and the average basket profit exceeds the basket target (default 15 pips) **and** the slow stochastic %K (length 90, slowing 1) falls below the oversold trigger (default 12), close the entire basket.
- An additional safety take-profit closes the basket when the average gain reaches the configured take-profit distance (default 100 pips).

## Position Sizing
- Start with the base volume (default 0.01 lots).
- After a profitable cycle (realized PnL increase), reset the next order volume to the base volume.
- After a losing cycle (realized PnL decrease), multiply the next order volume by the martingale multiplier (default 1.6). Volumes are automatically aligned to the instrument volume step.

## Risk Management
- The strategy registers a protective take-profit order using the configured pip distance. No fixed stop loss is used; exits are driven by indicator filters.
- Start protection is engaged once, as required by the StockSharp conversion guidelines.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Timeframe used for calculations. | 1H candles |
| `EntryWilliamsLength` | Williams %R lookback for entries. | 350 |
| `ExitWilliamsLength` | Williams %R lookback for exits. | 350 |
| `EntryStochasticLength` / `Signal` / `Slowing` | Fast stochastic settings for the entry cross. | 10 / 1 / 3 |
| `ExitStochasticLength` / `Signal` / `Slowing` | Slow stochastic settings for exit confirmation. | 90 / 7 / 1 |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | MACD parameters. | 12 / 120 / 9 |
| `EntryWilliamsThreshold` | Minimum Williams %R value required before shorting. | -10 |
| `ExitWilliamsThreshold` | Williams %R level that confirms exit for a single trade. | -80 |
| `EntryStochasticTrigger` | Level the fast stochastic must cross downward to trigger entries. | 90 |
| `ExitStochasticTrigger` | Level the slow stochastic must drop below to close baskets. | 12 |
| `MacdThreshold` | Minimum MACD main-line value. | 0.178 |
| `SingleTradeTargetPips` | Profit target (pips) when only one short is active. | 10 |
| `MultiTradeTargetPips` | Profit target (pips) when multiple shorts are active. | 15 |
| `TakeProfitPips` | Hard take-profit distance (pips). | 100 |
| `InitialVolume` | Base order size. | 0.01 |
| `MartingaleMultiplier` | Multiplier applied after a loss when martingale is enabled. | 1.6 |
| `UseMartingale` | Enable or disable martingale escalation. | true |

## Notes
- The strategy only trades short positions and assumes Forex-like pip conventions when measuring profits.
- The average profit calculations treat each entry equally, mirroring the MetaTrader block that averaged pips per trade.
- Adjust thresholds or disable martingale (`UseMartingale = false`) to reduce risk on highly volatile pairs.
