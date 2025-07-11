# Volume MA Cross

This strategy processes volume through fast and slow moving averages. When the fast volume MA crosses above the slow volume MA, it indicates increasing participation and triggers a long entry. A cross below signals weakness and initiates a short.

Positions are closed when the opposite crossover occurs. Price is monitored with its own moving average to help filter trades.

Volume-based signals often precede price movement, giving early entries.

## Details

- **Entry Criteria**: Fast volume MA crosses slow volume MA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Reverse crossover or stop.
- **Stops**: Yes.
- **Default Values**:
  - `FastVolumeMALength` = 10
  - `SlowVolumeMALength` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Volume MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
