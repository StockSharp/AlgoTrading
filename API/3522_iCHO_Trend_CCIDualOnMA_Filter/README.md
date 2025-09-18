# iCHO Trend CCIDualOnMA Filter Strategy

This strategy is a high-level StockSharp port of the MetaTrader expert advisor **"iCHO Trend CCIDualOnMA Filter"**. It mixes a Chaikin oscillator zero-line regime filter with a dual Commodity Channel Index (CCI) confirmation that is calculated on top of a smoothed price series. The result is a trend-following approach that reacts to momentum shifts but still requires a momentum confirmation from the CCI pair before entering a trade.

## Trading logic

1. **Chaikin oscillator core** – the accumulation/distribution line is smoothed by two configurable moving averages. Their difference replicates the Chaikin oscillator. Crosses above/below zero signal a change in the dominant capital flow.
2. **Dual CCI filter** – both CCI instances use the same moving-average-smoothed price input but different lookback periods. A long setup requires the fast CCI to recover from negative territory and cross above the slow CCI while the Chaikin oscillator stays above zero. A short setup mirrors these conditions.
3. **Optional reversal** – the original EA provides a “reverse” flag that swaps long and short signals. The port keeps this behaviour so that the same rules can be used for counter-trend testing.
4. **Position management** – optional flags close the opposite exposure before opening a new position and limit the strategy to a single open position. A one-trade-per-bar rule is enforced to mimic the MetaTrader implementation.
5. **Session filter** – trading can be restricted to a user-defined intraday window, including wrap-around sessions that pass midnight.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `FastChaikinLength` | Fast moving-average period used inside the Chaikin oscillator. |
| `SlowChaikinLength` | Slow moving-average period used inside the Chaikin oscillator. |
| `ChaikinMethod` | Moving-average method (Simple, Exponential, Smoothed, LinearWeighted) applied to the accumulation/distribution line. |
| `FastCciLength` | Lookback of the fast Commodity Channel Index. |
| `SlowCciLength` | Lookback of the slow Commodity Channel Index. |
| `MaLength` | Length of the preprocessing moving average that feeds the CCIs. |
| `MaMethod` | Moving-average method used for preprocessing price before it reaches the CCIs. |
| `MaPrice` | Price type (close, open, high, low, median, typical, weighted) that is smoothed before the CCIs. |
| `UseClosedBar` | Process only fully finished candles (default true, identical to `SignalsBarCurrent=bar_1` in the EA). |
| `ReverseSignals` | Swap long and short logic. |
| `CloseOpposite` | Close an open position in the opposite direction before entering a new one. |
| `OnlyOnePosition` | Allow only a single open position at any time. |
| `TradeMode` | Restrict execution to longs, shorts or both (BuyOnly, SellOnly, BuyAndSell). |
| `UseTimeFilter` | Enable the trading session filter. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Session boundaries (inclusive of the start, exclusive of the end) expressed in exchange time. Wrap-around sessions are supported. |
| `CandleType` | Timeframe of the candle subscription feeding the indicators. |

## Notes

- The strategy uses only high-level `SubscribeCandles` bindings and built-in indicators; no custom buffers or historical requests are required.
- All price-based calculations adopt the same moving-average preprocessing as the MetaTrader `CCIDualOnMA` indicator by feeding the CCI with a smoothed price series.
- The default parameters reproduce the original EA defaults: Chaikin 3/10 EMA, CCI periods 14 and 50, 12-period SMA preprocessing and a trading window from 10:01 to 15:02.
