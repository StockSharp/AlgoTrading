# Manual EA Strategy

The **Manual EA Strategy** is a one-to-one StockSharp high-level API conversion of the MetaTrader 4 expert advisor *Manual_EA.mq4* (folder `MQL/8159`). The original system issues discretionary buy or sell orders whenever the Stochastic oscillator leaves extreme zones. The StockSharp port keeps the same 5-3-3 oscillator configuration, automatically nets existing exposure before placing the next market order, and exposes the common money-management options through strategy parameters.

## Trading logic

1. The strategy subscribes to the `CandleType` series (default: 15-minute candles) and feeds the close prices into a Stochastic Oscillator configured with:
   - `%K` lookback = `KPeriod` (default 5 bars)
   - `%K` slowing = `Slowing` (default 3 bars)
   - `%D` smoothing = `DPeriod` (default 3 bars)
2. Signals are evaluated on the final value of the %D (signal) line of each finished candle. Two consecutive readings are compared to detect level crossings.
3. **Long entry** – When the previous %D value was below or equal to `OversoldLevel` (default 10) and the latest value rises above that threshold. The strategy first neutralizes any short exposure and then buys `Volume + |short position|` by market order.
4. **Short entry** – When the previous %D value was above or equal to `OverboughtLevel` (default 90) and the latest value falls below that threshold. Any existing long position is closed before selling `Volume + |long position|` at market.
5. Protective orders are handled via `StartProtection`. A positive `StopLoss` and/or `TakeProfit` (measured in price points) activates automatic risk management. Setting a parameter to `0` disables the corresponding protection.

The port deliberately avoids indicator buffer access patterns and unfinished-candle logic, complying with StockSharp high-level API best practices.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Timeframe (as `DataType`) used to build candles and drive the oscillator. | 15-minute time frame |
| `KPeriod` | Lookback length of the Stochastic %K line. | 5 |
| `DPeriod` | Smoothing length of the Stochastic %D signal line. | 3 |
| `Slowing` | Additional smoothing applied to %K before %D is computed. | 3 |
| `OverboughtLevel` | Upper bound that triggers short entries when crossed downward by %D. | 90 |
| `OversoldLevel` | Lower bound that triggers long entries when crossed upward by %D. | 10 |
| `StopLoss` | Distance in price points for the protective stop-loss (0 = disabled). | 100 |
| `TakeProfit` | Distance in price points for the take-profit target (0 = disabled). | 100 |
| `Volume` | Order size sent with each new signal (lots). Existing opposite positions are netted first. | 0.1 |

## Additional notes

- The strategy uses `SubscribeCandles` together with `BindEx` to stream `StochasticOscillatorValue` updates, ensuring indicator values are final before trading decisions are taken.
- Chart visualization automatically plots the selected candle series, the Stochastic oscillator, and own trades when a chart area is available.
- Because %D crossings are evaluated on consecutive finished candles, the behaviour matches the MQL implementation that compared `MODE_SIGNAL` values at shifts 1 and 2.
