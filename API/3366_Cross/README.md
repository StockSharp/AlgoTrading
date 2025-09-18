# Cross Strategy (MQL 27596 Conversion)

## Overview
The **Cross Strategy** is a direct conversion of the MetaTrader expert advisor `Cross.mq4` (repository entry `MQL/27596`). The original EA traded a single exponential moving average (EMA) cross measured on bar open prices and applied fixed-distance take profit and stop loss levels. This StockSharp port keeps the trading logic intact while using high-level API features such as candle subscriptions, indicator binding, and managed position tracking.

## Trading Logic
1. **Indicator** – a single Exponential Moving Average (EMA) calculated from candle close prices. The period is configurable and defaults to 200, matching the MQL source.
2. **Signal Detection** – on every finished candle, the strategy compares the candle open with the EMA value:
   - A **bullish signal** occurs when the candle opens above the EMA after previously opening at or below it. This reproduces the `Cross(0, Open[0] > EMA)` call in the MQL script.
   - A **bearish signal** occurs when the candle opens below the EMA after previously opening at or above it (`Cross(1, Open[0] < EMA)` in the original code).
3. **Position Management** – when a signal fires, the strategy fully reverses the current position:
   - If a bullish cross appears while flat or short, it buys enough volume to cover the short exposure and open a new long position.
   - If a bearish cross appears while flat or long, it sells enough volume to flatten the long exposure and establish a short position.
4. **Risk Control** – after entering a position, the strategy monitors candle highs and lows to implement fixed take profit and stop loss exits in price-step units. These exits emulate the `OrderSend` calls that set both `TakeProfit` and `StopLoss` in MetaTrader.

## Parameters
| Parameter | Default | Description |
| --- | --- | --- |
| `EMA Length` | 200 | Period of the EMA used for cross detection. Must be greater than zero. |
| `Take Profit (steps)` | 200 | Distance to the take profit level measured in price steps. Set to zero to disable the profit target. |
| `Stop Loss (steps)` | 100 | Distance to the protective stop measured in price steps. Set to zero to disable the stop. |
| `Candle Type` | 1-minute time frame | Candle data source processed by the strategy. You can switch to other time frames or custom candle types supported by StockSharp. |

The traded volume is controlled by the strategy's `Volume` property. When a reversal signal arrives, the strategy sends `Volume + |Position|` to ensure the existing exposure is closed before opening the new position.

## Execution Flow
1. `OnStarted` subscribes to the configured candle series and binds the EMA indicator using the high-level `Bind` helper.
2. The handler skips unfinished candles and waits until the EMA becomes fully formed. Once ready, it:
   - Manages the active position by checking stop loss and take profit levels against the candle's high/low values.
   - Detects bullish and bearish crosses based on the candle open price relative to the EMA.
   - Issues market orders to reverse the position when a new signal appears.
3. `OnNewMyTrade` tracks the average entry price and direction of the active position so that exit checks use precise levels even when scaling into trades.
4. Optional chart objects are created (if a chart is available) to display candles, the EMA line, and executed trades.

## Risk Management Details
- **Stop Loss** – computed as `entry price ± stop steps × price step` depending on direction. The strategy exits immediately when the candle low (long) or high (short) breaches the stop level.
- **Take Profit** – computed similarly using the configured profit steps. Hitting the target closes the entire position during the candle where the high/low crosses the threshold.
- **Account Protection** – `StartProtection()` is invoked once at startup so the strategy respects any global protection rules configured in StockSharp environments.

## Customisation Tips
- Shorter time frames or EMA lengths create more frequent reversals. Combine with increased stop distances to avoid whipsaws.
- To trade multiple symbols, instantiate separate strategy instances with their own securities and candle types.
- When optimising, keep the EMA length and stop/take distances within realistic bounds for the instrument's volatility and tick size.

## Conversion Notes
- The MQL array `crossed[2]` is mapped to two internal boolean flags that persist across candles.
- The MQL `OrderSend` function is represented by StockSharp's `BuyMarket` and `SellMarket` helpers, ensuring both reversal and new entries mirror the original behaviour.
- EMA values are supplied through the bind callback, avoiding direct `GetValue` calls as required by the repository guidelines.

By following these details you can reproduce the original MetaTrader strategy within StockSharp while retaining full control over data sources, parameter optimisation, and charting.
