# Scalper EMA Simple Strategy

## Overview

The **Scalper EMA Simple Strategy** is a conversion of the MetaTrader expert advisor `ScalperEMAEASimple`. It uses a combination of fast/slow exponential moving averages, a stochastic oscillator, and an Average Directional Index (ADX) filter to identify short-lived pullback entries within an existing trend. The strategy is designed for intraday scalping on liquid FX pairs but can be applied to any instrument where pip-based risk management makes sense.

The implementation follows the StockSharp high-level API and evaluates only finished candles. All calculations are performed incrementally without reprocessing historical data, making the logic suitable for live trading.

## Indicator Stack

- **Fast EMA (`FastEmaPeriod`)** – detects short-term momentum.
- **Slow EMA (`SlowEmaPeriod`)** – defines the prevailing trend direction.
- **Stochastic Oscillator (`StochasticLength`, `StochasticKPeriod`, `StochasticDPeriod`)** – tracks momentum reversals near oversold/overbought boundaries.
- **Average Directional Index** – rejects trades when the trend becomes excessively strong (ADX above `AdxThreshold`).

The stochastic oscillator fires a confirmation signal whenever the %K line crosses back above the oversold level (long setups) or below the overbought level (short setups). The EMA pair provides the directional filter, and the ADX component ensures that entries are restricted to calm retracements rather than runaway trends.

## Entry Logic

1. The candle must close on the trend side of the slow EMA and the fast EMA must agree with that direction (`fast > slow` for longs, `fast < slow` for shorts).
2. The distance between the candle and the slow EMA must be smaller than the candle range and tighter than the three previous distances. This behaviour recreates the pullback-detection loop from the original MQL code.
3. Either the candle body crosses the fast EMA or the fast EMA itself crosses the slow EMA. This condition acts as the breakout trigger.
4. The stochastic oscillator must confirm momentum by crossing back from the extreme zone within the last `ConditionWindowBars` candles.
5. ADX must remain below `AdxThreshold`, preventing trades when volatility accelerates sharply.
6. At least `SignalCooldownBars` candles must pass between two consecutive signals of the same direction.

When all checks pass, the strategy closes any opposite exposure and opens a new market order in the detected direction.

## Exit Logic and Risk Controls

- An initial stop-loss is placed at `StopLossPips` (converted to price using the instrument pip size) from the entry price.
- A trailing stop automatically maintains a distance of `TrailingDistancePips` once unrealised profit reaches `TrailingActivationPips`.
- Opposite signals force a flat position before establishing a fresh trade.

All protective orders are managed through StockSharp's `SetStopLoss` helper to keep risk controls in sync with the current position volume.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Volume` | Base trading volume for each signal. The strategy automatically adds existing exposure to ensure full reversal when changing direction. |
| `FastEmaPeriod` / `SlowEmaPeriod` | Period lengths for the exponential moving averages. |
| `StochasticLength`, `StochasticKPeriod`, `StochasticDPeriod` | Stochastic oscillator configuration mirroring the original EA defaults. |
| `StochasticOversold` / `StochasticOverbought` | Extreme levels that define the retracement zones. |
| `AdxThreshold` | Maximum ADX value allowed before rejecting trades. |
| `SignalCooldownBars` | Minimum bars between successive signals in the same direction. |
| `ConditionWindowBars` | Number of bars during which retracement, EMA breakout, and stochastic confirmation must align. |
| `StopLossPips` | Initial stop-loss distance expressed in pips. |
| `TrailingDistancePips` | Distance maintained by the trailing stop once activated. |
| `TrailingActivationPips` | Profit threshold that arms the trailing stop. |
| `CandleType` | Candle series used for all indicators. Default is a 5-minute timeframe. |

## Implementation Notes

- Pip conversions rely on the instrument `PriceStep`. For 3 or 5 decimal-place instruments the pip factor is multiplied by ten, matching MetaTrader conventions.
- The strategy processes only finished candles, so execution occurs after the close of each bar.
- Internal state variables store the last indices for retracement, EMA breakout, and stochastic confirmations in order to reproduce the look-back windows used by the original expert advisor without scanning the entire history.

## Usage

1. Attach the strategy to a `Connector` or `Trader` instance with a configured security and portfolio.
2. Ensure the security has a valid `PriceStep` for pip-to-price conversion.
3. Adjust parameters according to the instrument volatility. Slow EMA defaults to 740 to match the source EA, but faster markets may benefit from shorter settings.
4. Start the strategy. Market and protective orders will be generated automatically when the conditions described above are satisfied.

> **Disclaimer**: This strategy was ported for educational purposes. Thorough forward tests and risk analysis are recommended before trading live capital.
