# Bobnaley Strategy

## Overview
The Bobnaley strategy reproduces the MetaTrader 5 expert advisor "bobnaley" using the StockSharp high level API. It combines a simple moving average trend filter with the stochastic oscillator to search for reversal opportunities. The original expert evaluated tick prices; the port uses completed candles and keeps the order management rules intact.

## How It Works
1. **Indicators**
   - A simple moving average with the configured period filters the prevailing direction.
   - A stochastic oscillator (main and signal lines) identifies oversold and overbought situations. Only the main line is needed for signals; the signal line is calculated for completeness.
2. **Entry Conditions**
   - The strategy waits until the current candle is finished and all indicators are formed.
   - Long entries require the moving average to be strictly decreasing during the last three samples while price closes above the latest average. At the same time the stochastic main line must be below the oversold level and its previous value must be higher than the one before it, mirroring the original EA requirement `stochVal[1] > stochVal[2]`.
   - Short entries are the mirror image: the moving average must be rising in the last three samples while the price closes below it, and the stochastic main line must be above the overbought level while its previous value is lower than the earlier one.
   - New trades are opened only when no position is currently active, replicating the `PositionSelect` guard from MetaTrader.
3. **Risk Management**
   - When a position is opened the strategy relies on StockSharp's protection service to place a take-profit and a stop-loss in absolute price units. These distances match the MetaTrader inputs (0.007 and 0.0035 by default).
   - Before every decision the portfolio value is compared against the `Minimum Balance` parameter, mirroring the free-margin filter (`ACCOUNT_FREEMARGIN > 5000`) of the original code. If the account value is known and below the threshold, the entry is skipped.
4. **Volume Handling**
   - Orders use a fixed `Base Volume` parameter. This reproduces the lot setting that the MetaTrader script used after applying its own rounding routine.

## Parameters
| Category | Name | Description | Default |
| --- | --- | --- | --- |
| General | Candle Type | Candle data type used for indicator calculations. | 5-minute time frame |
| Trading | Base Volume | Fixed order volume applied to every new position. | 5 |
| Indicators | MA Period | Length of the simple moving average. | 76 |
| Indicators | Stochastic Period | Lookback for the stochastic main line. | 5 |
| Indicators | Stochastic %K | Smoothing length for the %K line. | 3 |
| Indicators | Stochastic %D | Smoothing length for the %D line. | 3 |
| Indicators | Stochastic Oversold | Threshold that defines oversold territory for the main line. | 30 |
| Indicators | Stochastic Overbought | Threshold that defines overbought territory for the main line. | 70 |
| Risk Management | Take Profit | Distance between the entry price and the take-profit in price units. | 0.007 |
| Risk Management | Stop Loss | Distance between the entry price and the stop-loss in price units. | 0.0035 |
| Risk Management | Minimum Balance | Minimal portfolio value required before a new order can be sent. | 5000 |

## Notes
- The original expert used Bid/Ask quotes; in StockSharp the candle close is used as the execution price proxy.
- No trailing exits are implemented—the trade is closed only by the protective orders.
- Stochastic calculations follow the default MetaTrader settings (5/3/3) but can be optimized via the exposed parameters.
