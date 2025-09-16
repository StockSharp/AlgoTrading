# CH2010 Structure Multi-Timeframe Breakout Strategy

This strategy replicates the behaviour of the original **ch2010structure.mq5** expert by tracking multiple forex pairs on two timeframes. Each instrument monitors the daily candle to determine a directional bias and then watches 30-minute candles to look for breakouts beyond the prior daily range. Market positions are opened when the breakout aligns with the daily trend and closed using protective stop-loss and take-profit levels.

## Core Logic

1. **Daily bias detection**  
   * The strategy subscribes to daily candles for USDCHF, GBPUSD, AUDUSD, USDJPY and EURGBP.  
   * When a daily candle finishes the close/open relationship defines the bias: bullish, bearish or neutral.  
   * The daily high, low and close are stored together with the session date so intraday logic can confirm it is trading the same session.

2. **Intraday breakout execution**  
   * 30-minute candles are evaluated once they close.  
   * If the close is above the previous daily high plus a configurable buffer and the bias is not bearish, a long trade is triggered.  
   * If the close is below the previous daily low minus the buffer and the bias is not bullish, a short trade is triggered.  
   * Only one long and one short breakout can be activated per instrument each day to avoid over-trading.

3. **Risk management inspired by the original helper functions**  
   * Volumes are clamped between `MinTradeVolume` and `MaxTradeVolume` and the aggregated position across all instruments is restricted by `MaxAggregateVolume`.  
   * Each filled position immediately calculates absolute stop-loss and take-profit levels using percentage offsets from the entry price.  
   * Positions are closed via market orders as soon as the stop or target is reached; repeated exit orders are prevented by the `ExitInProgress` flag.

4. **State tracking**  
   * For every instrument the strategy tracks its own daily levels, last known position, entry side, exit orders and breakout flags in an `InstrumentContext`.  
   * This allows the multi-symbol workflow without having to maintain custom collections outside of the context class.

## Strategy Parameters

| Parameter | Description |
| --- | --- |
| `TradeVolume` | Base volume used for new entries, subject to the volume limits. |
| `MinTradeVolume` & `MaxTradeVolume` | Bounds that mirror the original MQL risk filter. |
| `MaxAggregateVolume` | Maximum sum of absolute positions across all traded pairs. |
| `StopLossPercent` | Protective stop offset in percent from the detected entry price. |
| `TakeProfitPercent` | Take-profit offset in percent from the detected entry price. |
| `BreakoutBufferPercent` | Percentage of the prior daily range added to breakout triggers. |
| `DailyCandleType` | DataType used to request the higher timeframe candles. |
| `IntradayCandleType` | DataType used to request the execution timeframe candles. |
| `UsdChfSecurity` .. `EurGbpSecurity` | Security objects for the five forex symbols monitored by default. |

## Required Data

* Daily candles for every configured symbol (default: 1-day time frame).  
* Intraday candles (default: 30-minute) for the same symbols.  
* Real-time order routing to submit market orders for each security.

## Usage Notes

1. Configure the five security parameters before starting the strategy. They can be replaced with other instruments if desired.  
2. Set the portfolio and connector as in other StockSharp strategies.  
3. Optionally adjust the breakout buffer or risk parameters to reflect the target broker's contract specifications.  
4. Start the strategy. It will automatically subscribe to both candle streams for each instrument, log the daily structure and wait for valid intraday breakouts.  
5. Monitor the log for entries such as `Daily candle captured` and `Enter Buy` to verify the decision flow.

## Differences vs. the Original MQL Expert

* Pending orders are replaced with immediate market orders once the breakout condition is observed. This keeps the logic compatible with the StockSharp high-level API while preserving the idea of limiting exposure and reacting only once per direction each day.  
* Volume restrictions from the `DebugOrderSend` helper were adapted into parameters that clamp single trade sizes and total exposure.  
* Extensive logging is added to show daily levels, entry reasons and exit triggers in English comments for easier debugging in StockSharp.

## Disclaimer

This example is intended for educational purposes. Parameters and securities should be reviewed and adjusted before using the strategy in production trading.
