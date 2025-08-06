# Soccer Clubs Arbitrage
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy seeks arbitrage opportunities between soccer club fan tokens traded on multiple venues. By watching price spreads and funding rate imbalances, it opens offsetting long and short positions to lock in mispricings.

A trade triggers when the spread between exchanges exceeds a threshold. Positions are hedged and exited when prices converge or a protective stop is reached.

## Details

- **Data**: Fan token prices and funding rates.
- **Entry**: Open opposite positions when spread > X%.
- **Exit**: Close when spread < Y% or at time stop.
- **Instruments**: Exchange‑listed fan tokens.
- **Risk**: Fixed‑percent stop to guard against slippage.

