# Parabolic SAR MultiTimeframe
[Русский](README_ru.md) | [中文](README_cn.md)

Parabolic SAR MultiTimeframe uses four different Parabolic SAR indicators from higher timeframes
to confirm a trend before entering a trade. The strategy processes 15-minute candles and checks the
state of SAR on 30-minute, 1-hour and 4-hour charts. A long position is opened only when price is
above all SAR values; a short position is opened when price is below all SARs.

The method attempts to filter out noise by requiring alignment across multiple timeframes. Position
is closed when the opposite condition appears.

## Details

- **Entry Criteria**: Price relative to Parabolic SAR on 15m/30m/1h/4h timeframes.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal from all SAR indicators.
- **Stops**: Uses `StartProtection` for basic protection, no explicit stop values.
- **Default Values**:
  - `Step15` = 0.062
  - `Step30` = 0.058
  - `Step60` = 0.058
  - `Step240` = 0.058
  - `MaxStep` = 0.1
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Parabolic SAR
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday (15m base with higher confirmations)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

## Usage

1. Attach the strategy to a security.
2. Adjust SAR step parameters if needed.
3. Start the strategy; it will subscribe to 15m, 30m, 1h and 4h candles automatically.

