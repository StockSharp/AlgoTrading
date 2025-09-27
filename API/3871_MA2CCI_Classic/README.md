# MA2CCI Classic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The MA2CCI strategy ports the classic MetaTrader expert advisor built around the interaction of two simple moving averages (SMA) and the Commodity Channel Index (CCI). It filters trades using the CCI zero line and applies protective stops derived from the Average True Range (ATR). The system is designed for trend-following entries with fast reaction to reversals.

The StockSharp version keeps the original trading logic while adapting risk management to the .NET environment. Position sizing follows a risk-per-thousand rule with an additional decrease factor that cuts trade size after consecutive losses. Each entry attaches a volatility-driven stop that mirrors the ATR distance used in the MQL implementation.

## Trading Logic

- **Indicators**
  - Fast SMA with default length 4.
  - Slow SMA with default length 8.
  - CCI filter using 4-period lookback.
  - ATR with period 4 for stop placement.
- **Entry Conditions**
  - **Long**: fast SMA crosses above the slow SMA and the previous finished bar shows CCI rising through zero (from negative to positive).
  - **Short**: fast SMA crosses below the slow SMA and the previous bar shows CCI falling through zero (from positive to negative).
- **Exit Conditions**
  - Opposite SMA crossover closes open positions even if no new trade is initiated.
  - ATR stop: long positions exit when price falls to `entry - ATR`; short positions exit when price rises to `entry + ATR`.

## Risk Management

- Base order volume is configurable; by default 0.1 lots (or exchange equivalent).
- Optional dynamic sizing scales the volume to `free capital * MaxRiskPerThousand / 1000` when portfolio data is available.
- After more than one consecutive loss, the position size is linearly reduced by `losses / DecreaseFactor` of the calculated volume.
- Volatility stops rely on the most recent finished candle; intrabar spikes beyond stop levels trigger a market exit on the next strategy tick.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Working timeframe for all indicators. | 1 hour candles |
| `OrderVolume` | Minimum trade size when risk-based sizing is unavailable. | 0.1 |
| `FastMaPeriod` | Period of the fast SMA. | 4 |
| `SlowMaPeriod` | Period of the slow SMA. | 8 |
| `CciPeriod` | Period of the CCI filter. | 4 |
| `AtrPeriod` | ATR length for stop calculation. | 4 |
| `MaxRiskPerThousand` | Fraction of free capital allocated per trade (per 1000 units). | 0.02 |
| `DecreaseFactor` | Divisor used to shrink volume after losing streaks. | 3 |

## Notes

1. The strategy processes only finished candles, ensuring one decision per bar similar to the original EA that used `Volume[0] > 1` as a gate.
2. Stop levels are simulated internally instead of registering exchange stop orders; this matches the behaviour of the MetaTrader version that relied on market closes when ATR thresholds were hit.
3. Enable charting inside StockSharp Designer to visualize SMA, CCI and executed trades using the built-in drawing helpers.
