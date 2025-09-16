# Color JJRSX Trend Strategy

## Overview
This strategy reimagines the MetaTrader expert advisor `Exp_ColorJJRSX` inside the StockSharp high-level framework. The original system relies on the proprietary ColorJJRSX oscillator, which combines Jurik smoothing techniques to detect trend changes. In this port the oscillator is approximated with a standard Relative Strength Index (RSI) that is further smoothed by a Jurik Moving Average (JMA). The slope of the smoothed oscillator is then evaluated over several historical bars to trigger entries and exits.

Trading takes place on a configurable candle timeframe (4-hour candles by default) and supports independent toggles for long and short operations. Additional parameters allow you to keep the exit logic identical to the source expert advisor while introducing StockSharp-native risk controls such as point-based stop loss and take profit.

## Indicator construction
1. **RSI approximation** – A `RelativeStrengthIndex` with the period defined by `JurxPeriod` replaces the original JurX smoothing stage. This keeps the oscillator bounded between 0 and 100 while capturing relative momentum.
2. **Jurik smoothing** – The RSI output is passed through a `JurikMovingAverage` (length `JmaPeriod`). The resulting series is a smooth curve that reacts quickly to momentum changes without excessive lag.
3. **Historical window** – The strategy stores the most recent `SignalBar + 3` JMA values to replicate `CopyBuffer` usage from MQL. Values indexed by `SignalBar`, `SignalBar + 1`, and `SignalBar + 2` correspond to the bars used in the source expert for signal evaluation.

## Trading logic
- **Bullish setup**
  - `JMA[SignalBar + 1] < JMA[SignalBar + 2]` confirms that the oscillator turned upward on the preceding bar.
  - `JMA[SignalBar] > JMA[SignalBar + 1]` shows that upward momentum continues on the latest closed bar.
  - If long entries are enabled and no long position is active, the strategy buys `OrderVolume` units. Existing short exposure is reversed automatically.
- **Bearish setup**
  - `JMA[SignalBar + 1] > JMA[SignalBar + 2]` confirms a downward turn.
  - `JMA[SignalBar] < JMA[SignalBar + 1]` validates continued downward momentum.
  - If short entries are enabled, the strategy sells `OrderVolume` units and flips any existing long exposure.
- **Exit rules**
  - When the smoothed oscillator slope turns against the position (`AllowBuyClose` / `AllowSellClose`), the open trade is closed at market.
  - Protective stop loss and take profit levels (expressed in price points) are recalculated on every new position. If the candle range touches a level the position is closed immediately.

## Risk management
- `StopLossPoints` converts to price distance with the instrument price step and protects against adverse moves.
- `TakeProfitPoints` defines the symmetrical target distance.
- Stops and targets are disabled automatically when set to zero.
- Volume can be fine-tuned independently of the base strategy volume through `OrderVolume`.

## Parameters
| Name | Description |
| --- | --- |
| `JurxPeriod` | Period of the RSI approximation used before Jurik smoothing. Mirrors the JurX period from the MQL expert. |
| `JmaPeriod` | Length of the Jurik Moving Average applied to the RSI output. |
| `SignalBar` | Index of the historical bar used for evaluation (1 = previous closed bar). Larger values delay signal confirmation. |
| `EnableBuy` / `EnableSell` | Toggle long or short entries independently. |
| `AllowBuyClose` / `AllowSellClose` | Enable slope-based exit signals for long and short positions respectively. |
| `OrderVolume` | Volume traded on each new entry. Existing opposite exposure is added to the new order to perform a full reversal. |
| `TakeProfitPoints` / `StopLossPoints` | Profit target and stop distance in instrument points. Set to zero to disable. |
| `CandleType` | Candle timeframe used for indicator calculations (defaults to 4-hour candles). |

## Differences from the original expert advisor
- JurX smoothing is approximated by a classic RSI because the proprietary JurX algorithm is not available in StockSharp. Parameter names remain consistent to simplify migration.
- MetaTrader slippage (`Deviation_`) and money-management enumerations are not reproduced. Instead a fixed `OrderVolume` parameter is provided; you can combine it with StockSharp position sizing modules if required.
- Orders are executed with `BuyMarket`/`SellMarket`, while stop loss and take profit are emulated via price checks on the finished candle.

## Usage tips
1. Attach the strategy to the desired security and set the `CandleType` to match the timeframe you want to replicate.
2. Adjust `JurxPeriod` and `JmaPeriod` to fit the market’s responsiveness. Higher values create smoother oscillations and fewer signals.
3. Fine-tune `SignalBar` if you need additional confirmation lag compared to the default one-bar delay.
4. Configure `OrderVolume`, `StopLossPoints`, and `TakeProfitPoints` according to your risk appetite. Use zero to disable automatic exits.
5. Combine with StockSharp’s built-in logging or charting helpers (already wired for candle + indicator plots) to monitor oscillator behavior in real time.

The strategy is ready for both discretionary experimentation and automated backtesting within the StockSharp environment while staying faithful to the intent of the original ColorJJRSX system.
