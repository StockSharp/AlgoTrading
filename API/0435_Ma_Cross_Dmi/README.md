# MA Cross + DMI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades a crossover of fast and slow exponential moving averages only when the Directional Movement Index confirms trend strength. By waiting for +DI or -DI to dominate while ADX rises above a key level, the system filters out weak crossovers.

This strategy can enter long or short positions and exits on opposing crossovers. ADX filtering helps the method stay out of ranging periods where moving averages frequently whipsaw.

## Details

- **Entry Criteria**:
  - **Long**: Fast EMA crosses above slow EMA, +DI > -DI, and ADX above the key level.
  - **Short**: Fast EMA crosses below slow EMA, -DI > +DI, and ADX above the key level.
- **Exit Criteria**:
  - Opposite crossover or manual stop.
- **Indicators**:
  - Two EMAs (periods 10 and 20)
  - Directional Movement Index (length 14, ADX smoothing 14)
- **Stops**: None by default; can use StartProtection.
- **Default Values**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `DmiLength` = 14
  - `AdxSmoothing` = 14
  - `KeyLevel` = 20
- **Filters**:
  - Trend following
  - Works on intraday to swing timeframes
  - Indicators: EMA, DMI
  - Stops: Optional
  - Complexity: Basic
