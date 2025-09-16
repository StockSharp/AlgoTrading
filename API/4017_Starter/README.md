# Starter 2005 Strategy

## Overview
The **Starter 2005 Strategy** is a StockSharp high-level API conversion of the classic MetaTrader 4 expert advisor `Starter.mq4` released in 2005. The original system mixed a Laguerre oscillator, an exponential moving average slope filter, and a Commodity Channel Index confirmation. This port keeps the decision tree intact while adapting money-management and execution to StockSharp conventions:

- A Laguerre RSI proxy replicates the `iCustom("Laguerre")` buffer that oscillates between 0 and 1.
- A 5-period EMA calculated on the median price supplies the same rising/falling slope confirmation used by the MT4 expert.
- A 14-period CCI measured on closing prices filters out weak setups just like the original `Alpha` variable.
- The adaptive lot sizing routine mirrors the historical `LotsOptimized()` function, including streak-based reductions after consecutive losses.
- Position exits are triggered either by Laguerre reversing out of the extreme zone or by the trade reaching a configurable profit distance equivalent to `Point * Stop`.

## Trading logic
1. **Indicator preparation**
   - Laguerre RSI value is reconstructed through a four-stage Laguerre filter with configurable `Gamma`.
   - EMA length defaults to five candles and operates on `(High + Low) / 2` to match `PRICE_MEDIAN` in MQL4.
   - CCI period defaults to 14 on close prices, and a very small threshold (`±5`) is maintained to stay faithful to the legacy code.
2. **Long setup**
   - Laguerre must sit close to zero (`LaguerreEntryTolerance` emulates the strict `== 0` comparison).
   - EMA must be rising compared to the previous finished candle.
   - CCI must drop below `-CciThreshold`.
3. **Short setup**
   - Laguerre must sit close to one (`1 - LaguerreEntryTolerance` approximates `== 1`).
   - EMA must be falling.
   - CCI must rise above `+CciThreshold`.
4. **Exits**
   - Longs close when Laguerre rallies above `LaguerreExitHigh` (default `0.9`) or when price advances by `TakeProfitPoints * PriceStep` from the entry.
   - Shorts close when Laguerre dips below `LaguerreExitLow` (default `0.1`) or when price falls by the same distance.
   - Any other manual flat position automatically resets the internal state to prevent stale entry data.

## Money management
The `CalculateOrderVolume` helper reproduces the MT4 `LotsOptimized()` behaviour:

1. **Risk-based sizing** – Equity multiplied by `MaximumRisk` is divided by `RiskDivider` (default 500, as in the original `/500` rule). When divided by the current price, this produces the risk-adjusted lot size.
2. **Fallback lot** – If risk sizing produces a smaller number than `BaseVolume`, the algorithm keeps the base lot.
3. **Loss streak reduction** – After two or more consecutive losing trades, the volume is reduced by `volume * losses / DecreaseFactor`, exactly matching the MQL loop that inspected the trade history.
4. **Normalization** – Volumes are normalized to the instrument’s `VolumeStep` and clamped between `MinVolume` and `MaxVolume` to avoid rejected orders.

Consecutive loss tracking resets after any profitable exit and increments after losing trades; break-even results leave the counter untouched, mirroring the original behaviour that ignored zero-profit tickets.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BaseVolume` | `decimal` | `1.2` | Minimum lot size used when risk sizing suggests a smaller amount. |
| `MaximumRisk` | `decimal` | `0.036` | Fraction of equity exposed on a new position before applying the divider. |
| `RiskDivider` | `decimal` | `500` | Divisor applied to risk capital, reproducing the original `AccountFreeMargin() * MaximumRisk / 500` rule. |
| `DecreaseFactor` | `decimal` | `2` | Streak divisor used to shrink volume after consecutive losses. |
| `MaPeriod` | `int` | `5` | EMA length on the candle median price. |
| `CciPeriod` | `int` | `14` | Commodity Channel Index lookback. |
| `CciThreshold` | `decimal` | `5` | Absolute CCI level required to trigger a signal. |
| `LaguerreGamma` | `decimal` | `0.66` | Smoothing factor of the Laguerre filter. |
| `LaguerreEntryTolerance` | `decimal` | `0.02` | Tolerance around 0/1 used to mimic the original equality checks. |
| `LaguerreExitHigh` | `decimal` | `0.9` | Upper exit level for long positions. |
| `LaguerreExitLow` | `decimal` | `0.1` | Lower exit level for short positions. |
| `TakeProfitPoints` | `decimal` | `10` | Profit target expressed in price points (`Point * Stop` in MQL). |
| `CandleType` | `DataType` | `TimeFrame(5m)` | Candle subscription processed by the strategy. |

## Implementation notes
- Laguerre RSI is implemented inline using the four-level recursion from the original indicator; no calls to `GetValue()` are required.
- EMA and CCI indicators are updated manually inside the candle callback to guarantee the median-price feed matches MetaTrader’s `PRICE_MEDIAN` option.
- Market entries respect `AllowLong()` / `AllowShort()` flags and ensure no active orders are pending, preserving the single-position design of the source EA.
- Trade result tracking uses the candle’s decision price (last price, close, or open) to estimate PnL direction and maintain the loss streak counter.
- Inline English comments describe every major decision block to help future maintenance.

## Usage tips
- The original EA was intended for intraday FX charts; start with liquid instruments that offer small price steps so the 10-point profit target aligns with one pip.
- Because the MT4 script only ever holds one position, run the strategy in environments where partial fills and simultaneous orders are unlikely (historical testing or liquid markets).
- Adjust `LaguerreEntryTolerance` if the oscillator rarely touches exactly 0 or 1 on your data set.
- Tune `RiskDivider` and `DecreaseFactor` together to balance risk growth and loss mitigation.
