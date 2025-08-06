# Country Value Factor Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Country Value Factor strategy ranks equity markets by the Shiller CAPE ratio. Countries with the lowest CAPE are considered cheap and are bought, while expensive markets are avoided. The approach exploits the tendency for undervalued markets to outperform over time.

Every month the strategy redistributes capital equally among the cheapest countries from a user supplied universe. Positions are sized by portfolio value and only executed when the trade exceeds a minimum USD amount.

## Details

- **Universe**: Collection of country equity ETFs.
- **Signal**: Buy the countries with the lowest CAPE ratios.
- **Rebalance**: First trading day of each month.
- **Positioning**: Long only.
- **Parameters**:
  - `Universe` – securities representing each country.
  - `MinTradeUsd` – minimum dollar amount per order.
  - `CandleType` – time frame of candles (default: 1 day).
- **Note**: The sample code contains placeholder logic and should be extended with real factor calculations.
