# Lbs V12 Strategy

## Overview
The Lbs V12 strategy is a conversion of the MetaTrader expert advisor **LBS_V12.mq4**. It opens a pair of breakout stop orders around the previous 15-minute candle when the configured trigger hour begins. Both orders are offset by the current Average True Range (ATR) value in order to account for short-term volatility. The strategy attempts to catch the first impulse of the trading session and manages exits through virtual stop-loss, take-profit and trailing rules evaluated on every finished candle.

## Trading Logic
1. The strategy monitors finished candles of the selected timeframe (15 minutes by default).
2. When a new candle with minute `00` appears at the configured `TriggerHour`, the previous candle becomes the reference range.
3. If there are no open positions and no working orders for the current day, two stop orders are sent:
   - **Buy stop** above the reference high plus the instrument spread, one price step and the latest ATR value.
   - **Sell stop** below the reference low minus the same buffers.
4. Protective price levels for each side are stored internally:
   - Stop-loss is placed beyond the opposite extreme of the reference candle.
   - Take-profit is calculated using the MetaTrader-style point distance.
   - A trailing stop activates once the trade moves further than the configured distance.
5. When a long or short position is opened, the opposite stop order is cancelled. All protection is applied virtually: candle highs and lows are compared against the stored stop/take values and the position is closed with market orders when limits are reached.
6. The strategy runs only once per day. All pending orders and internal state are cleared at the start of a new trading date.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `Volume` | Trading volume in lots. | `1` |
| `TriggerHour` | Hour of the day (terminal time zone) when the breakout orders should be sent. | `9` |
| `TakeProfitPoints` | MetaTrader-style points between the entry price and the take-profit target. | `100` |
| `TrailingStopPoints` | MetaTrader-style points used for the trailing stop after the trade moves into profit. | `20` |
| `AtrPeriod` | Period of the ATR indicator that offsets the pending orders. | `3` |
| `CandleType` | Candle type used for signal calculations. The default is 15-minute time frame candles. | `15m timeframe` |

## Risk Management
- Exits are executed through market orders when the candle extremes touch the virtual stop-loss or take-profit levels.
- The trailing stop increases (for longs) or decreases (for shorts) the protective level whenever the trade gains more than the configured distance.
- Daily reset ensures that the strategy does not accumulate multiple positions or outdated pending orders.

## Notes
- Accurate bid/ask updates improve the spread compensation that is added to the breakout prices. If spread data is not available, the strategy falls back to one price step.
- The conversion keeps the original MetaTrader defaults but adapts take-profit handling for short positions so that the target is always placed in the profitable direction.
