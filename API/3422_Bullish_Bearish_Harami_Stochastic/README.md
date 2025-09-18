# Bullish & Bearish Harami Stochastic Strategy

The **Bullish & Bearish Harami Stochastic Strategy** is the StockSharp port of the MetaTrader Expert Advisor `expert_abh_bh_stoch.mq5` from folder `MQL/310`. The original expert uses candlestick pattern recognition for Bullish Harami and Bearish Harami setups and requires a stochastic oscillator confirmation. The C# version keeps the same logic using the high-level StockSharp API and adds detailed logging and chart output for easier monitoring.

## Core Ideas

- Detect Bullish Harami and Bearish Harami candlestick patterns using the previous two completed candles.
- Confirm bullish setups with the stochastic %D line below an oversold threshold and bearish setups with %D above an overbought threshold.
- Close short positions when the stochastic %D line rebounds above either the lower or upper exit thresholds, and close long positions when %D drops below those thresholds.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Timeframe of the candle series used for pattern recognition. | `1 Hour` |
| `StochasticKPeriod` | Lookback period for the stochastic %K calculation. | `47` |
| `StochasticDPeriod` | Smoothing period for the %D line. | `9` |
| `StochasticSlowing` | Additional smoothing applied to %K (MT5 “slowing”). | `13` |
| `MovingAveragePeriod` | Number of candles used to average body size for pattern validation. | `5` |
| `OversoldLevel` | Stochastic %D threshold to confirm bullish signals. | `30` |
| `OverboughtLevel` | Stochastic %D threshold to confirm bearish signals. | `70` |
| `ExitLowerLevel` | Lower stochastic level that triggers exits. | `20` |
| `ExitUpperLevel` | Upper stochastic level that triggers exits. | `80` |

## Trading Rules

### Long Entry
1. A Bullish Harami pattern is detected on the two most recent completed candles (a small bullish candle engulfed by a longer bearish candle in a downtrend).
2. The stochastic %D line of the confirmation candle is at or below `OversoldLevel`.
3. No long position is currently open (`Position <= 0`).
4. The strategy buys at market for the configured `Volume`, adding any short exposure to flip the position if necessary.

### Short Entry
1. A Bearish Harami pattern is detected (small bearish candle inside a long bullish candle during an uptrend).
2. The stochastic %D value is at or above `OverboughtLevel`.
3. No short exposure exists (`Position >= 0`).
4. The strategy sells at market, covering any long position first if required.

### Exits
- **Cover Shorts:** When the stochastic %D crosses upward through either `ExitLowerLevel` or `ExitUpperLevel`, the algorithm covers the entire short position.
- **Close Longs:** When the stochastic %D crosses downward through `ExitUpperLevel` or `ExitLowerLevel`, the long position is closed.

## Files

- `CS/BullishBearishHaramiStochasticStrategy.cs` — high-level StockSharp implementation of the strategy.
- `README.md` — English documentation (this file).
- `README_ru.md` — Russian documentation.
- `README_cn.md` — Chinese documentation.

> **Note:** The Python version is not included per the conversion instructions.
