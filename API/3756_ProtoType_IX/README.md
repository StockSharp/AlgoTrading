# ProtoType IX Strategy

## Overview
ProtoType IX is a multi-filter trend following strategy converted from the original MetaTrader 4 expert advisor. The algorithm observes Williams %R swings to detect fresh impulsive moves and validates them with Average True Range (ATR) expansion. Trades are opened only when the projected reward-to-risk ratio is attractive enough and the breakout is confirmed.

## Indicators and Signals
- **Williams %R (Period configurable)** – monitors overbought/oversold rotations. The strategy records the two most recent swing highs and swing lows that appear when the indicator leaves its extreme zones.
- **Average True Range (ATR)** – measures current volatility. Breakouts are considered valid when the distance between the latest and previous swing exceeds `ATR × multiplier`.

## Entry Rules
1. Wait for both recent swing highs and lows to be recorded.
2. Determine the Williams %R direction. If the indicator is above the upper threshold the bullish bias is stored; if it is below the lower threshold the bearish bias is stored.
3. Confirm the swing structure with ATR:
   - Bullish trend – the latest swing high must exceed the previous swing high by at least `ATR × multiplier` and the latest swing low must be higher than the previous swing low.
   - Bearish trend – the latest swing low must drop below the previous swing low by at least `ATR × multiplier` and the latest swing high must be lower than the previous swing high.
4. Evaluate the reward/risk ratio using current close price:
   - **Long**: target = max(last swing high, previous swing high); stop = max(last swing low, previous swing low).
   - **Short**: target = min(last swing low, previous swing low); stop = min(last swing high, previous swing high).
5. Only open a position when `take profit distance / stop loss distance ≥ TP/SL criteria` and the target distance is larger than the minimum spread requirement.

## Exit Rules
- Initial protective orders are placed immediately after entry. Stop-loss and take-profit levels are converted into price steps to use StockSharp protective orders.
- After the configured `Zero Bar` delay expires, the stop-loss is tightened using an ATR based trailing model:
  - Long positions trail the stop to `max(previous stop, close − 2 × ATR)`.
  - Short positions trail the stop to `min(previous stop, close + 2 × ATR)`.

## Position Sizing
The lot size is estimated from the portfolio value and the `Risk %` parameter. The stop-loss distance in price steps is used to translate the allowed monetary risk into volume. Volumes are normalized to the instrument volume step and capped by `Max Order Size`.

## Parameters
| Name | Description |
| --- | --- |
| Williams %R Period | Length of the Williams %R indicator. |
| Criteria WPR | Absolute threshold defining overbought/oversold zones. |
| ATR Period | Length of the Average True Range indicator. |
| ATR Multiplier | Multiplier applied to ATR for breakout validation. |
| Zero Bar | Number of bars before enabling ATR trailing. |
| Min Target Spread | Minimal acceptable target distance expressed in spread multiples. |
| TP/SL Criteria | Minimal take-profit / stop-loss ratio required to enter a trade. |
| Max Orders | Maximum simultaneously opened orders. |
| Max Order Size | Upper bound for order volume after sizing. |
| Risk % | Risk percentage used for position sizing. |
| Candle Type | Candle data type for calculations. |

## Notes
- The strategy focuses on a single security but keeps the multi-filter logic of the original EA.
- Protective orders rely on the instrument price step; ensure the instrument metadata is configured before running the strategy.
- Zero values for volume step or step price are substituted with reasonable defaults to keep the sizing routine stable.
