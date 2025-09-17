# The Predator Strategy

## Overview

This strategy is a StockSharp high-level translation of the MQL expert advisor **"The Predator"**. The original system mixes trend direction filters with momentum, Bollinger Bands, and stochastic oscillators. Two independent entry templates (Strategy 1 and Strategy 2) are available, replicating the selectable modes inside the MQL implementation.

The conversion focuses on candle-based processing, using StockSharp subscriptions and indicator bindings. All calculations are performed on a single configurable candle series.

## Core Indicators

- **Linear Weighted Moving Averages (LWMA)** – fast/slow structure to confirm the short-term trend.
- **Directional Movement Index + Average Directional Index (DMI/ADX)** – directional strength and trend confirmation.
- **Momentum (period 14 by default)** – measures the distance from the neutral 100 level for both breakout and pullback logic.
- **Bollinger Bands** – two envelopes (narrow and wide) to detect context and prior candle location, especially for Strategy 2.
- **Stochastic Oscillator** – additional filter for Strategy 2 to require momentum exhaustion zones.
- **MACD** – trend momentum confirmation by comparing the MACD line with its signal.

## Trading Logic

### Common filters

1. Process only completed candles.
2. Require the selected indicators to be formed before trading (`IsFormedAndOnlineAndAllowTrading`).
3. ADX must be greater than the configured threshold.
4. Momentum deviation history is maintained for the last three values to match the MQL checks without calling `GetValue` on indicators.

### Strategy 1

- **Long entries** when:
  - ADX above threshold and +DI exceeds −DI.
  - Fast LWMA above slow LWMA.
  - Momentum deviation above the buy threshold on any of the last three values.
  - MACD line above its signal line.
- **Short entries** mirror the above with the signs reversed.

### Strategy 2

- **Long entries** additionally require:
  - Previous candle close at or above the previous narrow-band lower Bollinger boundary.
  - Stochastic signal and main lines both above the upper threshold.
  - Momentum deviation below the buy threshold on any of the last three values (looking for pullbacks inside trends).
- **Short entries** require:
  - Previous candle close at or below the previous narrow-band upper Bollinger boundary.
  - Stochastic signal line above the upper threshold while the main line is below the lower threshold.
  - Momentum deviation below the sell threshold on any of the last three values.

### Position handling

- The strategy cancels any pending active orders before opening a new trade.
- When a reversal signal occurs, the strategy closes the current exposure and opens a new position in the opposite direction using a combined market order.

## Risk Management

- `StartProtection` configures:
  - Initial stop-loss distance in pips.
  - Initial take-profit distance in pips.
  - Optional trailing stop that trails by a fixed pip amount once armed.
- Risk distances are converted into absolute price units using the security price step.
- The money-based break-even and trailing modules from the original EA are replaced with these pip-based protections (documented difference below).

## Parameters

| Parameter | Description |
|-----------|-------------|
| `Mode` | Chooses Strategy 1 (trend breakout) or Strategy 2 (pullback with stochastic filters). |
| `FastMaLength`, `SlowMaLength` | LWMA lengths used to determine trend direction. |
| `DmiPeriod`, `AdxSmoothing` | Directional Movement Index parameters. |
| `MomentumPeriod` | Lookback used by the momentum indicator. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Minimum deviation from 100 required to accept signals. |
| `AdxThreshold` | Minimum ADX level signalling an actionable trend. |
| `BollingerPeriod`, `TightBandWidth`, `WideBandWidth` | Bollinger Band settings for the context filters. |
| `StochasticLength`, `StochasticSmooth`, `StochasticUpper`, `StochasticLower` | Parameters for the stochastic oscillator used in Strategy 2. |
| `TradeVolume` | Volume submitted with market orders. |
| `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | Risk distances (converted to price units with the instrument step). |
| `CandleType` | Data series used by the strategy. |

## Differences from the MQL Version

- Money-based take-profit, stop-loss, and trailing values are translated into pip distances handled by `StartProtection`.
- Break-even adjustments and notification emails/push messages are not ported (not available in high-level API).
- The MQL expert called MACD and Momentum on higher timeframes. In StockSharp the logic runs on the configured candle series only; multi-timeframe data can be added through additional subscriptions if required.
- Order volume optimization and martingale-style sizing are not implemented; the StockSharp version uses a fixed `TradeVolume` parameter.

## Usage

1. Create a connector and portfolio as in other StockSharp samples.
2. Instantiate `ThePredatorStrategy`, assign `Security`, `Portfolio`, and desired parameters.
3. Start the strategy. Visualisation is optional but available when a chart area is provided.

The translation keeps the decision tree faithful to the original while embracing StockSharp best practices such as indicator binding and `StartProtection` for risk. Adjust thresholds to fit the chosen instrument and timeframe.
