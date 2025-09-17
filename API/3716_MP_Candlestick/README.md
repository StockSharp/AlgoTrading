# MP Candlestick Strategy

## Overview
The **MP Candlestick Strategy** is a conversion of the MetaTrader 5 Expert Advisor `mp candlestick.mq5` into the StockSharp high-level strategy framework. The system evaluates the direction of completed candles and opens trades in the same direction while applying strict risk management. It supports both fixed stop-loss distances expressed in MetaTrader pips and adaptive stop-loss placement derived from the Average True Range (ATR).

## Trading Logic
1. The strategy subscribes to a single configurable candle series (default: 1-hour candles).
2. On each finished candle:
   - Bullish candle (close above open) → consider a long position.
   - Bearish candle (close below open) → consider a short position.
   - Doji candles are ignored.
3. Before any entry the strategy calculates a stop-loss price either from ATR or from the fixed pip distance. The take-profit price is computed using the configured risk-to-reward ratio.
4. If margin usage stays within the allowed percentage and the calculated position size is valid, the trade is opened at market.
5. While the position is active the strategy monitors each new candle for:
   - Stop-loss or take-profit hits using candle extremes.
   - Trailing adjustment that moves the stop toward breakeven when ATR stops are enabled.
6. Once the position is flat the process restarts with the next finished candle.

## Risk and Money Management
- **Risk Percent** defines the equity fraction risked per trade. The position size is derived from the price distance between entry and stop-loss and the instrument price/step value.
- **Risk/Reward Ratio** determines the distance between the entry price and take-profit target relative to the initial risk.
- **Max Margin Usage** restricts how much estimated margin the new trade may consume compared to current portfolio equity.
- **Trailing Stop** is activated automatically when ATR-based risk management is used. It moves the stop halfway toward the profit target without exceeding the latest candle close, attempting to lock profits while respecting exchange constraints.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `RiskPercent` | 1 | Percent of portfolio equity allocated as maximum loss for a single trade. |
| `RiskRewardRatio` | 1.5 | Multiplier applied to the initial risk distance to define the take-profit target. |
| `MaxMarginUsage` | 30 | Upper bound for margin consumption expressed as a percentage of equity. |
| `StopLossPips` | 50 | Fixed stop-loss size in MetaTrader pips when ATR is disabled. |
| `UseAutoSl` | true | Enables ATR (length 14) stop-loss sizing with multiplier 1.5. |
| `CandleType` | 1-hour time frame | Candle series used for signals and ATR calculation. |

## Implementation Notes
- The strategy relies on StockSharp high-level subscriptions (`SubscribeCandles`) and indicator binding (`AverageTrueRange`).
- Position sizing aligns with the instrument volume step, minimum and maximum volume constraints.
- Margin checks reuse available instrument margin hints (`MarginBuy`/`MarginSell`) and fall back to a price-based estimate.
- Stop-loss and take-profit levels are enforced internally by monitoring candle highs and lows, ensuring consistent behavior across brokers.
- All code comments are in English as required by the conversion guidelines.

## Files
- `CS/MpCandlestickStrategy.cs` — main C# strategy implementation.
- `README.md` — English documentation (this file).
- `README_cn.md` — Chinese translation.
- `README_ru.md` — Russian translation.
