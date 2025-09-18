# Parabolic SAR Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp port of the MetaTrader "PSAR Trader EA" expert advisor. It observes how price interacts with the Parabolic SAR indicator and reacts only when the dot field flips from one side of the candle body to the other. The conversion preserves the original money-management logic: the strategy can either trade a fixed lot size or dynamically adjust the order volume based on account balance, applies fixed stop-loss and take-profit levels, and activates a trailing stop once a trade accumulates sufficient profit.

## Strategy logic
- Build a Parabolic SAR indicator with user-defined acceleration and maximum values on the selected candle series (30-minute candles by default).
- Detect a **bullish flip** when the SAR dot moves from above the candle body to below it. If no position is open, submit a market buy order. If a short position exists, close it first and wait for the next signal to re-enter long.
- Detect a **bearish flip** when the SAR dot moves from below the candle body to above it. If flat, open a short position. If a long position is active, close it and defer the entry until the following signal.
- Monitor open trades on every finished candle and execute exits whenever any protective level (stop-loss, take-profit, or trailing stop) is reached by the current candle’s high/low.

## Risk management
- **Stop loss** – expressed in points (price steps). For long trades the stop is placed below the entry price; for shorts it is placed above.
- **Take profit** – also expressed in points. The target mirrors the stop in the opposite direction and closes the entire position when reached.
- **Trailing stop** – starts after price moves by a configurable number of points in favor of the trade. The trailing stop tightens only in the direction of profit, replicating the “tighten stops only” behavior of the original EA.

## Volume management
- **Fixed lot** – when auto-lot is disabled, the strategy submits orders with the configured fixed lot size.
- **Balance-based lot** – when auto-lot is enabled, volume is calculated as `(Account Balance / 1000) * LotsPerThousand` and aligned to the security’s volume step and minimum volume.

## Parameters and defaults
- `SarStep` – Parabolic SAR acceleration factor. Default: `0.02`.
- `SarMaximum` – Parabolic SAR maximum acceleration. Default: `0.2`.
- `CandleType` – timeframe for the analysis. Default: 30-minute candles.
- `UseAutoLot` – enable dynamic lot sizing. Default: `false`.
- `FixedLot` – volume used when auto lot sizing is off. Default: `0.1`.
- `LotsPerThousand` – multiplier for auto-lot calculations. Default: `0.05`.
- `StopLossPoints` – distance to the stop in points. Default: `500`.
- `TakeProfitPoints` – distance to the take profit in points. Default: `1000`.
- `TrailingStartPoints` – profit threshold that enables trailing. Default: `500`.
- `TrailingDistancePoints` – trailing offset once enabled. Default: `100`.

## Notes
- The strategy trades both long and short directions but keeps at most one position open at a time.
- Protective orders are simulated on candle data; intra-candle spikes smaller than the selected timeframe may influence fill quality during live trading.
