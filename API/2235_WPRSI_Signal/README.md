# WPRSI Signal Strategy

## Overview
This strategy replicates the WPRSIsignal MetaTrader expert. It combines the Williams Percent Range (WPR) and Relative Strength Index (RSI) to generate buy and sell signals.

## Logic
- A **buy** signal is generated when WPR crosses above -20 from below and RSI is above 50. The signal is confirmed only if WPR remains above -20 for the next `FilterUp` bars.
- A **sell** signal is generated when WPR crosses below -80 from above and RSI is below 50. The signal is confirmed only if WPR remains below -80 for the next `FilterDown` bars.
- When a buy signal is confirmed, the strategy opens a long position if no long position is active. When a sell signal is confirmed, it opens a short position if no short position is active.

## Parameters
- `Period` – calculation length for WPR and RSI.
- `FilterUp` – number of bars that must keep WPR above -20 to confirm a buy signal.
- `FilterDown` – number of bars that must keep WPR below -80 to confirm a sell signal.
- `CandleType` – timeframe of candles used for the calculations.

## Usage
Attach the strategy to any security. The strategy uses `SubscribeCandles` and `Bind` to receive candle data and indicator values. Positions are managed using market orders: `BuyMarket` for long entries and `SellMarket` for short entries. The strategy does not implement stop-loss or take-profit; positions are closed by opposite signals.
