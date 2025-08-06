# Crude Oil Predicts Equity Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the relationship between crude oil and equity returns. If the trailing one‑month return on crude oil is positive, the strategy invests in an equity ETF. Otherwise it rotates the capital into a cash or bond ETF, staying out of equities when oil is weak.

The algorithm monitors daily candles and checks the signal on the first trading day of each month. Orders are submitted at market prices and respect a minimum trade size to avoid tiny fills.

## Details

- **Universe**: One equity ETF, one crude oil instrument, and a cash or bond ETF.
- **Signal**: Go long the equity ETF when crude oil's `Lookback` period return is greater than zero; otherwise hold the cash ETF.
- **Rebalance**: Monthly, at the start of the month.
- **Positioning**: Long equity or cash, never both.
- **Parameters**:
  - `Equity` – target equity ETF.
  - `Oil` – crude oil security for the signal.
  - `CashEtf` – defensive asset when oil return is negative.
  - `Lookback` – number of candles used to compute oil return.
  - `CandleType` – candle timeframe (default: 1 day).
- **Note**: The sample focuses on structure and omits transaction costs and slippage.
