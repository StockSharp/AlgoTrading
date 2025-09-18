# Suffic369 Strategy

## Overview
The Suffic369 strategy is a trend-following breakout system that combines two short moving averages with wide Bollinger Bands. The expert advisor enters long positions when the fast simple moving average (SMA) of closing prices crosses above the SMA of recent highs while the market trades near the lower Bollinger Band. Short positions are opened when the fast SMA crosses below the SMA of recent lows while the price is pressing against the upper band. The converted StockSharp version keeps the original MQL logic but expresses it with high-level candle subscriptions and indicator bindings.

## Indicators
- **Fast SMA (Close, length = 3)** – measures short-term direction of the closing price.
- **High SMA (High, length = 5)** – averages recent highs and acts as a bullish resistance reference.
- **Low SMA (Low, length = 5)** – averages recent lows and provides the bearish support reference.
- **Bollinger Bands (length = 156, deviation = 1)** – identifies price extremes relative to volatility.

All indicators are updated on completed candles. Previous values are cached to reproduce the one-bar shift used in the original MetaTrader program.

## Trading Rules
### Long Entry
1. Previous fast SMA (close) is below the previous high SMA.
2. Current fast SMA (close) crosses above the current high SMA.
3. Candle closing price is below the lower Bollinger Band.

### Short Entry
1. Previous fast SMA (close) is above the previous low SMA.
2. Current fast SMA (close) crosses below the current low SMA.
3. Candle closing price is above the upper Bollinger Band.

### Exit Logic
- **Opposite Signal:** A long position is closed when a fresh short entry signal appears, and vice versa.
- **Stop-Loss:** Optional price-step based stop that protects the position once activated.
- **Take-Profit:** Optional price-step based target mirroring the original TakeProfit parameter.
- **Trailing Stop:** Optional trailing stop that tightens behind profitable trades exactly like the MQL logic (uses current close to move the stop only when profit exceeds the configured distance).

The strategy holds at most one position at a time. After a stop, target, or opposite signal closes the trade, no new entry is evaluated until the next finished candle.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `FastMaLength` | 3 | Length of the fast SMA built on close prices. |
| `HighMaLength` | 5 | Length of the SMA calculated on candle highs. |
| `LowMaLength` | 5 | Length of the SMA calculated on candle lows. |
| `BollingerLength` | 156 | Window size of the Bollinger Bands. |
| `BollingerDeviation` | 1 | Standard deviation multiplier for the bands. |
| `UseStopLoss` | true | Enables the stop-loss block. |
| `StopLossPoints` | 30 | Stop distance in instrument price steps. |
| `UseTakeProfit` | true | Enables the take-profit block. |
| `TakeProfitPoints` | 60 | Profit target distance in price steps. |
| `UseTrailingStop` | true | Enables trailing stop management. |
| `TrailingStopPoints` | 30 | Trailing offset in price steps. |
| `CandleType` | 15-minute time frame | Candle type used for calculations. |

All numeric parameters are exposed as `StrategyParam<T>` instances so they can be optimised directly inside StockSharp.

## Risk Management
- Stop-loss, take-profit, and trailing stops use the instrument price step (`Security.PriceStep`) to convert point distances into absolute prices.
- Trailing stops follow profitable moves only when the price has advanced more than the configured distance, replicating the original order-modification logic.
- `StartProtection()` is invoked on start to enable StockSharp’s built-in protective features.

## Usage Notes
- Subscribe the strategy to an instrument that supports the selected candle type.
- Ensure the `Volume` property is set to the desired trade size before starting the strategy.
- The strategy waits for fully formed indicator values before issuing any orders; initial candles are used to seed indicator history.
