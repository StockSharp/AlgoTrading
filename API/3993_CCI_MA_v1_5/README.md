# CCI MA v1.5 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy recreates the MetaTrader "CCI_MA v1.5" expert advisor inside the StockSharp high-level API. The original robot waits for the Commodity Channel Index (CCI) to cross a simple moving average calculated on the CCI values themselves and uses a secondary CCI to supervise exits around the ±100 thresholds. The StockSharp port keeps the same signal ordering, optional money management, and point-based stop/target distances while adapting everything to candle subscriptions and indicator bindings.

## How it works
* **Data source** – A user-defined candle series (15-minute candles by default) feeds both CCIs. The indicators read the candle close price to mirror the `PRICE_CLOSE` setting from MetaTrader.
* **Core indicators** – The primary `CommodityChannelIndex` (parameter `CciPeriod`) provides the momentum reading. A `SimpleMovingAverage` with period `MaPeriod` is applied to the stream of CCI values to form the trigger line. A secondary CCI (`SignalCciPeriod`) supervises overbought and oversold reversals around ±100.
* **Entry logic** – A long trade is opened on the bar following an upward crossover: the previous completed candle (`prevCci`) must sit above the CCI moving average while the candle before it (`prev2Cci`) was below. A short signal is the symmetrical crossing downward. Existing opposite positions are closed and flipped by adding the absolute value of the current position to the new order size, matching the behaviour of the MQL version.
* **Exit logic** – Longs are liquidated when the supervisory CCI drops from above +100 to below +100 or when the primary CCI crosses back under its moving average (again evaluated on the two previously finished candles). Shorts exit on the inverse conditions. Protective stops emulate the point-based distances from MetaTrader: the strategy derives a pip size from the instrument `PriceStep` (multiplying by 10 for three- or five-digit quotes) and compares the candle extremes with `entry ± distance` on each completed candle.
* **Position sizing** – `LotVolume` defines the base order size. If `UseMoneyManagement` is enabled the strategy multiplies it by an integer factor equal to `floor(balance / DepositPerLot)`, capped by `MaxMultiplier`, reproducing the deposit ladder from the expert advisor. Order volume is aligned with the instrument `VolumeStep`, `MinVolume`, and `MaxVolume` constraints before submission.

## Parameters
- **Candle Type** – Candle data type that powers all indicator calculations.
- **CCI Period** – Length of the primary CCI oscillator.
- **Exit CCI Period** – Length of the supervisory CCI used for threshold exits.
- **CCI MA Period** – Period of the simple moving average applied to the primary CCI.
- **Lot Volume** – Base trading volume before money-management scaling.
- **Enable Money Management** – Turns on deposit-based scaling of the lot volume.
- **Deposit Per Lot** – Balance increment required to raise the lot multiplier by one (used only when money management is active).
- **Max Multiplier** – Maximum multiplier that money management can reach.
- **Stop Loss (pips)** – Distance in pips for the protective stop; set to zero to disable.
- **Take Profit (pips)** – Distance in pips for the profit target; set to zero to disable.

The strategy waits for two fully closed candles before issuing the first order so that the two-bar crossover comparisons exactly match the delayed execution of the MQL expert. Stop-loss and take-profit checks run on finished candles using their high/low extremes, which approximates MetaTrader's server-side protective orders while staying within the high-level StockSharp API.
