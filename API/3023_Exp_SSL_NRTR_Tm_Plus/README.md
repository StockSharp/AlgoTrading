# Exp SSL NRTR Tm Plus Strategy

## Overview

The strategy replicates the MetaTrader expert advisor "Exp_SSL_NRTR_Tm_Plus" using StockSharp high level infrastructure. It
subscribes to a single timeframe, calculates the SSL NRTR channel with a configurable smoothing method and reacts to the color
transitions provided by the indicator. Long entries are triggered when the channel turns bullish while short entries occur on
bearish transitions. The implementation preserves the original risk controls, optional trade filters and the timer-based exit.

## Parameters

| Group | Parameter | Description |
| --- | --- | --- |
| Trading | Money Management | Fraction of the portfolio (or direct lots when negative/`Lot` mode) used to size orders. |
| Trading | Margin Mode | Mode used to translate the money management value into a position size. Modes other than `Lot` are approximated with portfolio-based calculations. |
| Trading | Allow Long/Short Entries | Enable or disable opening positions in the respective direction. |
| Trading | Allow Long/Short Exits | Allow the strategy to close positions in the respective direction on indicator reversals. |
| Risk | Stop Loss | Protective stop distance in price steps. The strategy monitors the levels instead of placing native stop orders. |
| Risk | Take Profit | Take profit distance in price steps. |
| Risk | Slippage | Informational parameter kept from the original EA. |
| Risk | Use Time Exit | Enable the timer that forces a flat position after the configured holding period. |
| Risk | Exit Minutes | Holding period in minutes for the time-based exit. |
| Data | Candle Type | Working timeframe used for both trading and indicator calculations. |
| Indicator | Smoothing Method | Moving-average type used by the SSL NRTR channel. Unsupported custom types fall back to an EMA. |
| Indicator | Length | Base period of the smoothing algorithm. |
| Indicator | Phase | Auxiliary parameter used by adaptive averages (T3, VIDYA, AMA). |
| Indicator | Signal Bar | Number of closed bars to look back when evaluating SSL colors. |

## Trading Logic

1. Subscribe to the configured timeframe and process only finished candles.
2. Calculate the SSL NRTR moving averages and derive the channel color (up, down or neutral).
3. When the color switches to bullish (`0`), optionally close short positions and, if enabled, open a long position.
4. When the color switches to bearish (`2`), optionally close long positions and, if enabled, open a short position.
5. Track stop-loss/take-profit levels using the entry price and close the position when either level is reached.
6. Optionally close positions once the holding time exceeds the `Exit Minutes` parameter.
7. Prevent repeated entries within the same bar by throttling with the original MT5 "time level" logic.

## Money Management

- `Lot` mode treats the money management value as a direct volume expressed in lots/contracts.
- `FreeMargin` and `Balance` approximate the requested capital fraction by dividing it by the latest close price.
- `LossFreeMargin` and `LossBalance` estimate the tradable volume from the allowed loss per trade using the configured stop-loss distance.
- Negative money management values always map to an absolute lot size.

## Notes

- Only the smoothing methods available in StockSharp are implemented directly. `Jurx` and `Parma` fall back to the exponential moving average and this behaviour is documented in code comments.
- The strategy keeps stop-loss and take-profit logic inside the strategy loop instead of sending native protective orders to stay platform agnostic.
- Slippage is an informational setting retained for completeness; orders are sent as plain market orders.
- The implementation draws candles and own trades on the chart area by default.
