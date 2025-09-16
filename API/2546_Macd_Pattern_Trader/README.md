# MACD Pattern Trader Strategy

## Overview
This strategy is a direct conversion of the MetaTrader expert **MacdPatternTraderAll0.01**. It trades a single instrument using six different MACD-based entry patterns, optional trading hours filtering, partial profit taking, and a martingale position sizing option. All calculations are performed on completed candles delivered by the configured `CandleType`.

## Trading Logic
1. On every finished candle the strategy updates six MACD indicators (each pattern has its own fast and slow EMA lengths and a single-period signal line).
2. If trading time filtering is enabled, new trades are only evaluated between `SessionStart` and `SessionEnd`. Risk management is always active.
3. Each MACD pattern checks very specific value relationships between the current MACD value and the two previous values to detect momentum reversals. When a pattern is triggered it sends a market order in the corresponding direction and sets internal stop-loss and take-profit levels.
4. Stop-loss is calculated as the recent extreme (highest high for shorts, lowest low for longs) of a configurable lookback plus/minus an offset measured in price steps. Take-profit scans older groups of candles in blocks to replicate the recursive target search of the original expert.
5. Only one net position is managed at a time. If a new signal appears in the opposite direction, the current position is closed and a reverse position is opened with the martingale-adjusted volume.
6. Active positions are monitored by `ManageActivePosition`. The logic emulates the original partial closure routine:
   - For longs: when profit exceeds `ProfitThreshold` (5 currency units) and the previous close is above the medium-term EMA, one-third of the position is sold. If profit persists and the previous high is above the average of the long SMA and the very slow EMA, half of the remaining position is closed.
   - For shorts: symmetric rules close one-third and then half of the remaining position when profit targets and moving average filters are met.
7. Risk management runs on every candle regardless of the trading window. If price pierces the stored stop-loss or take-profit level inside a candle (based on high/low), the entire position is flattened at the breach price.
8. After a trade is fully closed the realized PnL is evaluated. When `UseMartingale` is enabled, a losing trade doubles the next order volume, while any profitable exit resets the volume back to the base `LotSize`.

## Key Patterns
- **Pattern 1:** Detects MACD spikes above `Pattern1MaxThreshold` that start turning down, and drops below `Pattern1MinThreshold` that bounce up.
- **Pattern 2:** Looks for MACD crosses around the zero line with minimal excursions.
- **Pattern 3:** Uses two-tier thresholds (`Pattern3MaxThreshold`, `Pattern3SecondaryMax`, `Pattern3MinThreshold`, `Pattern3SecondaryMin`) to detect three-step rollovers on both sides. It also counts consecutive bars above the secondary max to mimic the original `bars_bup` accumulation.
- **Pattern 4:** Trades when MACD exceeds the primary thresholds but the previous bar sits within the tighter secondary range, anticipating reversals.
- **Pattern 5:** Responds to quick MACD flips within narrow ranges defined by `Pattern5PrimaryMax/Min` and the secondary limits.
- **Pattern 6:** Uses counters (`Pattern6MaxBars`, `Pattern6MinBars`, `Pattern6CountBars`) to require multiple consecutive MACD excursions before triggering a trade.

## Risk Management
- Internal stop-loss and take-profit targets are recalculated for each entry. Stops use price extremes plus an offset measured in price steps. Take-profit searches consecutive blocks of candles until an extreme fails to improve, reproducing the recursive logic from the MQL expert.
- Partial exits respect the original minimum lot size (0.01) and keep track of how many partial closures have been executed per direction.
- The strategy never places broker-side protective orders; instead it monitors the candle highs and lows to close positions at the configured prices.

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle series used for indicators and trading signals. | 1 hour candles |
| `LotSize` | Base trade volume before martingale adjustments. | 0.1 |
| `UseTimeFilter` | Enable trading only between `SessionStart` and `SessionEnd`. | true |
| `SessionStart` / `SessionEnd` | Trading window (local exchange time). | 07:00 / 17:00 |
| `UseMartingale` | Double `LotSize` after a losing trade. | true |
| `Ema1Period`, `Ema2Period`, `SmaPeriod`, `Ema3Period` | Moving averages used for partial exits. | 7, 21, 98, 365 |
| Pattern-specific parameters | Each pattern has its own enable flag, stop-loss/take-profit lookbacks, offsets, EMA lengths, and threshold values matching the original expert inputs. | See constructor defaults |

All thresholds and EMA lengths are exposed through `StrategyParam` objects, allowing optimization or fine tuning.

## Notes
- The strategy assumes the instrument provides `PriceStep` and `PriceStepCost` to translate offsets and profits to account currency. When not available, price differences are used directly.
- Stops and targets are simulated internally; they will be evaluated on bar close. Intrabar execution in real-time may differ from MetaTrader behavior.
- The martingale mechanism can increase exposure quickly after a losing streak—use with caution.
