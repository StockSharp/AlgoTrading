# Auto Trading Publish Strategy

## Overview
This strategy ports the MetaTrader 4 utility **"Auto Trading Publish"** to StockSharp. Instead of submitting market orders, the
strategy focuses on controlling when trading is allowed. It monitors the market clock via a candle subscription and flips the
`AutoTradingActive` flag whenever the configured start or stop hour is reached. The flag mirrors the behaviour of the original
utility which programmatically toggled the MT4 "AutoTrading" button.

## Trading logic
- Subscribe to a lightweight candle stream (one-minute candles by default) to keep track of market time even if no trades are
done.
- When a finished candle reports the configured `StartHour`, enable the `AutoTradingActive` flag and log the event.
- When a finished candle reports the configured `StopHour`, disable the `AutoTradingActive` flag and log the event.
- Suppress duplicate toggles inside the same hour so the log does not flood when multiple candles or ticks arrive during that
hour.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `StartHour` | Hour of the day (0-23) that enables auto trading. |
| `StopHour` | Hour of the day (0-23) that disables auto trading. |
| `CandleType` | Timeframe used to poll the market clock. Smaller frames react faster. |

## Usage notes
- The strategy does not send orders; it only exposes the `AutoTradingActive` property, which other strategies or control panels
can observe to decide when to submit trades.
- When the start and stop hour are the same, the stop event runs after the start event, leaving trading disabled—identical to the
original expert advisor.
- Choose a candle timeframe that matches how quickly you need the toggle to happen. A one-minute timeframe is a good balance
between responsiveness and resource usage.

## Differences vs. MetaTrader version
- MT4 toggled a global platform button through Windows messages. StockSharp exposes a strategy-level flag instead, making the
behaviour easier to integrate with complex setups.
- The StockSharp port runs entirely within the high-level API, making it easy to combine with charting or other helper
strategies without low-level message hooks.
