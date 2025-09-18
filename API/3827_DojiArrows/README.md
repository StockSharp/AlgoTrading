# Doji Arrows Strategy

## Overview
The **Doji Arrows Strategy** is a StockSharp port of the MetaTrader expert advisor `Doji_arrows_expert1.mq4`. The trading idea is to detect a neutral doji candle and immediately trade the breakout that follows on the next bar. When the market prints a very small body candle (open ≈ close) and the subsequent candle closes beyond the doji high or low, the strategy interprets the move as a directional breakout and enters in that direction.

## Trading logic
- **Signal detection window** – the strategy continuously buffers the two previously completed candles. The oldest candle must be a doji, while the more recent candle confirms the breakout.
- **Doji definition** – a candle qualifies as a doji when the absolute difference between open and close is less than or equal to `DojiBodyThresholdSteps * PriceStep`. With the default threshold of 1 step the bar may deviate by one tick at most.
- **Breakout confirmation** –
  - Long setup: the candle following the doji closes above the doji high plus the optional `BreakoutBufferSteps` filter.
  - Short setup: the candle following the doji closes below the doji low minus the same buffer.
- **Single-shot signaling** – the strategy remembers whether the previous bar already triggered a long or short signal and only reacts to a fresh breakout. This behaviour mirrors the original expert that generated one arrow per breakout sequence.
- **Order execution** –
  - If a breakout appears against an existing opposite position the strategy first closes it, then enters in the new direction with volume `Volume + |Position|` to both flip and open the new trade.
  - In neutral state it opens a market order in the breakout direction.

## Risk management
- **Initial stop-loss** – after each entry the strategy places an internal protective level `InitialStopSteps * PriceStep` away from the fill price.
- **Fixed take-profit** – exits part or all of the position when price reaches `TakeProfitSteps * PriceStep` from the entry.
- **Trailing stop** – once the trade moves in favour more than `TrailingStopSteps * PriceStep`, the stop level is trailed candle by candle, locking in profits while allowing the move to run.
- All protective calculations are done in native price steps, making the logic instrument-agnostic.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle type/timeframe to analyse. | 5-minute time frame |
| `DojiBodyThresholdSteps` | Maximum doji body expressed in price steps. | 1 |
| `BreakoutBufferSteps` | Extra filter above/below the doji extreme before accepting a breakout. | 0 |
| `InitialStopSteps` | Initial stop-loss distance from entry in steps. | 20 |
| `TakeProfitSteps` | Take-profit distance from entry in steps. | 25 |
| `TrailingStopSteps` | Trailing stop distance maintained once the trade is in profit. | 10 |

All parameters are exposed through `StrategyParam<T>` making them visible in the UI and ready for optimisation.

## Implementation notes
- The class is built on the high-level candle subscription API (`SubscribeCandles().Bind(...)`) to stay in sync with the framework best practices.
- State between calls is maintained with `_previousCandle` and `_twoCandlesAgo`, ensuring that only finished candles participate in decision making.
- Protective levels are stored separately for long and short positions and are reset when positions close or when market data is insufficient.
- Logging statements provide insight into signal detection, stop-loss and take-profit events, simplifying debugging during backtests.

## Usage tips
1. Validate the default tick thresholds on each instrument: increase `DojiBodyThresholdSteps` for volatile markets where exact doji prints are rare.
2. Optimise `BreakoutBufferSteps` to filter small fake breakouts when spreads or noise are significant.
3. Combine the strategy with external risk overlays (portfolio stop, trading session filters) if you deploy it on multiple symbols simultaneously.
4. Because signals rely on completed candles, choose a candle type compatible with your desired trading horizon (e.g., 1-minute for scalping, 15-minute for swing entries).
