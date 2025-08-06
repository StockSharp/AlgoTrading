# Currency PPP Value Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Currency PPP Value strategy looks for mispricing relative to purchasing power parity (PPP). Currencies that trade below their PPP value are bought, while those trading above their PPP value are shorted. The portfolio is rebalanced monthly to maintain the long/short exposure.

Because PPP data updates infrequently, trades are only placed when the required adjustment exceeds a minimum USD amount. The sample code provides the framework for ranking currencies but leaves the actual PPP calculation as a placeholder.

## Details

- **Universe**: Set of currency pairs with available PPP estimates.
- **Signal**: Long the `K` most undervalued currencies and short the `K` most overvalued.
- **Rebalance**: Monthly.
- **Positioning**: Long/short, equal weight.
- **Parameters**:
  - `Universe` – tradable currencies.
  - `K` – number of currencies to long and short.
  - `MinTradeUsd` – minimum trade size in USD.
  - `CandleType` – candle timeframe (default: 1 day).
- **Note**: PPP deviation retrieval (`TryGetPPPDeviation`) is not implemented and must be supplied by the user.
