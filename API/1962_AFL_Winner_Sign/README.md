# AFL Winner Sign Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the AFL WinnerSign indicator. It applies a double-smoothed stochastic oscillator to a volume-weighted price series. A long position is opened when the fast stochastic line crosses above the slow line, and a short position is opened when the fast line crosses below the slow line.

## Details

- **Entry Criteria**:
  - Long: fast %K crosses above slow %D
  - Short: fast %K crosses below slow %D
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal closes or reverses the position
- **Stops**: Percent-based using `StartProtection`
- **Default Values**:
  - `Period` = 10
  - `KPeriod` = 5
  - `DPeriod` = 5
  - `CandleType` = `TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Stochastic Oscillator
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
