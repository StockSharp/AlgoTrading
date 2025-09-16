# Anubis Strategy

## Overview
The Anubis strategy combines multi-timeframe volatility and momentum filters to capture reversals against strong countertrend spikes. The original expert advisor from MetaTrader 5 used H4 indicators to gate entries and M15 signals for timing. This conversion keeps the same structure while adapting the logic to StockSharp's high-level API and providing rich runtime telemetry.

## Strategy Logic
- **Timeframes**
  - Main signal timeframe: configurable candle type (15-minute candles by default).
  - Higher timeframe confirmation: fixed 4-hour candles used for CCI and standard deviations.
- **Indicators**
  - *Commodity Channel Index (CCI)* on the higher timeframe detects overbought/oversold extremes.
  - *Two standard deviations* on the higher timeframe provide volatility measurements for take-profit sizing.
  - *MACD* on the signal timeframe supplies momentum crossover confirmation.
  - *Average True Range (ATR)* on the signal timeframe defines abnormal candle range exits.
- **Entry Conditions**
  - **Longs:** CCI falls below `-CciThreshold`, MACD main line crosses above the signal line, and the previous MACD histogram was negative.
  - **Shorts:** CCI rises above `+CciThreshold`, MACD main line crosses below the signal line, and the previous MACD histogram was positive.
  - The strategy optionally closes an opposite position before stacking a new one and enforces a minimum price spacing between consecutive entries.
- **Position Management**
  - Up to `MaxLongPositions` or `MaxShortPositions` stacked entries are allowed, each opened with `TradeVolume` contracts.
  - Stop-loss and take-profit distances are derived from pip-based settings and higher timeframe volatility.
  - Once price moves by `BreakevenPips`, the protective stop is lifted to the average entry price.
- **Exit Conditions**
  - Hard stops: stop-loss and take-profit levels are monitored on every closed candle.
  - Range exits: positions close if the previous candle range exceeds `CloseAtrMultiplier × ATR`.
  - Momentum exits: positions with sufficient profit close when MACD momentum flips against the trade and the gain exceeds `ThresholdPips`.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `TradeVolume` | 1 | Order size for each entry. |
| `CciThreshold` | 80 | Absolute CCI level on the 4-hour chart used to detect extremes. |
| `CciPeriod` | 11 | CCI lookback length on the higher timeframe. |
| `StopLossPips` | 100 | Stop-loss distance expressed in pips. Set to 0 to disable the initial stop. |
| `BreakevenPips` | 65 | Profit distance in pips before moving the stop to breakeven. |
| `ThresholdPips` | 28 | Additional profit cushion required before MACD-based exits trigger. |
| `TakeStdMultiplier` | 2.9 | Multiplier applied to the slow standard deviation when computing take-profit distance. |
| `CloseAtrMultiplier` | 2 | Multiplier of the signal timeframe ATR used for range-based exits. |
| `SpacingPips` | 20 | Minimum price distance between consecutive entries in the same direction. |
| `MaxLongPositions` | 2 | Maximum number of simultaneous long entries. |
| `MaxShortPositions` | 2 | Maximum number of simultaneous short entries. |
| `MacdFastLength` | 20 | Fast EMA length for MACD on the signal timeframe. |
| `MacdSlowLength` | 50 | Slow EMA length for MACD on the signal timeframe. |
| `MacdSignalLength` | 2 | Signal smoothing length for MACD. |
| `AtrLength` | 12 | ATR lookback period on the signal timeframe. |
| `StdFastLength` | 20 | Period for the fast standard deviation (used for diagnostics). |
| `StdSlowLength` | 30 | Period for the slow standard deviation that drives the take-profit distance. |
| `CandleType` | 15m candles | Main timeframe used for MACD and ATR calculations. |

## Trading Notes
- The higher timeframe is fixed at four hours; adjust `CandleType` if you wish to synchronize the main signal timeframe with different markets.
- Because StockSharp aggregates net positions by default, long and short exposure are not held simultaneously; an opposite signal will flatten the open position before placing the new order.
- Standard deviation calculation follows StockSharp's implementation. The slow length approximates the EMA-based deviation from the original MQL version.
- Ensure the selected security exposes a valid `PriceStep` so pip-based parameters translate accurately into price distances.
