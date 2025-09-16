# Waddah Attar Win Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy mirrors the original Waddah Attar Win expert advisor. It continuously maintains a symmetric grid of buy- and sell-limit orders spaced at a fixed number of points from the current bid/ask. Whenever the market price approaches the last submitted order, the strategy stacks a new limit at the same distance with an optional volume increment. Floating profit is monitored on every order book update and all positions together with pending orders are closed once the configured profit target in account currency is reached.

## Details

- **Entry Criteria**:
  - Initial buy-limit placed `Step Points` below the bid and sell-limit placed the same distance above the ask.
  - Additional pending orders are added when price comes within five price steps of the latest order on each side.
- **Long/Short**: Both, hedged grid.
- **Exit Criteria**:
  - Close all positions and cancel orders once equity exceeds the stored balance by `Min Profit`.
- **Stops**: None.
- **Default Values**:
  - `Step Points` = 20
  - `First Volume` = 0.1
  - `Increment Volume` = 0.0
  - `Min Profit` = 910
- **Notes**:
  - Works with hedging portfolios because long and short positions can coexist.
  - Uses order book data to react immediately to bid/ask changes.
