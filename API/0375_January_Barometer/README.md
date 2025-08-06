# January Barometer Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The January Barometer states that the market’s performance in January sets the tone for the remainder of the year. This strategy invests in an equity ETF for the rest of the year only if January closes higher; otherwise it stays in a cash proxy. The allocation decision is made once per year and held until year end.

On the first trading day of February the algorithm measures the total return of the equity ETF during January. If the return is positive and the order value exceeds the minimum threshold, it buys the equity ETF and holds it through December. If January was negative, it holds the cash ETF instead. The process repeats each year.

## Details

- **Entry Criteria**:
  - On the first trading day of February calculate the total January return of `EquityETF`.
  - Buy `EquityETF` if the return is positive and order size >= `MinTradeUsd`; otherwise hold `CashETF`.
- **Long/Short**: Long equities or cash only.
- **Exit Criteria**: Close the equity position on the last trading day of the year.
- **Stops**: None.
- **Default Values**:
  - `EquityETF` – ETF representing the equity market.
  - `CashETF` – cash proxy ETF.
  - `CandleType` = 1 day.
  - `MinTradeUsd` – minimum trade value.
- **Filters**:
  - Category: Seasonal.
  - Direction: Long only.
  - Timeframe: Long‑term.
  - Rebalance: Annually.

