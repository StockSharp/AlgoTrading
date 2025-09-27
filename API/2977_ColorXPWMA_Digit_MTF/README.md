# ColorXPWMA Digit Multi-Timeframe Strategy

## Overview
This strategy converts the MetaTrader 5 expert advisor **Exp_ColorXPWMA_Digit_NN3_MMRec** into the StockSharp high level API. The original robot operates three independent modules that trade on different timeframes by analysing the digital colouring of the ColorXPWMA moving average. The StockSharp port keeps the same behaviour: every module watches its own candle series, closes positions when the indicator flips colour and optionally opens a new trade in the detected direction.

The default configuration follows the MT5 template:

| Module | Timeframe | Stop Loss (points) | Take Profit (points) |
| ------ | --------- | ------------------ | -------------------- |
| A | 8 hours | 3000 | 10000 |
| B | 4 hours | 2000 | 6000 |
| C | 1 hour | 1000 | 3000 |

Each module can be enabled or disabled for long and short entries or exits through dedicated Boolean parameters. The implementation keeps individual position tracking per module so that simultaneous long and short trades can coexist without interfering with the volume accounting of the other timeframes.

## ColorXPWMA Digit indicator
The ColorXPWMA Digit indicator emulates the MT5 custom indicator. For every finished candle the algorithm:

1. Builds a power-weighted average of the selected applied price (`Period` and `Power`).
2. Smooths the value with the chosen moving average (`SmoothMethods` and `SmoothLength`).
3. Rounds the result to the configured number of decimals (`Digit`).
4. Assigns a colour code: **2** when the smoothed value increases, **0** when it decreases, otherwise the previous colour is reused.

`SignalBar` controls which historical bar is inspected. Value `0` uses the most recent closed candle, value `1` the previous candle, etc. A buy opportunity appears when the monitored bar turns to colour `2` after being different on the prior bar. A sell opportunity is generated when the colour becomes `0` after being different on the previous bar.

Smoothing methods are mapped to StockSharp indicators as follows:

- `Sma`, `Ema`, `Smma`, `Lwma`, `Jjma` → corresponding StockSharp moving averages.
- `T3` → internal Tillson T3 implementation.
- `Vidya` → internal VIDYA implementation driven by Chande Momentum Oscillator.
- `Ama` → Kaufman Adaptive Moving Average.
- Unsupported options (`JurX`, `Parabolic`) fall back to the simple moving average, matching the behaviour of the original template when exotic smoothers are not available.

## Trade management and money management
For every module the strategy keeps two independent virtual positions (long and short). When a module receives a closing signal the strategy sends a market order equal to the remaining volume of that virtual position. Opening orders are ignored while an opposite position is still open.

Position sizing copies the MT5 money-management helper:

- `NormalMM` defines the base volume.
- `SmallMM` replaces the base volume when recent trades recorded at least `LossTrigger` losses inside the last `TotalTrigger` trades for that direction.

The logic is evaluated separately for long and short sequences. Trade outcomes are calculated from the average filled price when a module fully closes its virtual position.

Risk management mirrors the MT5 stops in price points:

- When a long position is open and candle lows cross `entry - StopLoss * PriceStep`, the long is closed immediately.
- When candle highs touch `entry + TakeProfit * PriceStep`, profits are taken.
- The rules are mirrored for shorts (`entry + StopLoss` for protection, `entry - TakeProfit` for targets).

## Parameters
All parameters are exposed through `StrategyParam<T>` objects and can be optimised from the StockSharp designer. They are grouped per module (A, B, C). The following table lists the settings for any module **X**:

| Parameter | Description |
| --------- | ----------- |
| `X_CandleType` | Candle series to subscribe (default timeframes shown above). |
| `X_Period`, `X_Power` | Power weighted window used to build the base XPWMA value. |
| `X_SmoothMethod`, `X_SmoothLength`, `X_SmoothPhase` | Smoother applied to the weighted price. `SmoothPhase` is kept for compatibility with MT5 JJMA users. |
| `X_AppliedPrice` | Price source (close, open, high, low, median, typical, weighted, simple, quarter, TrendFollow, DeMark). |
| `X_Digit` | Rounding precision applied to the smoothed value. |
| `X_SignalBar` | Historical bar used for signal evaluation. |
| `X_BuyMagic`, `X_SellMagic` | Preserved for traceability (used inside order comments). |
| `X_BuyTotalTrigger`, `X_BuyLossTrigger` | Long-side money management thresholds. |
| `X_SellTotalTrigger`, `X_SellLossTrigger` | Short-side money management thresholds. |
| `X_SmallMM`, `X_NormalMM` | Volumes used by the money management rule. |
| `X_MarginMode`, `X_Deviation` | Reserved fields kept for feature parity; they do not alter the StockSharp orders. |
| `X_StopLoss`, `X_TakeProfit` | Distances in price steps applied to the module virtual position. |
| `X_BuyOpen`, `X_SellOpen`, `X_SellClose`, `X_BuyClose` | Permission switches for module actions. |

## Notes
- Each market order is annotated with `A|BuyOpen`, `B|SellClose`, etc. so that fills can be traced back to their module.
- The strategy operates exclusively on finished candles and therefore reproduces the MT5 `IsNewBar` protection automatically provided by the high-level API.
- If multiple modules trigger on the same bar, their volumes are processed sequentially using the per-module virtual position buffers.
