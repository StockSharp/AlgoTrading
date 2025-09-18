# Cryptocurrency Divergence Strategy

## Overview
The **Cryptocurrency Divergence Strategy** searches for classical momentum divergences between price action and the Relative Strength Index (RSI) while confirming trend direction with moving averages and MACD. The original MetaTrader expert advisor relied on multi-timeframe momentum checks, money management, and extensive trailing logic. This StockSharp port keeps the spirit of the system by:

- Detecting bullish divergences when price prints a lower low but RSI forms a higher low.
- Detecting bearish divergences when price creates a higher high but RSI prints a lower high.
- Validating setups with fast/slow moving averages and MACD line versus signal line.
- Managing positions through configurable stop loss, take profit, break-even, and trailing stop behaviour expressed in price steps.

The strategy is designed for spot cryptocurrencies but can be applied to any instrument that delivers enough volatility and clear swing points.

## Indicators
- **Simple Moving Average (SMA)**: A fast and a slow SMA provide the primary trend filter.
- **Relative Strength Index (RSI)**: Supplies the momentum pivot values used to measure divergence strength.
- **Moving Average Convergence Divergence (MACD)**: Confirms that momentum agrees with the detected divergence direction.

All indicators are bound through the high-level API, so no manual buffering is required.

## Trading Logic
1. Subscribe to the configured candle type and compute SMA, RSI, and MACD values on each finished bar.
2. Track the most recent swing highs and lows alongside their RSI values. Only monotonic extensions (new higher highs or lower lows) update the swing data.
3. A **bullish divergence** appears when a fresh lower low in price is paired with a higher low in RSI. The trade also requires the fast SMA to be above the slow SMA, the MACD line to exceed the signal line, and the RSI value to remain below the neutral level (default 45) to ensure oversold conditions.
4. A **bearish divergence** requires a new higher high in price with a lower RSI high, the fast SMA below the slow SMA, MACD line below its signal, and RSI above the neutral bearish level (default 55).
5. The strategy opens only one net position at a time. Reversals close the existing position and immediately enter in the opposite direction when signals align.

## Risk Management
- **Volume**: User-defined trade size applied to all market orders.
- **Stop Loss / Take Profit**: Expressed in price steps and attached after every fill using the actual execution price.
- **Break-Even Move**: Optionally replaces the stop loss with an offset above/below the entry once price travels a configurable distance.
- **Trailing Stop**: Optionally ratchets behind the close price at a fixed distance measured in steps. The trailing stop takes priority over the original stop loss after activation.

Stops and targets are evaluated on every finished candle, ensuring deterministic behaviour that matches backtests with real-time execution.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Candle series used for analysis (default 15-minute time frame). |
| `TradeVolume` | Order volume applied to all entries. |
| `FastMaLength` / `SlowMaLength` | Periods of the fast and slow SMAs. |
| `RsiLength` | RSI calculation length. |
| `RsiBullishLevel` / `RsiBearishLevel` | RSI thresholds that define oversold and overbought zones for divergence confirmation. |
| `MacdShortLength` / `MacdLongLength` / `MacdSignalLength` | MACD configuration. |
| `StopLossPoints` / `TakeProfitPoints` | Distances in price steps for risk and reward targets. |
| `EnableBreakEven`, `BreakEvenTrigger`, `BreakEvenOffset` | Controls for the break-even move. |
| `EnableTrailing`, `TrailDistance` | Trailing stop activation and spacing. |

Every parameter is exposed through `StrategyParam<T>` so it can be optimised inside the StockSharp designer.

## Usage Notes
1. Attach the strategy to a cryptocurrency symbol and ensure the instrument has a defined `PriceStep` and `Board`. Without a price step the strategy cannot calculate stops.
2. Align the candle type with the market you trade (e.g., 15m, 1h). Divergence detection is sensitive to timeframe.
3. Adjust stop and target distances to the instrument’s volatility. Crypto pairs with five decimal places often require larger step counts.
4. Enable break-even or trailing behaviour only after observing sufficient profit cushion in historical tests; aggressive trailing may exit trades prematurely.
5. Monitor the strategy in the StockSharp designer or market data panel to visualise indicator alignment and executed trades.

## Differences from the MQL Version
- Money-based trailing and equity stop protections are simplified into price-step-based stop management.
- Multi-timeframe momentum checks are replaced by single-timeframe MACD confirmation for clarity.
- Email/notification side effects are omitted because they are handled externally in StockSharp ecosystems.

Despite these adjustments, the core divergence detection and protective logic remain faithful to the intent of the original expert advisor.
