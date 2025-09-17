# AutoRisk Strategy

## Overview
The AutoRisk strategy reproduces the position-sizing logic of the original MetaTrader expert advisor. It continuously monitors daily candles, calculates the 14-period Average True Range (ATR), and derives a recommended order volume based on account equity or balance. The strategy does not send orders on its own: it simply keeps the latest calculated volume available through the `RecommendedVolume` property and logs each update.

## Core Logic
1. Subscribe to the configured candle type (daily by default) and feed the prices into an `AverageTrueRange` indicator.
2. Once a candle closes, convert the ATR value into instrument steps using `PriceStep` and into currency using `StepPrice`.
3. Apply the risk percentage (`RiskFactor`) to the selected account metric (`CalculationMode`) and divide by the ATR-based denominator to obtain the raw volume.
4. Align the raw volume with the instrument trading rules (`VolumeStep`, `MinVolume`, `MaxVolume`) using `RoundUp` to choose between rounding to the nearest step or flooring the value.
5. Store and log the resulting volume so external components can retrieve it and place orders accordingly.

## Parameters
- **RiskFactor** – risk percentage applied to the ATR-based denominator (default `2`).
- **CalculationMode** – selects between `Equity` (current portfolio value) and `Balance` (initial portfolio value) as the capital base (default `Balance`).
- **RoundUp** – when enabled, rounds to the nearest volume step; otherwise the value is floored (default `true`).
- **CandleType** – market data type for the ATR calculation (default `TimeFrameCandleMessage` with one-day interval).

## Data Requirements
- Attach a security that exposes `PriceStep`, `StepPrice`, `VolumeStep`, `MinVolume`, and `MaxVolume` so the lot calculation can respect broker constraints.
- Connect a portfolio adapter that updates `Portfolio.CurrentValue` (equity) and `Portfolio.BeginValue` (balance).
- Provide a daily candle stream or adjust `CandleType` to match the desired timeframe.

## Usage Notes
1. Assign a security and portfolio before starting the strategy.
2. Launch the strategy to begin ATR subscriptions; no manual indicator registration is required.
3. Read `RecommendedVolume` (or intercept info log messages) to obtain the latest lot suggestion.
4. Submit orders externally, reusing the recommended volume as needed.
