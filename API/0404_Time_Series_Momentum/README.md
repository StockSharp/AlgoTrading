# Time Series Momentum
[Русский](README_ru.md) | [中文](README_cn.md)

This approach goes long or short each asset based on its own past returns. If the trailing return is positive the model buys; if negative it sells, forming a diversified trend‑following portfolio.

Signals are evaluated monthly using one‑year lookbacks and positions are equally weighted across assets.

## Details

- **Data**: Monthly total returns for each asset.
- **Entry**: Long when 12‑month return > 0; short when < 0.
- **Exit**: Reverse when the signal changes sign.
- **Instruments**: Broad set of futures or ETFs.
- **Risk**: Volatility scaling and diversification.

