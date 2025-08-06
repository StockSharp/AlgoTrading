# Currency Momentum Factor Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This factor strategy ranks currencies by medium‑term momentum and builds a long/short portfolio. Currencies with the strongest performance over the lookback window are bought, while the weakest are shorted in equal sizes.

Momentum is evaluated using daily candles and the book is rebalanced on the first trading day of each month. Orders smaller than a minimum USD value are ignored to reduce noise.

## Details

- **Universe**: List of currency pairs or ETFs.
- **Signal**: Go long the top `K` currencies by momentum and short the bottom `K`.
- **Lookback**: Return computed over `Lookback` daily candles (default 252).
- **Rebalance**: Monthly.
- **Positioning**: Long/short, dollar‑neutral.
- **Parameters**:
  - `Universe` – tradable currency symbols.
  - `Lookback` – number of candles for momentum.
  - `K` – count of assets to long and short.
  - `MinTradeUsd` – minimum trade size.
  - `CandleType` – candle timeframe (default: 1 day).
- **Note**: The sample lacks real momentum calculation for demonstration purposes.
