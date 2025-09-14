# CCI Woodies Strategy

## Overview
This strategy trades based on the crossover of two Commodity Channel Index (CCI) lines derived from the Woodies CCI method. A fast CCI and a slow CCI are calculated on the specified timeframe. When the fast line crosses below the slow line, a long position is opened and any short position is closed. When the fast line crosses above the slow line, a short position is opened and any long position is closed.

## Parameters
- **FastPeriod** – length of the fast CCI indicator.
- **SlowPeriod** – length of the slow CCI indicator.
- **CandleType** – timeframe of candles used for calculations.
- **InvertSignals** – if enabled, buy and sell rules are swapped.
- **TakeProfitPoints** – profit target in price points.
- **StopLossPoints** – loss limit in price points.

## Notes
The strategy uses the high-level StockSharp API. Indicators are bound via `Bind`, and risk control is handled with `StartProtection` using stop-loss and take-profit levels.
