# Pipsover Chaikin Hedge

## Overview
This strategy reproduces the "Pipsover 2" MetaTrader expert advisor inside StockSharp. It searches for oversold or overbought
conditions with the Chaikin oscillator while price pierces a moving average and uses the previous candle body to confirm the
reversal. The StockSharp port keeps the discretionary hedging logic from the original code: when an opposite signal appears while
already in a position, the strategy immediately reverses the net exposure to follow the new bias.

## Indicators and data
- **Chaikin oscillator**: built from an Accumulation/Distribution line smoothed by two moving averages. Both averages are
  configurable and match the MetaTrader implementation (simple, exponential, smoothed, or weighted).
- **Price moving average**: configurable length, shift and type. It acts as the mean-reversion anchor that previous-candle highs
  or lows must pierce.
- **Timeframe**: the strategy subscribes to a single candle stream chosen through the `CandleType` parameter.

## Trading logic
1. Work only with finished candles. The previous candle body (close vs. open) provides the directional bias.
2. Read the Chaikin oscillator value from the previous candle. Large negative values signal oversold, large positive values mark
   overbought zones.
3. Require the previous candle to pierce the current moving average value (`Low < MA` for bullish setups and `High > MA` for
   bearish ones).
4. Enter when no position is open:
   - **Long**: previous candle bullish, low below MA, Chaikin below `-OpenLevel`.
   - **Short**: previous candle bearish, high above MA, Chaikin above `OpenLevel`.
5. When a position exists and an opposite setup appears, the algorithm reverses the net position (`SellMarket` / `BuyMarket` with
   extra volume) to mirror the hedging behaviour of the MT5 version.
6. Stops and targets are emulated inside the strategy using candle highs/lows, because StockSharp works with net positions rather
   than individual hedged tickets.

## Risk management
- **Stop-loss & take-profit**: distances in pips converted through the instrument price step. Both can be disabled with zero.
- **Breakeven**: once price moves by `BreakevenPips` in favour, the stop is moved to the entry price.
- **Trailing**: after the move exceeds `BreakevenPips + TrailingStopPips`, the stop follows price at the trailing distance.
- **Position state reset**: whenever an exit happens, all internal price levels are cleared to prepare for the next trade.

## Parameters
| Name | Description |
| ---- | ----------- |
| `OpenLevel` | Chaikin magnitude required to open a new position (default 100). |
| `CloseLevel` | Chaikin magnitude required to reverse an existing position (default 125). |
| `StopLossPips` | Stop-loss distance in pips (default 65). |
| `TakeProfitPips` | Take-profit distance in pips (default 100). |
| `TrailingStopPips` | Trailing distance in pips (default 30). |
| `BreakevenPips` | Gain in pips before moving the stop to break-even (default 15). |
| `MaPeriod` | Moving average length for the price filter (default 20). |
| `MaShift` | Bars to shift the moving average (default 0). |
| `MaType` | Moving average type (Simple, Exponential, Smoothed, Weighted). |
| `ChaikinFastPeriod` | Fast smoothing length in the Chaikin oscillator (default 3). |
| `ChaikinSlowPeriod` | Slow smoothing length in the Chaikin oscillator (default 10). |
| `ChaikinMaType` | Moving average type used for Chaikin smoothing. |
| `CandleType` | Candle series used for calculations. |

## Notes
- Configure the base `Volume` property in StockSharp to control trade size.
- Pips are calculated using the instrument `PriceStep`. If the step corresponds to 3 or 5 decimal quotes (e.g., 0.00001), the
  strategy multiplies it by 10 to match MetaTrader pip spacing.
- Because StockSharp uses net positions, hedge orders from the original MQL expert advisor are represented as immediate reversals
  of the existing position.
