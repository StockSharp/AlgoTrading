# Triple Moving Average Crossover Strategy

This strategy trades based on the relationship between three moving averages: fast, medium and slow. It is a conversion of the **X3MA_EA_V2_0** MQL expert.

## Trading Logic

* **Entry**
  * When *EnableEntryMediumSlowCross* is true, a long position is opened when the medium moving average crosses above the slow one. The inverse crossover triggers a short entry.
  * When the option is false, the strategy waits for the fast average to cross the medium one while both stay on the same side of the slow average. Long positions require `fast > medium > slow` and short positions require `fast < medium < slow`.
* **Exit**
  * When *EnableExitFastSlowCross* is true, open positions are closed when the fast and slow averages cross in the opposite direction.

All signals are evaluated on finished candles.

## Parameters

| Name | Description |
|------|-------------|
| `FastMaLength` | Period of the fast moving average. |
| `MediumMaLength` | Period of the medium moving average. |
| `SlowMaLength` | Period of the slow moving average. |
| `EnableEntryMediumSlowCross` | Allow entries on medium/slow crossover. |
| `EnableExitFastSlowCross` | Close positions on fast/slow crossover. |
| `CandleType` | Timeframe of candles. |

## Notes

The strategy uses the high-level API with `SubscribeCandles` and `Bind`. Indicator values are accessed through the `ProcessCandle` callback without using `GetValue`. Protective logic is enabled with `StartProtection()` in `OnStarted`.
