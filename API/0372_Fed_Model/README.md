# Fed Model Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This macro timing system compares the earnings yield of the equity market to the yield on 10‑year Treasury notes. When stocks offer a higher yield, the strategy holds an equity ETF; when bonds yield more, it moves to cash. A monthly regression on the yield gap forecasts the next month’s value to reduce noisy switches.

At the end of each month the algorithm forecasts the coming month’s yield spread using the last year of data. If the forecast is positive it buys equities, otherwise it holds the cash proxy. Positions change only when the forecast crosses zero, minimizing turnover.

## Details

- **Entry Criteria**:
  - At month end, regress the last `RegressionMonths` observations of `(EarningsYield - BondYield)` and forecast the next value.
  - Buy the equity ETF when the forecast is above zero and the order meets `MinTradeUsd`.
- **Long/Short**: Long equities or cash only.
- **Exit Criteria**: Exit the equity position when the forecast yield spread turns negative.
- **Stops**: None.
- **Default Values**:
  - `Universe` – [equity ETF, optional cash ETF].
  - `BondYieldSym` – 10‑year Treasury yield series.
  - `EarningsYieldSym` – equity market earnings yield.
  - `RegressionMonths` = 12.
  - `CandleType` = 1 day.
  - `MinTradeUsd` – minimum trade value.
- **Filters**:
  - Category: Macro.
  - Direction: Long only.
  - Timeframe: Monthly.
  - Rebalance: Monthly.

