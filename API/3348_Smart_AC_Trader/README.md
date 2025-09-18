# Smart AC Trader Strategy

## Overview
Smart AC Trader adapts the original MetaTrader "Smart AC Trader" idea to StockSharp's high level API. The MQL expert evaluated the relative strength of the currencies inside a pair and reacted when the base currency outperformed the quote currency. In StockSharp we focus on the same momentum-driven behaviour but operate on a single instrument that the strategy is attached to. Strength is approximated through a combination of exponential moving averages (EMAs) and the rate of change (ROC) indicator:

- A fast EMA measures short-term trend direction.
- A slow EMA represents the primary trend.
- ROC confirms that price momentum aligns with the trend before entries are allowed.

Once a position is opened, the strategy actively manages the trade using stop-loss, take-profit, trailing stop and break-even rules that mirror the extensive money management configuration of the original expert.

## Trading Logic
1. Subscribe to the configured candle type (time-frame) and calculate the fast EMA, slow EMA and ROC on the candle close.
2. Enter a long position when the fast EMA is above the slow EMA and ROC is greater than or equal to the buy momentum threshold. Existing short exposure is closed before the new long is opened.
3. Enter a short position when the fast EMA is below the slow EMA and ROC is less than or equal to the negative sell momentum threshold. Existing long exposure is closed before the new short is opened.
4. Manage an open position on every finished candle:
   - Close the trade at the configured take-profit or stop-loss distances (expressed in price steps).
   - Optionally arm a break-even exit once price moves in favour of the trade by the trigger distance and liquidate if price returns to the preserved offset.
   - Optionally trail the stop by the configured distance from the highest high (long) or lowest low (short) observed after entry.

## Parameters
| Parameter | Description |
|-----------|-------------|
| **Fast EMA** | Length of the fast EMA trend filter. |
| **Slow EMA** | Length of the slow EMA trend filter. |
| **ROC Period** | Lookback window for the rate of change momentum filter. |
| **Buy Momentum** | Minimum positive ROC required to open long trades. |
| **Sell Momentum** | Minimum absolute negative ROC required to open short trades. |
| **Stop Loss** | Stop-loss distance expressed in price steps. |
| **Take Profit** | Take-profit distance expressed in price steps. |
| **Use Trailing** | Enables trailing stop management. |
| **Trailing** | Trailing stop distance in price steps. |
| **Use Break Even** | Enables the break-even protection logic. |
| **Break Even Trigger** | Profit in price steps required to arm the break-even logic. |
| **Break Even Offset** | Distance in price steps kept after the break-even trigger is hit. |
| **Candle Type** | Candle type used to feed the indicators. |

## Notes
- The strategy uses `Strategy.StartProtection()` once at start-up to ensure the built-in position protection system is active as recommended by the project guidelines.
- Position sizing relies on the base `Strategy.Volume` property. Reversal orders automatically include the current exposure so that an opposite signal both closes the existing position and establishes a new one.
- All risk parameters are expressed in price steps because the original expert advisor used pip-based distances. Make sure the instrument has a valid `PriceStep` configured.
