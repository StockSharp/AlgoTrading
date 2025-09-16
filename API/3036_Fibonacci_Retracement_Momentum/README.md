# Fibonacci Retracement Momentum Strategy

## Overview
The **Fibonacci Retracement Momentum Strategy** is a conversion of the original MetaTrader expert advisor "FIBONACCI.mq4" to the StockSharp high-level API. The strategy combines multi-timeframe Fibonacci retracement levels with momentum and MACD filters to time pullback entries in the direction of the prevailing trend. The primary trading logic is executed on the base timeframe, while confirmation data is derived from higher aggregation periods.

The algorithm was rewritten from scratch using StockSharp idioms: candle subscriptions, indicator bindings, and the built-in order management helpers. Trailing logic from the source EA was simplified to focus on the core retracement breakout behaviour, while preserving the original signal structure (Fibonacci touch + momentum surge + trend filter).

## How it works
1. **Primary timeframe** – the strategy subscribes to the selected base candles (15 minutes by default) and calculates two weighted moving averages (fast and slow) to assess local direction.
2. **Fibonacci anchor timeframe** – the higher timeframe (default: 1 hour) provides the most recent completed candle. Its high/low are used to construct the 0%–100% Fibonacci retracement grid. The same candle stream feeds a momentum indicator (lookback 14) and the absolute deviation from the neutral 100 level is stored for the last three bars.
3. **MACD filter timeframe** – a long-term MACD (default: 12/26/9) is calculated on monthly candles (30-day approximation) and acts as a trend confirmation filter.
4. On every finished base candle, the algorithm checks whether price retraced to any Fibonacci level while the previous closes stayed on the opposite side of that level. Combined with moving average alignment, momentum impulse, and MACD confirmation, a trade is opened.
5. Protective exits rely on stop-loss and take-profit distances expressed in price steps. If the price moves against the position or reaches the target, the position is flattened.

## Entry rules
### Long setup
- Last higher-timeframe candle defines Fibonacci levels; current base candle low touches or penetrates any level while at least one of the three previous closes stayed above it.
- Fast weighted moving average is above the slow weighted moving average on the base timeframe.
- Momentum deviation `|Momentum - 100|` on the higher timeframe exceeds the configured threshold for any of the last three values.
- MACD main line is above the signal line on the MACD timeframe.
- Structural check: the previous base candle’s high is above the low of two bars ago (mirrors `Low[2] < High[1]` from the EA).

### Short setup
- Current base candle high touches any Fibonacci level while at least one of the last three closes remained below it.
- Fast weighted moving average is below the slow weighted moving average.
- Momentum deviation surpasses the threshold for any of the last three readings.
- MACD main line is below the signal line on the MACD timeframe.
- Structural check: the previous candle’s high is above the low of the immediately preceding bar (`Low[1] < High[2]` analogue).

### Position management
- If an opposite signal appears while a position is open, the strategy first closes the existing position and waits for the next bar to initiate the reversal. This mirrors the conservative order handling of the original MQL code.

## Risk management
- **Stop loss / Take profit** – configured in multiples of the security’s price step. Zero disables the corresponding exit.
- **Entry price tracking** – the fill price is approximated by the close of the signal candle and is used to compute protective distances.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `FastMaLength` | 6 | Length of the fast weighted moving average on the base timeframe. |
| `SlowMaLength` | 85 | Length of the slow weighted moving average. |
| `MomentumLength` | 14 | Momentum lookback on the Fibonacci timeframe. |
| `MomentumThreshold` | 0.3 | Minimum absolute deviation from 100 required to validate momentum. |
| `StopLossSteps` | 20 | Stop-loss distance in price steps (0 disables). |
| `TakeProfitSteps` | 50 | Take-profit distance in price steps (0 disables). |
| `MacdFastLength` | 12 | Fast EMA length used inside MACD. |
| `MacdSlowLength` | 26 | Slow EMA length used inside MACD. |
| `MacdSignalLength` | 9 | Signal EMA length used inside MACD. |
| `CandleType` | 15-minute candles | Primary execution timeframe. |
| `FibonacciCandleType` | 1-hour candles | Timeframe supplying Fibonacci anchors and momentum. |
| `MacdCandleType` | 30-day candles | Timeframe supplying the MACD trend filter. |

## Usage notes
- Adjust the timeframe parameters to match the original EA mapping (e.g., M5 → M30, M15 → H1). StockSharp allows any candle type, including range or tick bars.
- Because the strategy uses `ClosePosition()` for flattening, the `Volume` property should match the desired trade size (default: 1 lot equivalent).
- The conversion focuses on indicator-driven logic; money management extras from the MQL version (equity stop, trailing by account balance, etc.) were intentionally omitted for clarity. You can extend the class with additional protection by hooking into `ManageRisk`.
- Run the strategy inside StockSharp Designer, Shell, or Runner with the required market data adapters configured.
