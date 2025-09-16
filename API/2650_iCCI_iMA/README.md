# iCCI iMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the MetaTrader "iCCI iMA" expert advisor. It trades Commodity Channel Index (CCI) crossovers against an exponential moving average (EMA) that is applied directly to the CCI stream. A secondary CCI, calculated with its own period, supervises overbought/oversold reversals around the ±100 bands. Orders are sized in lots, optionally scaled by account balance, and each trade is protected by configurable stop-loss and take-profit levels expressed in pips.

## How it works
* **Data source** – A configurable candle series (1-minute candles by default) feeds all indicator calculations using the candle's typical price `(high + low + close) / 3`.
* **Core indicators** – The primary CCI measures momentum with the `CciPeriod` length. An EMA of that CCI (length `MaPeriod`) smooths the oscillator and acts as the signal line. The secondary `CciClosePeriod` CCI monitors threshold crossovers.
* **Entry logic** – A long position opens when the current CCI is above its EMA while the value from two completed candles ago was below the EMA, indicating an upward crossover. A short position mirrors this logic when the CCI crosses downward. The algorithm only trades after all indicators are fully formed and two historical bars are available to reproduce the original look-back of the MQL implementation.
* **Exit logic** – Existing longs close when the secondary CCI falls back below +100 or when the primary CCI drops under the EMA after having been above it two bars earlier. Shorts exit when the secondary CCI rises above −100 or when the CCI rises back above the EMA under the same two-bar confirmation. Protective stops monitor each finished candle: long positions close if price trades down to `entry − stopLossPips * pipSize` and take profit at `entry + takeProfitPips * pipSize`; shorts use the symmetric levels with `entry + stopLoss` and `entry − takeProfit`. Pip size is derived from the security's price step and adapts to 3- or 5-digit quotes by multiplying the tick size by 10, matching the MetaTrader conversion.
* **Position sizing** – The base lot size (`LotSize`) is validated against the instrument's `VolumeStep`, `MinVolume`, and `MaxVolume` values so orders respect exchange constraints. If money management is enabled, the strategy multiplies the lot size by an integer factor equal to the account balance divided by `DepositPerLot`, capped at 20, and updated on every bar, reproducing the integer step scaling from the original expert.

## Parameters
- **Candle Type** – Data series used for indicator calculations.
- **CCI Period** – Length of the primary CCI that drives the crossover signals.
- **CCI Close Period** – Length of the secondary CCI used to watch ±100 reversals.
- **CCI EMA Period** – Period of the EMA that smooths the primary CCI values.
- **Lot Size** – Base trading volume in lots before any scaling.
- **Enable Money Management** – Toggles balance-based scaling of the lot size.
- **Deposit Per Lot** – Balance increment required to increase the lot multiplier by one (active only when money management is on).
- **Stop Loss (pips)** – Protective stop distance in pips; set to zero to disable.
- **Take Profit (pips)** – Profit target distance in pips; set to zero to disable.

The algorithm requires two fully completed candles before it begins trading so that the two-bar crossover comparisons match the source MQL logic. Stop-loss and take-profit checks are evaluated on closed candles using their high/low extremes, which approximates MetaTrader's server-side protective orders within the high-level StockSharp API.
