# MacdPatternTraderV01 Strategy

## Overview

`MacdPatternTraderV01Strategy` is a faithful StockSharp port of the FORTRADER "MacdPatternTraderv01" MetaTrader 4 expert advisor. The system hunts for MACD hook patterns that appear after the oscillator stretches to an extreme level and then rolls back toward the zero line. When a bearish hook forms after an overbought spike the strategy opens short positions, while a bullish hook after an oversold drop triggers longs. The StockSharp version preserves the original multi-layered risk management, including recursive stop-loss and take-profit levels as well as staged position scaling.

The C# implementation uses the high-level candle subscription API with the `MACD`, `ExponentialMovingAverage`, and `SimpleMovingAverage` indicators. All calculations are performed on finished candles, mirroring the `iMACD` and `iMA` calls with explicit bar shifts from the MQL version. Additional helper logic manually tracks recent highs and lows to reproduce the recursive price searches that the EA uses for protective orders.

## Signal Logic

1. **Arming conditions**
   - A *bearish* setup is armed once the MACD main line exceeds `BearishThreshold`. The arming flag is cleared as soon as MACD crosses below zero.
   - A *bullish* setup is armed once the MACD main line drops below `BullishThreshold`. The flag is cleared when MACD becomes positive.
2. **Hook confirmation**
   - Short entries require `macd₀ < BearishThreshold`, `macd₀ < macd₁`, `macd₁ > macd₂`, the bearish flag to remain active, and `macd₂ < BearishThreshold` while `macd₀` stays above zero.
   - Long entries require `macd₀ > BullishThreshold`, `macd₀ > macd₁`, `macd₁ < macd₂`, the bullish flag to remain active, and `macd₂ > BullishThreshold` while `macd₀` remains negative.
3. **Order execution**
   - When the hook completes, the strategy sends a market order with volume `OrderVolume`. It simultaneously stores the computed stop-loss and take-profit prices for later monitoring.

## Risk Management

### Stop-Loss

The stop-loss mimics the MQL function `StopLoss(type)`:

- Short trades look for the highest high over the last `StopLossBars` candles **excluding** the freshly closed bar, then add `OffsetPoints * PriceStep` to the result.
- Long trades search the lowest low over the last `StopLossBars` historical candles, subtracting the same offset.

This logic is implemented with manual extrema searches over a capped in-memory buffer (1,000 values) to avoid building large custom collections.

### Take-Profit

The take-profit reproduces the recursive `TakeProfit(type)` MQL routine:

1. Start with the most recent block of `TakeProfitBars` values. Include the candle that triggered the signal.
2. Compute the extreme (low for shorts, high for longs) within that block.
3. Move back by `TakeProfitBars` candles and repeat while the new block yields a more favorable extreme.
4. Stop at the first block that does **not** improve the extreme and use the last recorded value as the take-profit.

### Partial Position Management

- After entry the strategy records the original volume and entry price.
- Partial exits are allowed only after the floating profit expressed in account currency exceeds `ProfitThreshold`.
- For long positions:
  1. Close one third of the initial volume when the candle close rises above the medium EMA (`EmaMediumPeriod`).
  2. Close half of the remaining position when the candle high pierces the average of `SmaPeriod` and `EmaLongPeriod` values.
- For short positions the rules are mirrored with the candle close below the medium EMA and candle low below the composite average.

Protective orders are checked before scaling to ensure that hard stops or targets always take precedence.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `StopLossBars` | 6 | Number of historical candles for the stop-loss swing search. |
| `TakeProfitBars` | 20 | Block size used by the recursive take-profit algorithm. |
| `OffsetPoints` | 10 | Additional points added to the stop-loss price. |
| `MacdFastPeriod` | 5 | Fast EMA length of the MACD indicator. |
| `MacdSlowPeriod` | 13 | Slow EMA length of the MACD indicator. |
| `MacdSignalPeriod` | 1 | Signal EMA length of the MACD indicator. |
| `BearishThreshold` | 0.0045 | Positive MACD level that arms short setups. |
| `BullishThreshold` | -0.0045 | Negative MACD level that arms long setups. |
| `OrderVolume` | 1 | Volume per market order. |
| `EmaShortPeriod` | 7 | Fast EMA used in the first partial exit. |
| `EmaMediumPeriod` | 21 | Medium EMA used in filters and partial exits. |
| `SmaPeriod` | 98 | SMA used inside the composite exit average. |
| `EmaLongPeriod` | 365 | Long EMA combined with the SMA for the second partial exit. |
| `ProfitThreshold` | 5 | Minimum floating profit (in currency units) before scaling out. |
| `CandleType` | 1-hour time frame | Candle series processed by the strategy. |

All parameters are exposed via `StrategyParam<T>` and support optimization where appropriate.

## Implementation Notes

- The strategy relies exclusively on high-level `SubscribeCandles` bindings. It does not push indicators into the `Indicators` collection, following the project guidelines.
- MACD history is stored using a compact three-value shift register (`_macdPrev1..3`) to mimic `iMACD(..., shift)` access.
- Protective price levels are tracked as decimals; when candles hit a stop or target the strategy closes the entire position with market orders and resets the internal state machine.
- Floating PnL is estimated using `PriceStep`/`StepPrice` so that the partial exit threshold remains consistent regardless of instrument price scale.
- The candle buffers for highs and lows are capped to 1,000 elements, which is sufficient for the default parameters yet prevents uncontrolled growth.

## Usage

1. Instantiate `MacdPatternTraderV01Strategy`, assign the desired security, portfolio, and connector.
2. Optionally adjust parameters such as `CandleType`, `StopLossBars`, or `OrderVolume` to suit the traded instrument.
3. Start the strategy; it will subscribe to the configured candle series, draw MACD and trade markers on the chart, and manage orders automatically.

The strategy contains extensive inline comments describing each translated block to ease maintenance and further customization.
