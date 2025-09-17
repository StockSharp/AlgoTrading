# Stochastic Momentum Filter Strategy

## Overview
The **Stochastic Momentum Filter Strategy** is a StockSharp port of the MetaTrader expert advisor `Stochastic.mq4` (folder `MQL/23473`). The original robot combines two stochastic oscillators, linear weighted moving averages (LWMA), a momentum deviation filter, and a higher timeframe MACD trend check. This C# version recreates the same building blocks on top of the StockSharp high-level API and keeps the multi-layered confirmation workflow:

1. **Trend filter** – a fast LWMA must be above (or below) a slow LWMA before long (or short) trades are allowed.
2. **Oscillator confirmation** – both a fast stochastic (5/2/2) and a slow stochastic (21/4/10) must agree on oversold/overbought zones.
3. **Momentum deviation** – at least one of the three most recent momentum readings must deviate from the 100 baseline by more than a configurable threshold, matching the expert's use of the MT4 `iMomentum` function.
4. **Higher timeframe MACD** – the MACD main line on a configurable higher timeframe must stay above the signal line for longs (and below for shorts). The default 30-day timeframe approximates the original monthly filter.
5. **Risk logic** – stop loss, take profit, and optional trailing are handled through `StartProtection`, mirroring the EA's protective parameters. Position flips close opposing exposure automatically before establishing the new net position.

The strategy subscribes to two candle streams: the trading timeframe and the higher timeframe that feeds the MACD filter. All calculations are performed with StockSharp indicators and processed through the high-level `Bind` helpers.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `StochasticBuyLevel` | `30` | Oversold level that both stochastic oscillators must breach for long setups. |
| `StochasticSellLevel` | `80` | Overbought level that both stochastic oscillators must reach for short setups. |
| `FastMaPeriod` | `6` | Length of the fast LWMA trend filter. |
| `SlowMaPeriod` | `85` | Length of the slow LWMA trend filter. |
| `FastStochasticPeriod` | `5` | `%K` period of the fast stochastic oscillator. |
| `FastStochasticSignal` | `2` | `%D` smoothing period of the fast stochastic. |
| `FastStochasticSmoothing` | `2` | Extra smoothing applied to the fast stochastic (matches MT4 “slowing”). |
| `SlowStochasticPeriod` | `21` | `%K` period of the slow stochastic oscillator. |
| `SlowStochasticSignal` | `4` | `%D` smoothing period of the slow stochastic. |
| `SlowStochasticSmoothing` | `10` | Extra smoothing applied to the slow stochastic. |
| `MomentumPeriod` | `14` | Look-back of the momentum oscillator (same as MT4 `iMomentum`). |
| `MomentumThreshold` | `0.3` | Minimum absolute deviation from the 100 baseline required within the last three momentum values. |
| `MacdFastPeriod` | `12` | Fast EMA period for the higher timeframe MACD. |
| `MacdSlowPeriod` | `26` | Slow EMA period for the higher timeframe MACD. |
| `MacdSignalPeriod` | `9` | Signal EMA period for the higher timeframe MACD. |
| `TakeProfitPoints` | `50` | Take-profit distance (in price points). Set to `0` to disable. |
| `StopLossPoints` | `20` | Stop-loss distance (in price points). Set to `0` to disable. |
| `EnableTrailing` | `true` | Enables StockSharp trailing of the protective stop. |
| `TradeVolume` | `1` | Net position size targeted on each signal. |
| `MaxNetPositions` | `1` | Caps the stacked net exposure (multiplies `TradeVolume`). |
| `CandleType` | `15m` time frame | Main trading timeframe. |
| `HigherTimeframe` | `30d` time frame | Timeframe used for MACD confirmation. |

## Trading Logic
1. **Indicator preparation** – the strategy binds both LWMAs, both stochastic oscillators, the momentum indicator, and the MACD to their respective candle streams.
2. **Momentum history** – the absolute distance of the momentum oscillator from 100 is stored for the last three finished bars. This replicates the EA's `MomLevelB/MomLevelS` arrays.
3. **Entry rules**
   - **Long**: fast LWMA above slow LWMA, both stochastic `%K` and `%D` values below `StochasticBuyLevel`, momentum deviation above `MomentumThreshold`, and MACD main line above the signal line.
   - **Short**: fast LWMA below slow LWMA, both stochastic `%K` and `%D` values above `StochasticSellLevel`, momentum deviation above the threshold, and MACD main line below the signal line.
4. **Position handling** – orders are sent with `BuyMarket`/`SellMarket`. When a reversal signal appears the strategy automatically closes any opposite net exposure before establishing the new direction.
5. **Protection** – `StartProtection` applies the configured take-profit and stop-loss distances (in points). When `EnableTrailing` is true, StockSharp manages stop trailing similarly to the EA's trailing routine.

## Differences Compared to the MQL Version
- **Volume scaling**: the EA scales lot sizes using `LotExponent` and allows multiple simultaneous tickets. The StockSharp port focuses on net exposure and targets a single `TradeVolume` per direction (bounded by `MaxNetPositions`).
- **Margin management**: margin checks, equity stops, and notification functions from the original script are not reproduced because they rely on MT4 account APIs.
- **Freeze levels**: low-level broker-specific freeze-level checks are omitted; StockSharp order routing handles exchange constraints.
- **Break-even toggle**: the MT4 "move to breakeven" helper is replaced by StockSharp's trailing protection.

## Usage Notes
1. Assign a security and connector, then start the strategy. It will automatically subscribe to both the trading timeframe and the higher timeframe required by the MACD filter.
2. If your data source does not support a 30-day candle type, adjust `HigherTimeframe` to a supported interval (e.g., weekly or daily). The trend confirmation logic still expects the MACD main line to stay on the same side of its signal line.
3. Set `TradeVolume` to match your portfolio units. The strategy sets `Volume` during `OnStarted`, so Designer/Runner will use this size when submitting orders.
4. Set `TakeProfitPoints`/`StopLossPoints` to zero if protective orders should be disabled.
5. All comments inside the code are written in English, and indentation uses tabs, following repository guidelines.

## Files
- `CS/StochasticMomentumFilterStrategy.cs` – StockSharp implementation of the strategy logic.
- `README.md` – English documentation (this file).
- `README_ru.md` – Russian documentation.
- `README_cn.md` – Chinese documentation.
