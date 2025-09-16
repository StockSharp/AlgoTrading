# EMA WMA Crossover Strategy

## Overview
- Conversion of the MetaTrader 4 "EMA WMA" expert advisor by Vladimir Hlystov.
- Trades trend reversals detected from the relationship between an exponential moving average (EMA) and a weighted moving average (WMA) calculated on candle **open** prices.
- Automatically attaches stop-loss and take-profit orders identical to the MT4 robot by using StockSharp's protection helper.
- Supports risk-based position sizing that mirrors the original "risk" input while keeping an option for fixed volume trading.

## Original Expert Advisor Logic
- The MT4 version works on any symbol and timeframe, evaluating signals once on a new bar (guarded by `TimeBar`).
- Indicators use `PRICE_OPEN`, so the averages react to the bar opening tick.
- When EMA falls below WMA while previously being above it, all short positions are closed and a long trade is opened with predefined stop-loss and take-profit distances.
- When EMA rises above WMA after being below it, all long positions are closed and a new short position is opened.
- The `risk` input computes the lot size from available margin and the stop-loss distance.

## Trading Rules in StockSharp
1. Subscribe to the configured candle series (`CandleType`, default 30-minute). Only finished candles are processed to avoid repainting.
2. Feed candle open prices into EMA and WMA indicators. Wait until both indicators are formed.
3. Detect a bullish crossover when previous EMA > previous WMA and current EMA < current WMA.
   - Close any shorts and enter a long position sized by risk rules.
4. Detect a bearish crossover when previous EMA < previous WMA and current EMA > current WMA.
   - Close any longs and enter a short position sized by risk rules.
5. `StartProtection` creates market-protection orders so every new trade immediately receives stop-loss and take-profit levels expressed in price steps.

## Position Sizing and Risk Control
- **RiskPercent** emulates the MT4 `risk` parameter. Volume is computed from portfolio equity, stop-loss distance and security step/step-price values.
- If exchange metadata is missing (no price step or step price) the algorithm falls back to using the absolute stop distance.
- If `RiskPercent` is set to zero the strategy requires a positive **OrderVolume** (fixed volume override).
- Existing opposite exposure is closed before new orders are sent, matching the MT4 behaviour of `CLOSEORDER` then `OPENORDER`.

## Parameters
| Name | Description |
| --- | --- |
| `EmaPeriod` | Period of the exponential moving average (default 28). |
| `WmaPeriod` | Period of the weighted moving average (default 8). |
| `StopLossPoints` | Stop-loss distance in instrument steps (default 50). |
| `TakeProfitPoints` | Take-profit distance in instrument steps (default 50). |
| `RiskPercent` | Percentage of equity to risk per trade (default 10%). |
| `OrderVolume` | Fixed order volume; use 0 to enable risk-based sizing. |
| `CandleType` | Candle data type/timeframe used for calculations. |

## Implementation Notes
- EMA and WMA values are pushed manually via `DecimalIndicatorValue` to ensure the open price is used exactly like the MT4 indicator configuration.
- The strategy relies on closed candles for signal confirmation; this can delay entries by one bar compared to MT4 but prevents look-ahead bias.
- Protective orders are expressed in price steps to match the `Point` multiplier from MetaTrader.
- Charts automatically plot candles, both moving averages and trade markers when a chart area is available.
