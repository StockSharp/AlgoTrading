# Alexav D1 Profit GBPUSD Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Daily breakout system for GBPUSD that combines an EMA calculated on highs, RSI filters, MACD momentum confirmation and ATR-based risk management. The script reproduces the four-layer profit taking and breakeven behaviour of the original MetaTrader version.

## Key Facts

- **Market**: GBP/USD spot or CFD
- **Timeframe**: Daily candles (configurable)
- **Direction**: Long and short
- **Position Style**: Multi-target scaling with shared stop loss
- **Instruments Used**: EMA (High), RSI, MACD main line, ATR

## Indicator Setup

1. **EMA on High prices** – default length 6, approximates the dynamic breakout level.
2. **RSI** – default length 10, defines overbought/oversold corridors used as momentum filters.
3. **MACD main line** – fast 5, slow 21, signal 14. Only the main line is used to measure momentum slope.
4. **ATR** – length 28, delivers volatility-dependent stops and targets.

## Entry Logic

### Long Entries

1. The previous daily bar opens below the EMA (High) and closes above it (cross-over confirmation).
2. RSI stays between **60** and **80** – prevents trades during weak momentum and avoids overextended rallies.
3. MACD main line satisfies one of two momentum checks:
   - The value two bars ago is negative (indicating the trend recently turned positive), **or**
   - The relative reduction in absolute MACD between the last two bars exceeds the configurable **MacdDiffBuy** threshold (default 0.5).

If all conditions hold, four equal market buy orders are placed (default 0.1 lots each). Any existing short exposure is flattened before the new batch is sent.

### Short Entries

1. The bar opens above the EMA (High) and closes below it.
2. RSI is between **25** and **39** – mirrors the long side thresholds.
3. MACD two bars ago is positive **or** the relative change in absolute MACD between the last two bars is above **MacdDiffSell** (default 0.15).

On confirmation, the strategy flattens existing longs, then submits four equal market sells.

## Trade Management

- **Initial Stop**: Shared ATR stop calculated from the entry close. Longs use `entry - ATR * StopLossMultiplier` (default 1.6). Shorts use `entry + ATR * StopLossMultiplier`.
- **Profit Targets**: Four incremental ATR-based levels per direction: `1.0`, `1.5`, `2.0`, and `2.5` ATR multiples scaled by the `TakeProfitMultiplier` parameter (default 1). Each level closes one quarter of the original position via a market order when price trades through the level.
- **Breakeven Behaviour**: After each partial exit the protective stop for the remaining position is moved to the most recent target price. This mimics the original EA which modifies stop losses to the filled take-profit price whenever a TP deal occurs.
- **Stop Handling**: If price touches the protective level intrabar (using candle high/low), the remaining position is closed immediately at market.

## Risk Control Notes

- The strategy does not pyramid beyond the four-entry batch. A new signal is ignored while exposure remains in the same direction.
- ATR must be positive; signals are skipped if the volatility indicator has not formed yet.
- Parameter changes at runtime affect future orders only; per-order volume is captured at entry for correct scaling on exits.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `OrderVolume` | Volume per individual market order in the batch | `0.1` |
| `EmaPeriod` | EMA length applied to candle highs | `6` |
| `RsiPeriod` | RSI averaging period | `10` |
| `AtrPeriod` | ATR averaging period | `28` |
| `StopLossMultiplier` | ATR multiple for the protective stop | `1.6` |
| `TakeProfitMultiplier` | Base ATR multiple for profit targets | `1.0` |
| `MacdFastPeriod` | MACD fast EMA length | `5` |
| `MacdSlowPeriod` | MACD slow EMA length | `21` |
| `MacdSignalPeriod` | MACD signal EMA length | `14` |
| `MacdDiffBuyThreshold` | Minimum MACD slope improvement for long trades | `0.5` |
| `MacdDiffSellThreshold` | Minimum MACD slope improvement for short trades | `0.15` |
| `RsiUpperLimit` | Maximum RSI allowed before a long entry | `80` |
| `RsiUpperLevel` | Minimum RSI required for a long entry | `60` |
| `RsiLowerLevel` | Maximum RSI allowed for a short entry | `39` |
| `RsiLowerLimit` | Minimum RSI required before shorts | `25` |
| `CandleType` | Timeframe used for candle subscription | `1 Day` |

## Deployment Tips

- Optimise RSI and MACD thresholds together; loosening RSI corridors without adjusting the MACD acceleration filters can create whipsaws.
- Because partial exits rely on candle extremes, accurate data for high/low values is important for realistic backtests.
- Always run with sufficient capital to handle four simultaneous orders per signal.
