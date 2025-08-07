# Pin Bar Magic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Detects bullish and bearish pin bars within a trend defined by a trio of moving averages. Orders are placed at the candle extremes and cancelled after a few bars if not filled. Position size is calculated from an equity risk percentage and ATR-based stop distance.

The method aims to capture sharp reversals at significant support or resistance. It exits positions when the fast and medium EMAs cross in the opposite direction, signalling trend weakness.

## Details

- **Entry Criteria**:
  - **Long**: Fast EMA > Medium EMA > Slow SMA, bullish pin bar piercing one of the averages.
  - **Short**: Fast EMA < Medium EMA < Slow SMA, bearish pin bar piercing one of the averages.
- **Exit Criteria**:
  - Fast EMA crosses the medium EMA in the opposite direction.
- **Indicators**:
  - Slow SMA (period 50)
  - Medium EMA (18) and Fast EMA (6)
  - ATR (length 14)
- **Stops**: Position risk = EquityRisk% of account with stop at ATR * multiplier.
- **Default Values**:
  - `EquityRisk` = 3
  - `AtrMultiplier` = 0.5
  - `SlowSmaLength` = 50
  - `MediumEmaLength` = 18
  - `FastEmaLength` = 6
  - `AtrLength` = 14
  - `CancelEntryBars` = 3
- **Filters**:
  - Price action reversal
  - Works on 1h candles by default
  - Indicators: EMA, SMA, ATR
  - Stops: Yes
  - Complexity: High
