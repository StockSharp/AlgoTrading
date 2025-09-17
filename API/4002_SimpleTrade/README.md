# SimpleTrade Strategy

## Overview
- StockSharp port of the MetaTrader 4 expert advisor **SimpleTrade.mq4** (a.k.a. "neroTrade").
- Designed for single-symbol trading on the timeframe configured through the `CandleType` parameter.
- Always maintains at most one open position and flips direction at the open of every new bar.

## Trading Logic
1. Each time a new candle becomes active, the strategy compares the candle's opening price with the opening price of the candle that is `LookbackBars` periods older.
2. If the new open is strictly higher than the historical reference, all existing positions are closed and a fresh long market order with `TradeVolume` lots is submitted.
3. Otherwise (open is equal or lower) the strategy closes any existing positions and opens a short market position of the same size.
4. The `StopLossPoints` parameter mirrors the original EA's `stop` setting. When both the security's `PriceStep` and `StopLossPoints` are available, the strategy converts the value into an absolute distance and forwards it to `StartProtection`, letting StockSharp maintain the protective stop-loss orders automatically.
5. Candle opens are tracked using the high-level candle subscription API. Finished candles populate the history list, while the active candle triggers the decision once per bar.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `TradeVolume` | Base order size expressed in lots. Must be positive. | `1` |
| `StopLossPoints` | Protective stop distance in instrument points. Set to `0` to disable the automatic stop-loss. | `120` |
| `LookbackBars` | Number of bars used for the open-price comparison. A value of `3` reproduces `Open[0]` vs `Open[3]` from the original code. | `3` |
| `CandleType` | Timeframe (as a `DataType`) from which candles are requested. Controls when new signals appear. | `1 hour timeframe` |

## Implementation Notes
- Uses the high-level `SubscribeCandles(...).Bind(...)` workflow, so the strategy remains lightweight and reacts to both historical and live candles.
- `StartProtection` is invoked once during `OnStarted`. Ensure the connected security provides `PriceStep`; otherwise the stop-loss distance cannot be translated into absolute prices.
- Because all trades are entered with market orders at the start of each bar, slippage handling is delegated to the trading venue and there is no additional `slippage` parameter.
- The historical open buffer keeps only a small rolling window (`LookbackBars + 5` values) to avoid unnecessary memory usage.
- No Python port is supplied; the `CS/` directory contains the only implementation.

## File Structure
```
4002_SimpleTrade/
├── CS/
│   └── SimpleTradeStrategy.cs
├── README.md
├── README_cn.md
└── README_ru.md
```
