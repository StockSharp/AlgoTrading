# Flat Trend Strategy

## Overview
The **Flat Trend Strategy** reproduces the core ideas of the original Flat Trend expert advisor by combining multi-speed trend filters, ADX confirmation and a standard deviation "juice" breakout filter. The strategy focuses on detecting the moment when price leaves a ranging phase and momentum expands, allowing it to join directional moves with dynamic position protection.

## Trading Logic
1. **Trend filters** – three exponential moving averages (EMAs) with configurable lengths represent the trigger, first filter and second filter. Their slope and the price position relative to each EMA are translated into states:
   - Strong bullish (price above EMA and EMA rising).
   - Moderate bullish (price above EMA but slope neutral).
   - Strong bearish (price below EMA and EMA falling).
   - Moderate bearish (price below EMA but slope neutral).
2. **Entry rules**
   - Long trades require bullish states on the trigger and filter EMA. The second filter can be optionally ignored. Strict mode forces the use of only strong bullish states.
   - Short trades mirror the conditions for bearish states.
   - Optional ADX confirmation ensures that the Average Directional Index exceeds a threshold and, when enabled, the +DI and –DI components agree with the trade direction.
   - The "juice" filter verifies that the standard deviation of prices is above a user-defined breakout level, preventing trades during flat volatility phases.
   - Trading can be restricted to a selected intraday window.
3. **Exit rules**
   - Opposite trend states on the trigger EMA initiate an exit. In strict mode the strategy waits for the strongest counter-signal.
   - Dynamic stops exit positions whenever price touches the calculated stop level.

## Risk Management
- **Initial stop** – calculated either from a static pip distance or from the Average True Range (ATR) value, emulating the ADR-based logic of the original EA.
- **Trailing stop** – moves with the highest (or lowest) price since entry using the ATR multiplied by a divisor.
- **Break-even** – once price advances by the configured distance, the stop moves beyond the entry price by a small lock value.

## Parameters
| Name | Description |
| --- | --- |
| `TriggerLength` | EMA length for the trigger filter. |
| `FilterLength1` | EMA length for the first confirmation filter. |
| `FilterLength2` | EMA length for the second confirmation filter. |
| `UseOnlyPrimaryIndicators` | Use only trigger and first filter for entries. |
| `IgnoreModerateForEntry` | Require strong trend states for new trades. |
| `IgnoreModerateForExit` | Require strong counter-signals to close trades. |
| `UseTradingHours` | Enable intraday trading window. |
| `TradingHourBegin` / `TradingHourEnd` | Start and end hour of the trading window. |
| `UseJuiceFilter`, `JuicePeriod`, `JuiceThreshold` | Standard deviation breakout filter parameters. |
| `UseAdxFilter`, `AdxPeriod`, `AdxThreshold`, `UseDirectionalFilter` | ADX strength and DI confirmation. |
| `UseAdrForStop`, `StopLossPips` | Initial stop-loss configuration. |
| `TrailingDivisor` | ATR multiplier for trailing stop calculation. |
| `BreakEvenPips`, `BreakEvenLockPips` | Break-even activation and lock distance. |
| `AtrPeriod` | ATR lookback used for volatility estimation. |
| `CandleType` | Primary candle timeframe. |

## Indicator Summary
- **Exponential Moving Average (EMA)** – three instances for multi-speed trend assessment.
- **Standard Deviation** – models the "juice" volatility breakout filter.
- **Average True Range (ATR)** – measures volatility for stops and trailing.
- **Average Directional Index (ADX)** – confirms trend strength and direction.

## Usage Notes
1. Ensure the strategy security has a defined `PriceStep`; otherwise the default step of 0.0001 is used for pip-based distances.
2. The strategy uses market orders (`BuyMarket`, `SellMarket`) and automatically scales volume when reversing positions.
3. Dynamic stops are simulated internally by closing positions when the virtual stop level is touched.
4. Combine the trading window and strict entry options to focus on high-liquidity sessions and avoid choppy periods.
