# Exp Cronex AO Strategy

This strategy ports the MetaTrader expert advisor **Exp_CronexAO** to the StockSharp high-level API. The original robot trades crosses between the two lines of the Cronex Awesome Oscillator (AO). The StockSharp version subscribes to a configurable candle series, computes the AO, smooths it twice with moving averages to reproduce the Cronex lines, and opens or closes positions when the fast line crosses the slow line.

## Trading logic

1. Build the Awesome Oscillator from the selected candles.
2. Smooth the oscillator twice with simple moving averages. The first smoothing creates the "fast" Cronex line, the second smoothing produces the "signal" line.
3. Look back `SignalBar` completed candles and compare the Cronex lines on that bar and on the previous one.
4. A **buy** signal appears when the fast line is above the slow line and performed an upward cross on the lookback bar. The strategy optionally closes any short position and, if allowed, opens a long market order.
5. A **sell** signal mirrors the previous rule: the fast line must be below the slow line and must have crossed downward on the lookback bar. The strategy optionally closes any long position and, if allowed, opens a short market order.
6. Stop-loss and take-profit levels, expressed in instrument points, are attached to the resulting position whenever a new trade is opened.

Only one net position is maintained. When the direction changes, the strategy combines the volume required to close the opposite position with the new trade volume to emulate MetaTrader's netting mode.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Data type of the candles used for the Cronex AO calculations. The default is an 8-hour time frame. |
| `FastPeriod` | Length of the first smoothing applied to the Awesome Oscillator. |
| `SlowPeriod` | Length of the second smoothing applied to the fast line. |
| `SignalBar` | Number of completed bars back that must contain the cross signal. The strategy also inspects the following bar to confirm the direction. |
| `BuyOpenEnabled` / `SellOpenEnabled` | Enable or disable opening of long or short positions. |
| `BuyCloseEnabled` / `SellCloseEnabled` | Control whether opposite positions may be closed when an inverse signal appears. |
| `TakeProfit` | Profit target in points, applied after every new entry if greater than zero. |
| `StopLoss` | Protective stop in points, also applied after every new entry if greater than zero. |

## Risk management

The stop-loss and take-profit distances mimic the point-based inputs of the MetaTrader version. They are recalculated every time a new trade is sent so that protective orders always match the current net position size.

## Differences from the MetaTrader version

- The StockSharp implementation uses simple moving averages for both Cronex smoothing stages. The original XMA implementation allows several smoothing methods, but the default configuration corresponds to the simple average that is reproduced here.
- Slippage and money-management routines from the `TradeAlgorithms` library are not replicated. Position sizing is controlled via the standard `Volume` property.
- Trade execution relies on StockSharp's netting behaviour. When the direction reverses, a single market order is issued with enough volume to flatten and flip the position in one step, mirroring the MT5 netting account logic.

