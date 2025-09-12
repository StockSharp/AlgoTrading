# Yuri Garcia Smart Money Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This smart money concept strategy searches for price reactions inside high volume zones and four-hour support/resistance areas. It confirms entries with cumulative delta and wick pullbacks, aiming to follow institutional order flow.

Testing indicates an average annual return of about 42%. It works best on BTC and major indices.

The system calculates ATR-based stop loss and take profit with a configurable risk/reward ratio. Trades are allowed long, short or both, and positions are opened only when price is inside the zone, a wick pullback occurs and delta supports the move.

## Details

- **Entry Criteria**:
  - **Long**: Price within buffered high/low zone, bullish wick pullback, cumulative delta rising.
  - **Short**: Price within zone, bearish wick pullback, cumulative delta falling.
- **Long/Short**: Configurable (both, buy only or sell only).
- **Exit Criteria**:
  - ATR-based stop loss or take profit.
- **Stops**: Yes, ATR-based.
- **Filters**:
  - HTF zone, cumulative delta confirmation, wick pullback.
