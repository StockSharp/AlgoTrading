# Gandalf PRO Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the Gandalf PRO expert advisor from MQL. It computes two moving averages (LWMA and SMA) on the candle close
and feeds them into the original recursive smoothing logic to forecast a future target price. When the projected target moves far
enough away from the current close (15 price steps or more), the strategy opens a market order and stores stop-loss and take-profit
levels derived from the forecast.

Long trades require the smoothed target to be above the current close by at least 15 steps; short trades require the target to be
below the close by the same margin. Stop losses are defined in price steps and converted using the security price step. Take-profit
levels are equal to the projected target and are monitored on every finished candle. The risk multipliers rescale the base strategy
volume, enabling simple money management rules.

## Parameters
- Candle Type
- Enable Buy
- Buy Length
- Buy Price Factor
- Buy Trend Factor
- Buy Stop Loss
- Buy Risk Multiplier
- Enable Sell
- Sell Length
- Sell Price Factor
- Sell Trend Factor
- Sell Stop Loss
- Sell Risk Multiplier
