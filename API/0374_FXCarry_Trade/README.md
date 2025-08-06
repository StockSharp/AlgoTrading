# FX Carry Trade Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This currency strategy ranks a universe of currency instruments by the interest rate differential between the base and quote currencies. At the start of each month it goes long the `TopK` highest‑carry symbols and shorts the `TopK` lowest. Profits aim to capture positive carry on longs while paying the negative carry on shorts.

Interest rate differentials are gathered from each security’s yield data. Positions are sized equally and rebalanced monthly; any instrument leaving the top or bottom groups is closed and replaced.

## Details

- **Entry Criteria**:
  - On the first trading day of the month, compute the interest rate differential for each currency.
  - Go long the `TopK` currencies with the highest carry and short the `TopK` with the lowest carry if order values exceed `MinTradeUsd`.
- **Long/Short**: Long top carry, short bottom carry.
- **Exit Criteria**: Positions are closed when a currency leaves the selected sets at the next rebalance.
- **Stops**: None.
- **Default Values**:
  - `Universe` – list of currency securities.
  - `TopK` = 3.
  - `CandleType` = 1 day.
  - `MinTradeUsd` – minimum trade value.
- **Filters**:
  - Category: Carry.
  - Direction: Long & short.
  - Timeframe: Monthly.
  - Rebalance: Monthly.

