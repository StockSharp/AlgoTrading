# Tokyo Session Strategy

## Overview

The Tokyo Session Strategy replicates the logic of the MetaTrader expert advisor *TokyoSessionEA_v2.8* in StockSharp. The
strategy is designed for intraday breakout or mean-reversion trading around the Asian (Tokyo) session. It captures a
reference candle at a configurable hour, builds a price channel from that candle, and evaluates breakout or rebound
conditions at another target hour. Depending on the chosen signal mode, the strategy can trade either contrary to the
level breakout (fade moves that extend beyond the reference range) or along the breakout direction.

The StockSharp port focuses on high-level API usage. All signal calculations are performed inside the candle subscription
handler, stops are managed through `StartProtection`, and every action is logged through `LogInfo` to keep the behaviour
transparent during backtests and live trading.

## Trading Logic

1. **Reference candle** – at `TimeSetLevels` (broker hour) the strategy records the candle high, low and close. These
   values define the session channel and reset the internal validation flags.
2. **Channel validation** – every finished candle between the reference hour and the entry hour can invalidate the
   pending signal depending on configuration:
   - `CheckAllBars`: if enabled, the close must remain between the captured high and low.
   - `ReCheckPrices`: at `TimeRecheckPrices` the candle close is compared with the running average to confirm momentum.
3. **Entry evaluation** – when the candle that precedes `TimeCheckLevels` closes, the strategy compares its close price
   with the channel borders. If the close is inside the configured distance range the corresponding position is opened.
4. **Exits** – positions can be closed by three mechanisms:
   - `CloseInSignal` closes a trade once price returns inside the channel (logic mirrors the original EA).
   - `CloseOrdersOnTime` flattens at `TimeCloseOrders` to avoid holding risk into the next session.
   - Protective stops, trailing stops and break-even handling are delegated to the StockSharp protection subsystem.

## Parameters

### General

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle series used for analysis (defaults to H1). |
| `BrokerOffset` | Difference between broker time and GMT in hours. |

### Signals

| Parameter | Description |
|-----------|-------------|
| `TypeOfSignals` | `ContraryTrend` replicates fading the breakout; `AccordingTrend` follows the breakout direction. |
| `TimeSetLevels` | Hour (0–23) when the reference candle is captured. |
| `TimeCheckLevels` | Hour when breakout conditions are evaluated. |
| `TimeRecheckPrices` | Additional momentum check hour. |
| `MinDistanceOfLevel` | Minimum distance (in pips) between the close and the channel before allowing a trade. |
| `MaxDistanceOfLevel` | Maximum distance (in pips) from the level. Zero disables the limit. |
| `ReCheckPrices` | Enables/disables the additional momentum filter. |
| `CheckAllBars` | Requires all intermediate closes to stay within the channel. |

### Risk Management

| Parameter | Description |
|-----------|-------------|
| `CloseInSignal` | Exit once price crosses back through the channel boundary. |
| `CloseOrdersOnTime` | Flatten positions after `TimeCloseOrders`. |
| `TimeCloseOrders` | Hour used by the time-based exit. |
| `UseTakeProfit`, `TakeProfit` | Enable and configure a fixed take-profit (pips). |
| `UseStopLoss`, `StopLoss` | Enable and configure a protective stop-loss (pips). |
| `UseTrailingStop`, `TrailingStop`, `TrailingStep` | Enable StockSharp trailing stop management (pips). |
| `UseBreakEven`, `BreakEven`, `BreakEvenAfter` | Move stop-loss to break-even once profit reaches the trigger distance. |

### Trading

| Parameter | Description |
|-----------|-------------|
| `Volume` | Base order volume. When flipping direction the opposite position is closed automatically. |
| `MaxOrders` | Maximum number of `Volume` blocks allowed in one direction. Set to 0 for no limit. |

## Workflow

1. Deploy the strategy on an instrument with a valid price step (`Security.PriceStep`).
2. Select the desired timeframe and configure the broker hour offsets to align the daily schedule with the exchange.
3. Tune the distance and validation filters to match the behaviour of the original EA or to adapt to different markets.
4. Configure risk parameters. The StockSharp port natively manages stops and trailing logic through `StartProtection`.
5. Start the strategy. Logging messages will report the captured levels, opened trades and exit decisions.

## Differences from the MetaTrader Version

- Floating-point entries based on `UseFloatingPoint` and `PipsFloatingPoint` are not implemented because StockSharp
  executes market orders at the time the signal is generated.
- Spread and slippage filters are omitted because high-level candle subscriptions do not provide tick-level bid/ask data.
- Automatic money management (`AutoLotSize`, `RiskFactor`, recovery lots, preset symbol switching) is replaced with the
  simpler `Volume` and `MaxOrders` parameters. Position sizing should be adjusted directly in the strategy settings.
- Sound and print notifications are replaced by `LogInfo` messages.

All other signal conditions, validation gates and time-based exits mirror the behaviour of the original EA.

## Notes

- The default configuration is aligned with the H1 timeframe recommended by the original expert advisor. Other timeframes
  can be used, but the hour-based logic assumes that candle durations divide an hour evenly.
- Ensure that the data feed delivers continuous candles for the selected timeframe. Missing candles may invalidate the
  average and channel checks.
- Because the strategy closes positions by sending market orders, brokers that require limit orders or minimum holding
  times may need additional adaptations.
