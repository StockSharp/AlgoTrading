# DMI Power Move

Strategy based on DMI (Directional Movement Index) power moves

DMI Power Move combines directional indicator differences with ADX to catch powerful trends. Trades enter when +DI markedly exceeds -DI (or vice versa) and ADX is strong. They exit when ADX fades or the DI spread narrows.

This approach filters out weak signals by requiring both strong directional movement and rising ADX. The result is fewer, but potentially higher-quality, trend trades.


## Details

- **Entry Criteria**: Signals based on ADX, ATR, DMI.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `DmiPeriod` = 14
  - `DiDifferenceThreshold` = 5m
  - `AdxThreshold` = 30m
  - `AdxExitThreshold` = 25m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ADX, ATR, DMI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
