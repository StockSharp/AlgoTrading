# Blau Ergodic MDI Time Strategy

## Overview

The **Blau Ergodic MDI Time Strategy** is a direct conversion of the MetaTrader expert `Exp_BlauErgodicMDI_Tm.mq5` to StockSharp. It operates on higher-timeframe candles and reproduces the three signal modes of the original algorithm: **Breakdown**, **Twist**, and **CloudTwist**. The strategy relies on a multi-stage exponential moving average (EMA) smoothing process applied to a selected candle price. All calculations are performed inside the strategy without additional indicators so that the logic matches the MetaTrader expert while remaining compatible with the high-level StockSharp API.

The smoothing pipeline follows the logic of the Blau Ergodic MDI oscillator:

1. Smooth the chosen price with an EMA (length `BaseLength`).
2. Subtract the smoothed value from the raw price to obtain a difference series.
3. Apply three consecutive EMAs to the difference (lengths `FirstSmoothingLength`, `SecondSmoothingLength`, `ThirdSmoothingLength`).
4. Scale the intermediate (`histogram`) and final (`signal`) outputs by the instrument price step. These values drive the trading signals.

## Signal Modes

### Breakdown

* Uses the histogram two bars back (controlled by `SignalBar`).
* When the previous histogram value is positive and the selected bar moves to non-positive territory, the strategy prepares a long entry and optionally closes short positions.
* When the previous histogram value is negative and the selected bar rises to non-negative territory, the strategy prepares a short entry and optionally closes long positions.

### Twist

* Compares the histogram slope over two historical bars.
* If the histogram accelerates upward (bar `SignalBar + 1` < bar `SignalBar + 2`) and the most recent selected bar is above the previous one, a long entry signal is generated. Short positions can be closed in the same block.
* If the histogram accelerates downward (bar `SignalBar + 1` > bar `SignalBar + 2`) and the most recent selected bar is below the previous one, the strategy prepares a short entry and may close long positions.

### CloudTwist

* Uses both the histogram and the additional smoothed line.
* When the previous histogram stays above the signal line but the selected bar moves beneath it, a long entry is prepared and short positions can be closed.
* When the previous histogram is below the signal line but the selected bar crosses above it, the strategy prepares a short entry and can exit long positions.

## Time Window Filter

The original expert restricts trading to a configurable session. The StockSharp version replicates the same rules via parameters `UseTimeFilter`, `StartHour`, `StartMinute`, `EndHour`, and `EndMinute`. The session logic supports windows that cross midnight, identical to the MetaTrader implementation:

* If the start time is earlier than the end time, the session stays within one day.
* If the start time equals the end time, the minutes define a shorter interval during that hour.
* If the start time is later than the end time, the session wraps over midnight.

Whenever trading is disabled by the session filter, the strategy flat-closes any open position and blocks new entries until the session re-opens.

## Risk Management

The parameters `StopLossPoints` and `TakeProfitPoints` mirror the stop-loss and take-profit distances of the expert. Distances are expressed in price steps. The strategy recalculates the protective prices whenever a new position is opened. Each finished candle checks whether the bar’s range touched any protective level and immediately closes the position if triggered.

## Price Inputs

The `PriceMode` parameter exposes the same list of price sources as the original indicator:

| Mode | Description |
| ---- | ----------- |
| Close | Close price. |
| Open | Open price. |
| High | High price. |
| Low | Low price. |
| Median | (High + Low) / 2. |
| Typical | (High + Low + Close) / 3. |
| Weighted | (High + Low + 2 × Close) / 4. |
| Simple | (Open + Close) / 2. |
| Quarter | (Open + High + Low + Close) / 4. |
| TrendFollow0 | High on bullish candles, Low on bearish candles, Close on neutral candles. |
| TrendFollow1 | Average of Close with the candle extreme in the trend direction. |
| Demark | Demark price (weighted by candle direction). |

## Parameters

| Parameter | Default | Description |
| --------- | ------- | ----------- |
| `Mode` | Twist | Selects Breakdown, Twist, or CloudTwist signal evaluation. |
| `PriceMode` | Close | Price source used for the oscillator. |
| `BaseLength` | 20 | EMA length applied to the raw price. |
| `FirstSmoothingLength` | 5 | EMA length of the first difference smoothing. |
| `SecondSmoothingLength` | 3 | EMA length of the second difference smoothing. |
| `ThirdSmoothingLength` | 8 | EMA length of the third difference smoothing. |
| `SignalBar` | 1 | Number of completed bars back used for signal checks (1 matches MetaTrader default). |
| `AllowLongEntry` / `AllowShortEntry` | true | Enable or disable long/short entries. |
| `AllowLongExit` / `AllowShortExit` | true | Enable or disable exits for the corresponding side. |
| `UseTimeFilter` | true | Activates the trading session filter. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | 0/0/23/59 | Session boundaries. |
| `StopLossPoints` | 1000 | Stop-loss distance in price steps (0 disables). |
| `TakeProfitPoints` | 2000 | Take-profit distance in price steps (0 disables). |
| `CandleType` | 4h timeframe | Candle subscription used for calculations. |
| `Volume` | 0.1 | Order volume, matching the `MM` input of the expert. |

## Trading Rules Summary

1. Subscribe to the configured timeframe candles.
2. On each finished candle, update the four-stage EMA pipeline and store the histogram and signal values in rolling buffers.
3. Wait until the minimum history depth is reached (matching the original `min_rates_total` calculation).
4. Evaluate the selected mode using bar `SignalBar` and older values to set open/close flags.
5. Close positions first if the corresponding exit flag is raised or if the time filter blocks trading.
6. Open new long or short trades only when the respective flag is set, the time filter allows trading, and the current position does not already point in the same direction. When reversing, the strategy automatically sizes the order to cover the existing exposure plus the configured volume.
7. Maintain protective stops and targets using candle extremes to detect triggers.

## Usage Notes

* The strategy uses tabs for indentation, consistent with project guidelines.
* It calls `StartProtection()` once during start-up to keep StockSharp safety features aligned with position changes.
* Indicator values are stored only for the minimal number of bars required by the signals. No large collections are created, following repository instructions.
* To experiment with other smoothing methods from the MetaTrader version, adjust the EMA lengths accordingly. The EMA-based pipeline provides the closest approximation supported by StockSharp’s high-level API.

## Running the Strategy

1. Add the strategy class to your StockSharp solution and compile the project.
2. Configure the parameters (instrument, candle timeframe, mode, session, and risk settings).
3. Attach the strategy to a connector that provides the required market data.
4. Start the strategy; it will automatically subscribe to the configured candles and manage orders according to the rules above.

