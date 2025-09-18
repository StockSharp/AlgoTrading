# Exp TEMA Strategy

The **Exp TEMA Strategy** is a StockSharp port of the MetaTrader expert advisor `Exp_TEMA.mq5`. The original system scans multiple forex pairs and monitors the slope of the Triple Exponential Moving Average (TEMA). Whenever the slope flips its sign, the expert either enters a new trend-following position or exits the opposite one. This C# conversion keeps the same indicator logic while focusing on a single security that is assigned to the strategy in StockSharp.

## Trading Logic

The strategy operates on finished candles produced by the selected `CandleType` parameter. A TEMA with the configurable `TemaPeriod` length is calculated on every candle close. Three consecutive TEMA readings are compared to reproduce the slope-detection scheme of the MQL5 expert:

1. Let `tema[0]` be the latest candle value, `tema[1]` the previous one and `tema[2]` the value two candles back.
2. The short-term slope is `d1 = tema[1] - tema[2]`, while the older slope is `d2 = tema[2] - tema[3]`.
3. A **bullish entry** is triggered when the slope turns up (`d2 < 0` and `d1 > 0`). Any short position is closed first, then a long order of `Volume + |Position|` lots is placed.
4. A **bearish entry** is triggered when the slope turns down (`d2 > 0` and `d1 < 0`). Any long position is flattened first, then a short order of `Volume + |Position|` lots is sent.
5. Protective exits mimic the original stop flags: if the current slope becomes negative the long position is closed, while a positive slope closes any short.

This reproduces the same signal timing as the source EA without using historical buffer access, staying within the high-level StockSharp API.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `TemaPeriod` | 15 | Length of the Triple Exponential Moving Average. |
| `TradeVolume` | 1 | Base order volume. The executed size becomes `TradeVolume + |Position|` when reversing. |
| `StopLossPoints` | 1000 | Stop-loss distance expressed in price steps. Passed to `StartProtection` if positive. |
| `TakeProfitPoints` | 2000 | Take-profit distance expressed in price steps. Passed to `StartProtection` if positive. |
| `CandleType` | 15-minute candles | Candle type that feeds the indicator. Choose a timeframe that matches the chart used by the original expert. |

All parameters are created with `StrategyParam<T>` so they can be optimized inside Designer.

## Differences from the MQL5 Expert

- The MQL version manages up to twelve symbols simultaneously. StockSharp strategies are bound to a specific `Security`, therefore this port trades the instrument that is assigned when the strategy is launched. Run several strategy instances if multi-symbol coverage is required.
- Order management relies on `BuyMarket`/`SellMarket` and `StartProtection`, which map the original market orders, stops and targets to StockSharp's high-level API.
- Indicator access is performed through `SubscribeCandles().Bind(...)`, avoiding manual buffer copying and staying compliant with the repository guidelines.

## Usage Tips

1. Attach the strategy to the desired security and set the `CandleType` that matches your analytical timeframe.
2. Tune the stop and take-profit distances in price steps according to the instrument's volatility.
3. Optional: run optimization on `TemaPeriod`, `StopLossPoints` and `TakeProfitPoints` to replicate the parameter sweeps performed in MetaTrader.
4. Monitor the included chart area to visualize candles, the TEMA line and the executed trades.
