[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades on the crossover of two Commodity Channel Index (CCI) indicators. A fast CCI reacts quickly to price changes while a slow CCI smooths market noise. Signals are generated when the fast line crosses the slow one.

## Details

- **Entry Criteria**:
  - Long: fast CCI crosses above slow CCI.
  - Short: fast CCI crosses below slow CCI.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Long positions closed when fast CCI crosses below slow CCI.
  - Short positions closed when fast CCI crosses above slow CCI.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = 6-hour candles
  - `FastLength` = 14
  - `SlowLength` = 6
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
- **Filters**:
  - Category: Trend-following
  - Direction: Both
  - Indicators: CCI
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
