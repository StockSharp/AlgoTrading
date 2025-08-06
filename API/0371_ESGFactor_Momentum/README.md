# ESG Factor Momentum Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy rotates among a universe of securities scored on environmental, social, and governance metrics. At the start of each month it ranks every symbol by its trailing return and holds only the strongest performer. The premise is that assets attracting ESG capital tend to sustain momentum. To avoid excessive turnover, the algorithm only trades when the position value exceeds a minimum dollar threshold.

During rebalancing the system exits any existing position and reallocates to the highest‑momentum security. The portfolio never uses leverage or shorts; it is fully invested in a single asset selected by momentum strength.

## Details

- **Entry Criteria**:
  - On the first trading day of the month, compute total return over `LookbackDays` for each security.
  - Buy the security with the highest return if the order size is at least `MinTradeUsd`.
- **Long/Short**: Long only.
- **Exit Criteria**: All positions are closed at each monthly rebalance before opening the new position.
- **Stops**: None.
- **Default Values**:
  - `Universe` – list of ESG‑focused symbols.
  - `LookbackDays` = 252.
  - `CandleType` = 1 day.
  - `MinTradeUsd` – minimum trade value.
- **Filters**:
  - Category: Momentum.
  - Direction: Long only.
  - Timeframe: Medium‑term.
  - Rebalance: Monthly.

