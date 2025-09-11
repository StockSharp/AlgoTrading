# Proxy Financial Stress Index

This strategy builds a composite stress index from several markets (VIX, US 10Y yield, DXY, S&P 500, EUR/USD and HYG). Each component is normalized with a z-score and combined using user defined weights. When the index crosses below the threshold the strategy opens a long position. The position is closed after a fixed number of bars.

## Entry Criteria
- Stress index crosses below `Threshold`.

## Exit Criteria
- Close after `HoldingPeriod` bars.

## Parameters
- `SmaLength` = 41
- `StdDevLength` = 20
- `Threshold` = -0.8
- `HoldingPeriod` = 28
- `VixWeight` = 0.4
- `Us10yWeight` = 0.2
- `DxyWeight` = 0.12
- `Sp500Weight` = 0.06
- `EurusdWeight` = 0.1
- `HygWeight` = 0.18

## Indicators
- SMA
- StandardDeviation
