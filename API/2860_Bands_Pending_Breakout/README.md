# Bands Pending Breakout Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the MetaTrader "Bands 2" expert advisor on top of the StockSharp high-level API. It monitors finished candles, checks that the current time is inside the configured trading window and that price is trading inside the Bollinger channel. Whenever those conditions are met it places a symmetric grid of three buy stop and three sell stop orders around the Bollinger envelope. Each order carries its own stop-loss and take-profit distances and any fill removes the other pending orders.

The approach is designed for breakouts from the Bollinger bands. The stop-loss reference can be switched between the opposite band or the central moving average. A separate trailing stop module continuously tightens the protective stop once the position moves in profit by a configurable step.

## Details

- **Market Data**: Works on any instrument/candle type provided through StockSharp.
- **Trading Hours**: Uses `HourStart`/`HourEnd` to restrict order placement. Orders are refreshed on every finished candle inside that window.
- **Entry Logic**:
  - Wait for a finished candle with close price strictly between the shifted upper and lower Bollinger bands.
  - Delete leftover pending orders from the previous bar and place three buy stops above the upper band and three sell stops below the lower band.
  - Each tier is separated by `StepPips` converted to ticks.
- **Stop-Loss Modes**:
  - *BollingerBands*: Stop-loss uses the opposite band offset by the same step distance as the entry order.
  - *MovingAverage*: Stop-loss uses the moving average value plus/minus the step distance (uses the configured applied price and method).
  - *None*: No initial stop is set; trailing stop can still activate later.
- **Take-Profit Logic**:
  - First level uses `FirstTakeProfitPips` for both buy and sell orders.
  - Second and third buy orders use `Second`/`Third` take-profit distances, while sell orders follow the original MQL script behaviour and always reuse the first take-profit distance.
- **Order Management**:
  - When any pending order fills the strategy cancels all other entry orders and creates market-independent protective orders (stop + limit) for the filled volume.
  - The trailing module moves the stop order toward the market once price moves by `TrailingStopPips + TrailingStepPips` from the entry.
  - Protective stop/limit orders are cancelled automatically when the position goes flat.
- **Price Normalisation**: All price levels are rounded to the instrument tick size and the point-to-pip conversion mimics the original 3/5 digit handling.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Volume for each pending order (same volume for all six orders). |
| `CandleType` | Timeframe/data type used for indicator calculations. |
| `HourStart`, `HourEnd` | Inclusive/exclusive hours (0-24) that allow placing new pending orders. `HourEnd` must be greater than `HourStart`. |
| `StopLossMode` | Placement reference for initial stop-loss (`BollingerBands`, `MovingAverage`, `None`). |
| `FirstTakeProfitPips`, `SecondTakeProfitPips`, `ThirdTakeProfitPips` | Take-profit distances (in pips) converted to price offsets for the first, second and third entries. |
| `TrailingStopPips`, `TrailingStepPips` | Trailing stop distance and the extra step required before the stop is advanced. Set zero to disable trailing. |
| `StepPips` | Spacing between consecutive pending orders (converted to price). |
| `MaPeriod`, `MaShift`, `MaMethod`, `MaPriceType` | Moving average configuration used for Bollinger input and optionally for stop placement when `StopLossMode` is `MovingAverage`. The `MaShift` emulates the forward shift of the original EA. |
| `BandsPeriod`, `BandsShift`, `BandsDeviation`, `BandsPriceType` | Bollinger band settings (period, shift, deviation multiplier and applied price). |

## Behaviour Summary

1. Subscribe to finished candles of the selected timeframe.
2. On each finished candle inside the trading window, compute the shifted Bollinger bands and the moving average using the selected applied prices.
3. Ensure the candle close is within the band channel, then place the buy/sell stop grid around the channel edges with individual stops and targets.
4. When an order fills, cancel the remaining entry orders, submit protective stop/limit orders and start trailing according to the configured parameters.
5. Close protective orders when the position exits, ready for the next breakout opportunity.
