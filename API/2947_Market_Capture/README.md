# Market Capture Strategy

## Overview
The Market Capture strategy reproduces the logic of the original MetaTrader 5 expert. The algorithm builds a dynamic grid around a moving center price and opens hedge-style trades whenever price swings around this center. Positions are distributed above and below the center with fixed profit targets, while account equity milestones control when to liquidate the worst losing trades.

## Trading Rules
- **Center line** – the strategy stores an internal center level that starts at the first processed candle close. When the market moves farther than the configured grid spacing, the center is shifted step-by-step to follow price.
- **Initial short** – an optional short position can be opened immediately after start to match the behaviour of the MQL script.
- **Long entries** – a long trade is allowed when the latest close is above the center and the previous candle traded below it. A proximity check ensures that no other long trade is active near the same level.
- **Short entries** – a short trade is allowed when the latest close is below the center and the previous candle traded above it. The same proximity filter prevents stacking identical shorts.
- **Take profit** – every trade stores a target level that is a fixed multiple of the instrument price step away from the entry price. Candle highs (for longs) or lows (for shorts) reaching the target trigger a market exit.
- **Equity management** – the strategy monitors portfolio equity. After a configurable percentage gain it closes a number of the worst losing trades to lock profits. Another percentage threshold defines when to reduce risk during drawdown by liquidating losing trades. Every time a threshold fires the equity baseline is recalculated.

## Parameters
- `Enable Long` / `Enable Short` – allow or block trades in each direction.
- `Grid Steps` – spacing between grid levels measured in price steps.
- `Take Profit Steps` – take profit distance measured in price steps.
- `Open Initial Short` – enable the first short order placed right after start.
- `Use Equity Target` – activate the equity growth rule for trimming losing trades.
- `Track Drawdown` – activate the drawdown rule for trimming losing trades.
- `Equity Gain %` / `Equity Loss %` – equity change percentages that fire the above rules.
- `Loss Trades Up` / `Loss Trades Down` – maximum number of losing trades closed when each rule is triggered.
- `Candle Type` – timeframe or custom candle type used for the decision process.
- `Volume` (strategy property) – trade size for each market order.

## Notes
- The strategy keeps an internal register of open trades to mimic the hedging style of the original expert while working with the netted position model of StockSharp.
- Distance parameters are multiplied by the security price step; ensure that the selected instrument exposes a valid `PriceStep` value.
- The logic operates on finished candles only. Select a candle type that matches the intended trading horizon from very short-term grids to wider swing grids.
