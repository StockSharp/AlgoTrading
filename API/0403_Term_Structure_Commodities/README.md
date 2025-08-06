# Term Structure Commodities
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy trades the slope of commodity futures curves. It buys contracts in backwardation and sells those in contango, betting on mean reversion in the term structure.

Each month the system ranks futures by carry, going long the strongest backwardation and short the steepest contango. Positions roll prior to expiry.

## Details

- **Data**: Front and deferred futures prices.
- **Entry**: Long top carry commodities, short bottom carry.
- **Exit**: Roll on contract expiration or if carry flips sign.
- **Instruments**: Commodity futures.
- **Risk**: Equal dollar weighting with stop on adverse carry change.

