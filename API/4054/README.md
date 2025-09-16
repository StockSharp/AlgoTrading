# N7S AO 772012 Strategy

## Overview

This strategy is a StockSharp conversion of the MetaTrader expert advisor **N7S_AO_772012**. The original robot combines perceptron-like filters of the Awesome Oscillator (AO) across several timeframes with a price pattern gate and a configurable "neuro" mode that can override the base logic. The converted version preserves the decision tree while exposing all tuning knobs as strategy parameters.

The bot operates on the primary instrument selected in the strategy and uses:

- **M1 candles** for entry timing and the price perceptron.
- **H1 candles** to feed multiple AO-based perceptrons.
- **H4 candles** to compute the AO momentum delta used by the base Buy/Sell selector (BTS).

## Trading logic

1. On each finished M1 candle the strategy updates the price pattern history, manages existing positions, and evaluates whether trading is allowed (no trading on Monday before 02:00 or on Friday from 18:00 and later, platform local time).
2. Hourly AO values are aggregated into five perceptrons:
   - `Perceptron X/Y` – base BTS filters that work together with the price perceptron and H4 AO delta.
   - `Neuro X/Y` – advanced long/short filters used when the neuro mode grants priority to them.
   - `Neuro Z` – gating perceptron that enables Neuro X in mode 4.
3. The price perceptron evaluates the weighted differences between recent M1 opens and the latest close.
4. The **neuro mode** parameter controls how the uppercase perceptrons intervene:
   - `4`: If Neuro Z > 0, only Neuro X can generate a long signal; otherwise Neuro Y can trigger a short. If neither fires, fall back to BTS.
   - `3`: Neuro Y can trigger shorts; otherwise fall back to BTS.
   - `2`: Neuro X can trigger longs; otherwise fall back to BTS.
   - Any other value skips the neuro layer and directly evaluates BTS.
5. The **BTS** block uses the price perceptron and H4 AO delta as gates:
   - Long setup: price perceptron > 0 (unless `BtsMode = 0`), Neuro/BTS X > 0 and H4 AO delta > 0. Stop-loss is `BaseStopLossPointsLong`, take-profit is `BaseTakeProfitFactorLong × BaseStopLossPointsLong`.
   - Short setup: price perceptron < 0 (unless `BtsMode = 0`), Neuro/BTS Y > 0 and H4 AO delta < 0. Stop-loss is `BaseStopLossPointsShort`, take-profit is `BaseTakeProfitFactorShort × BaseStopLossPointsShort`.
6. After a signal is accepted the strategy opens a market order (respecting the enabled direction). Protective prices are tracked internally; every finished M1 candle checks whether the stop or target was hit using candle highs/lows and closes the position when appropriate. Opposite signals first close the existing position and wait for the next candle before re-entry.

## Parameters

### Trading
- **OrderVolume** – Base volume for all market orders.
- **AllowLongTrades / AllowShortTrades** – Enable or disable long or short entries.
- **BtsMode** – When set to `0` the price perceptron gate in BTS is ignored; otherwise its sign must align with the trade.
- **NeuroMode** – Selects how the advanced perceptrons participate (see logic section).

### Base BTS perceptrons
- **BaseStopLossPointsLong / BaseTakeProfitFactorLong** – Stop distance (points) and multiplier for long take-profit.
- **BaseStopLossPointsShort / BaseTakeProfitFactorShort** – Analogous settings for short trades.
- **PerceptronPeriodX / Y** – AO shift (in H1 bars) used by the respective perceptron.
- **PerceptronWeightX1..4 / Y1..4** – Weights (0–100) of the perceptron inputs; internally they are centred by subtracting 50.
- **PerceptronThresholdX / Y** – Minimum absolute perceptron output required before it is considered valid.

### Price filter
- **PricePatternPeriod** – Number of M1 candles forming each lag in the price perceptron.
- **PriceWeight1..4** – Weights (centred around 50) applied to price differences inside the perceptron.

### Neuro perceptrons
- **NeuroStopLossPointsLong / NeuroTakeProfitFactorLong** – Stop and TP multiplier used by Neuro X signals.
- **NeuroStopLossPointsShort / NeuroTakeProfitFactorShort** – Stop and TP multiplier used by Neuro Y signals.
- **NeuroPeriodX / Y / Z** – AO shift (H1 candles) for the three neuro perceptrons.
- **NeuroWeightX1..4 / NeuroWeightY1..4 / NeuroWeightZ1..4** – Perceptron weights.
- **NeuroThresholdX / NeuroThresholdY / NeuroThresholdZ** – Minimum absolute value for each neuro perceptron.

### Data
- **CandleType** – Timeframe used for the primary trading candles (default 1 minute).

## Trade management

- Stop-loss and take-profit distances are converted from points to absolute prices using the instrument price step. If a distance is set to zero, the corresponding protection is disabled.
- Protective levels are monitored on completed M1 candles by comparing candle highs/lows to the stored prices.
- The strategy works in netting mode: it never holds both long and short positions simultaneously. An opposite signal closes the current position first.

## Notes on the conversion

- High level StockSharp bindings (`SubscribeCandles().Bind(...)`) are used to stream AO values without direct indicator queries.
- Historical buffers are kept as fixed-size lists to emulate the original shift-based indexing while avoiding direct indicator lookups.
- No Python version is provided, as requested.
- Tests were not modified.
