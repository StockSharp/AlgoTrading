# Hull Ma Adx Strategy

Strategy based on Hull Moving Average and ADX. Enters long when HMA
increases and ADX > 25 (strong trend). Enters short when HMA decreases
and ADX > 25 (strong trend). Exits when ADX < 20 (weakening trend).

Hull MA shows the trend, while ADX confirms its intensity. Entries follow the Hull slope when ADX indicates strength.

Effective for traders who focus on smooth trends with confirmation. ATR stops keep losses under control.

## Details

- **Entry Criteria**:
  - Long: `HullMA turning up && ADX > 25`
  - Short: `HullMA turning down && ADX > 25`
- **Long/Short**: Both
- **Exit Criteria**: Hull MA reversal
- **Stops**: ATR-based using `AtrMultiplier`
- **Default Values**:
  - `HmaPeriod` = 9
  - `AdxPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Hull MA, Moving Average, ADX
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
