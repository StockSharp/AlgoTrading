# Asset Class Momentum Rotational
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This rotational model allocates capital to the asset classes exhibiting the strongest recent momentum. Each period the system ranks asset ETFs and holds the leaders while avoiding laggards.

Rebalancing occurs monthly with cash as a defensive asset when no momentum is positive.

## Details

- **Data**: Monthly total returns of asset class ETFs.
- **Entry**: Hold top N assets with positive momentum.
- **Exit**: Replace assets when they fall out of the top ranking.
- **Instruments**: Broad asset class ETFs.
- **Risk**: Uses cash proxy and position caps.

