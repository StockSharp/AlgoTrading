# Synthetic Lending Rates
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy exploits differences between synthetic lending rates derived from derivative markets and on‑chain lending yields. By borrowing where rates are low and lending where rates are high, it captures the spread between them.

Positions are rebalanced regularly to maintain neutrality, and risk is controlled through rate change thresholds and liquidity filters.

## Details

- **Data**: Perpetual swap funding and DeFi lending rates.
- **Entry**: Borrow in low‑rate venue and lend in high‑rate venue when spread > threshold.
- **Exit**: Close when spread mean reverts or liquidity deteriorates.
- **Instruments**: Perpetual swaps and DeFi platforms.
- **Risk**: Spread cap and liquidity stop.

