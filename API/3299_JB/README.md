# JB Strategy

## Summary

The JB strategy originates from an fxDreema expert advisor that combines long-term trend filters, momentum confirmation and volatility breakouts:

- **Trend filter:** require the previous candle close to stay above (long) or below (short) a 100-period simple moving average.
- **Momentum filter:** confirm direction with a 100-period Force Index (positive for longs, negative for shorts).
- **Volatility trigger:** enter when the previous close pierces the corresponding Bollinger Band (20-period, 2.0 deviation).
- **Position management:** increase the order volume with a martingale-style multiplier after a losing cycle and reset to the base size after profitable cycles.
- **Exit rule:** close all open positions once the average unrealized profit per contract reaches a configurable money target.

## Parameters

| Name | Description |
| --- | --- |
| `SmaPeriod` | Length of the SMA trend filter. Default: 100. |
| `ForcePeriod` | Length of the Force Index indicator. Default: 100. |
| `BollingerPeriod` | Bollinger Bands length. Default: 20. |
| `BollingerDeviation` | Standard deviation multiplier for Bollinger Bands. Default: 2.0. |
| `BaseVolume` | Initial order volume before martingale adjustments. Default: 0.1. |
| `LossMultiplier` | Multiplier applied to the next order volume after a losing cycle. Default: 1.55. |
| `AverageProfitTarget` | Average unrealized profit per contract required to close all positions. Default: 2.8. |
| `CandleType` | Candle type used for calculations (defaults to 1-minute time frame). |

## Signals

### Long entry
1. Previous candle close is below or equal to the lower Bollinger Band.
2. Previous close is greater than the 100-period SMA (trend pointing up).
3. Force Index value is positive.

### Short entry
1. Previous candle close is above or equal to the upper Bollinger Band.
2. Previous close is lower than the 100-period SMA (trend pointing down).
3. Force Index value is negative.

### Exits
- When the average unrealized profit per contract across all open positions meets `AverageProfitTarget`, all positions are closed at market.
- After every flat position, the strategy adjusts the next order volume: multiply by `LossMultiplier` after a losing cycle, reset to `BaseVolume` after a profitable cycle.

## Notes

- The martingale adaptation uses realized PnL to decide when a loss streak occurred; make sure the strategy is only used on instruments where increasing volume is acceptable.
- Because StockSharp strategies work with net positions, hedging (simultaneous long and short baskets) from the MQL version is approximated using aggregate positions.
