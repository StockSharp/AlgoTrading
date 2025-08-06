# Volatility Risk Premium
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy sells options to harvest the volatility risk premium, expecting implied volatility to exceed realized volatility on average. Positions are delta‑hedged to isolate the premium.

Short option exposure is managed with strict risk controls and periodic re‑hedging.

## Details

- **Data**: Implied volatility from options and realized volatility.
- **Entry**: Sell out‑of‑the‑money options when implied > realized.
- **Exit**: Buy back at expiration or when volatility spikes.
- **Instruments**: Index or FX options.
- **Risk**: Delta hedging and stop‑loss on vega.

