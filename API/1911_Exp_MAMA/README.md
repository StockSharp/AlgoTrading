# Exp MAMA Strategy

This strategy trades using the MESA Adaptive Moving Average (MAMA) indicator.

The indicator produces two lines:

- **MAMA** – the adaptive moving average.
- **FAMA** – a following average used as a signal line.

Trading logic:

1. When MAMA crosses below FAMA the strategy closes short positions and opens a new long position.
2. When MAMA crosses above FAMA the strategy closes long positions and opens a new short position.

## Parameters

- `FastLimit` – fast alpha limit used by the adaptive factor.
- `SlowLimit` – slow alpha limit used by the adaptive factor.
- `CandleType` – timeframe for incoming candles.
- `BuyOpen` / `SellOpen` – allow opening long or short positions.
- `BuyClose` / `SellClose` – allow closing long or short positions.

The strategy operates on finished candles and uses market orders for entry and exit.
