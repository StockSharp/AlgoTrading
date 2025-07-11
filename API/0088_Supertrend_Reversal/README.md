# Supertrend Reversal Strategy

The Supertrend indicator combines ATR and price to produce trailing support or resistance. When the Supertrend line flips from above to below price or vice versa, it suggests a potential trend change. This strategy trades those flips.

On each candle an ATR-based calculation updates the Supertrend level. A switch from above price to below triggers a long entry, while a move from below to above creates a short. The code sample omits explicit stops, so exits are discretionary or managed by a separate risk module.

The indicator can react quickly to volatility, so traders often combine it with additional filters to reduce whipsaws.

## Details

- **Entry Criteria**: Supertrend switches sides relative to price.
- **Long/Short**: Both.
- **Exit Criteria**: Manual or external stop.
- **Stops**: Not defined.
- **Default Values**:
  - `Period` = 10
  - `Multiplier` = 3.0
  - `CandleType` = 15 minute
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Supertrend
  - Stops: Optional
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
