# FmOne Scalping Strategy

## Overview
FmOne Scalping Strategy is a simplified translation of the FMOneEA MetaTrader 4 expert advisor. The strategy combines a fast and a slow exponential moving average with the MACD indicator to capture short-term momentum on any timeframe.

## How It Works
1. The fast and slow EMAs define the current trend direction.
2. The MACD histogram confirms momentum in the direction of the trend.
3. A buy order is opened when the fast EMA is above the slow EMA and the MACD histogram is positive.
4. A sell order is opened when the fast EMA is below the slow EMA and the MACD histogram is negative.
5. Each position is protected with configurable stop-loss and take-profit levels. Trailing stop can be enabled to follow profitable moves.

## Parameters
- **FastMaPeriod** – Length of the fast EMA.
- **SlowMaPeriod** – Length of the slow EMA.
- **MacdSignalPeriod** – Signal line period for the MACD indicator.
- **StopLossPercent** – Stop-loss size in percent of entry price.
- **TakeProfitPercent** – Take-profit size in percent of entry price.
- **EnableTrailingStop** – Enables trailing stop management.
- **CandleType** – Time frame for incoming candles.

## Notes
This port focuses on the core logic of the original EA. Advanced features like redemption cycles and break-even automation from the MQL version are intentionally omitted to keep the example readable.
