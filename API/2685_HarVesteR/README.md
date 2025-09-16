# HarVesteR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The HarVesteR strategy blends MACD momentum with two simple moving averages and an optional ADX strength filter.
It looks for situations where price hugs the moving averages while MACD has recently crossed the zero line, signalling a potential breakout from consolidation.
Stops are attached to swing highs or lows, half of the position is booked at a fixed reward multiple, and the remainder is protected with a break-even exit driven by the fast moving average.

## Details

- **Entry Criteria**:
  - Long: `MACD > 0 && MACD history contains negative value && Close < SlowSMA && Close + Indentation > FastSMA && Close + Indentation > SlowSMA && ADX ≥ AdxBuyLevel (if enabled)`
  - Short: `MACD < 0 && MACD history contains positive value && Close > SlowSMA && Close - Indentation < FastSMA && Close - Indentation < SlowSMA && ADX ≥ AdxSellLevel (if enabled)`
- **Stop Loss**: Latest swing low/high over `StopLookback` completed candles.
- **Partial Exit**: Closes half the position once price moves `HalfCloseRatio` times the distance between entry and stop, then moves the stop to break-even.
- **Final Exit**:
  - Long: closes the rest if price dips below `FastSMA + Indentation` after the stop is at break-even.
  - Short: closes the rest if price rises above `FastSMA + Indentation` after the stop is at break-even.
- **Long/Short**: Both directions supported.
- **Filters**: Optional ADX trend-strength filter; set `UseAdxFilter` to `false` to disable it.
- **Position Management**: Reverses the position by netting the opposite signal volume plus the current exposure.

## Parameters

| Name | Default | Description |
|------|---------|-------------|
| `MacdFast` | 12 | Fast EMA period for the MACD difference line. |
| `MacdSlow` | 24 | Slow EMA period for the MACD difference line. |
| `MacdSignal` | 9 | Signal EMA period for MACD smoothing. |
| `MacdLookback` | 6 | Number of recently finished candles checked for a MACD sign change. |
| `SmaFastLength` | 50 | Length of the fast simple moving average. |
| `SmaSlowLength` | 100 | Length of the slow simple moving average. |
| `MinIndentation` | 10 | Offset in pips applied around the moving averages before entering or exiting. |
| `StopLookback` | 6 | Swing-high/low lookback used to seed the initial stop level. |
| `UseAdxFilter` | false | Enables the ADX strength filter for both directions. |
| `AdxBuyLevel` | 50 | Minimum ADX level required to allow long entries when the filter is enabled. |
| `AdxSellLevel` | 50 | Minimum ADX level required to allow short entries when the filter is enabled. |
| `AdxPeriod` | 14 | Period used for the ADX calculation. |
| `HalfCloseRatio` | 2 | Multiplier applied to the entry-to-stop distance before taking partial profits. |
| `Volume` | 1 | Order volume for new entries (netting against any opposite exposure). |
| `CandleType` | 1 hour | Primary timeframe used to build candles and indicators. |

## Notes

- `MinIndentation` is converted to price distance using the instrument tick size. Instruments quoted with three or five decimals receive a tenfold adjustment to approximate pip units.
- When `UseAdxFilter` is disabled the strategy accepts signals in both directions without checking the ADX value.
- Partial profit taking and break-even exits run on every finished candle to protect open positions even when no new trades are allowed.
