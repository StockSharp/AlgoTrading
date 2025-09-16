# MasterMind 3 Strategy

This strategy trades extreme reversals using four **Williams %R** indicators with different periods. When all indicators drop to deep oversold values, the strategy enters a long position. When all indicators rise to strong overbought values, it enters a short position.

## Trading Logic

1. Subscribe to candles of the selected timeframe.
2. Calculate four Williams %R indicators with periods 26, 27, 29 and 30.
3. **Buy** when all indicators are below `-99.99`.
4. **Sell** when all indicators are above `-0.01`.
5. Signals are processed only on finished candles.

The volume of the order is taken from the strategy `Volume` property. Existing opposite positions are closed automatically by sending a market order with the required size.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `WprPeriod1` | Length of the first Williams %R indicator | 26 |
| `WprPeriod2` | Length of the second Williams %R indicator | 27 |
| `WprPeriod3` | Length of the third Williams %R indicator | 29 |
| `WprPeriod4` | Length of the fourth Williams %R indicator | 30 |
| `CandleType` | Type and timeframe of candles | 1 minute candles |

## Notes

* The strategy uses high-level API with `Bind` for indicator processing.
* No stop-loss or take-profit levels are included; position is reversed on opposite signals.

