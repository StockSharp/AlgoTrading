# Spot Futures Arbitrage

Arbitrages the price difference between a spot asset and its futures contract.
Enters long spot/short futures when the futures trades above the spot by a threshold, and the opposite when below.
Thresholds can be dynamic based on the spread average and standard deviation, and trades are closed when the spread reverts or after a maximum holding time.

## Parameters
- **Spot** — spot security.
- **Future** — futures security.
- **CandleType** — candle timeframe.
- **MinSpreadPct** — minimum spread percentage to enter.
- **LookbackPeriod** — period for spread statistics.
- **AdaptiveThreshold** — enable dynamic thresholds.
- **MaxHoldHours** — maximum position holding time in hours.
