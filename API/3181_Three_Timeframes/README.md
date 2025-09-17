# Three Timeframes Strategy

## Overview
The **Three Timeframes Strategy** replicates the MetaTrader expert `Three timeframes.mq5` using StockSharp's high level API. The system combines momentum and trend filters taken from different timeframes:

- **MACD (M5)** detects recent momentum reversals on the trading timeframe.
- **Alligator (H4)** verifies that the higher timeframe structure is aligned with the intended trade direction.
- **RSI (H1)** confirms that momentum on the middle timeframe supports the breakout.
- Optional **session filtering** blocks trades outside the configured working hours.

The strategy uses pip-based risk management. Initial stop-loss and take-profit levels are attached to every new position. When the price advances, an optional trailing stop tightens the protective stop after the market covers both the trailing distance and the trailing step.

## Signal Logic
1. Prices are processed on three different subscriptions: trading candles, higher-timeframe candles for the Alligator, and intermediate candles for the RSI.
2. A long setup requires:
   - MACD main line crossing **below** the signal line on the previous bar while the bar before that was above the signal line, reproducing the MetaTrader "blue crosses red downward" rule.
   - RSI on the H1 feed above 50.
   - Alligator jaw > teeth > lips on the previous completed H4 candle, signalling an upward structure.
3. A short setup mirrors the rules: the MACD main line crosses above the signal line, RSI is below 50, and lips > teeth > jaw on the Alligator to confirm a downward structure.
4. If an opposite position exists, the strategy closes it by sending a market order for the net size, just like the original EA before opening a new trade.
5. After entry the strategy applies initial stop-loss/take-profit distances and continues to trail the stop once the price moves by `TrailingStopPips + TrailingStepPips` from the entry.

The trading session filter mirrors the MetaTrader implementation. When the start hour is less than the end hour, trading is allowed only inside the interval. When the start hour is greater than the end hour, the active window wraps across midnight.

## Risk Management
- **Stop Loss / Take Profit** – distances are expressed in pips. The strategy converts them into price units using the symbol's price step and adjusts for 3- or 5-digit FX quotes.
- **Trailing Stop** – activates once the trade covers both the trailing stop and the trailing step distance. The stop is then moved to `price - trailing distance` for longs and `price + trailing distance` for shorts.
- **Trade Volume** – specifies the base lot size for new market orders. Opposite exposure is flattened automatically before reversing.

## Differences from MetaTrader Version
- StockSharp's asynchronous order model removes the need for explicit transaction tracking flags (`m_waiting_transaction`). Orders are executed using `BuyMarket`/`SellMarket`, which already wait for confirmations internally.
- Slippage, filling policy, and margin mode settings from the MQL version are abstracted by StockSharp. These platform-specific controls are not required for the .NET implementation.
- The Alligator indicator is rebuilt from smoothed moving averages while preserving the original periods and shifts. Indicator values are stored in sliding buffers to reproduce the offset behaviour of MetaTrader's built-in Alligator.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `TradeVolume` | Market order size in lots/contracts. | `1` |
| `StopLossPips` | Initial stop-loss distance in pips. | `50` |
| `TakeProfitPips` | Initial take-profit distance in pips. | `140` |
| `TrailingStopPips` | Trailing stop distance in pips. | `5` |
| `TrailingStepPips` | Extra pip move required before moving the trailing stop. | `5` |
| `MacdFastPeriod` | Fast EMA length for MACD. | `13` |
| `MacdSlowPeriod` | Slow EMA length for MACD. | `26` |
| `MacdSignalPeriod` | Signal smoothing period for MACD. | `10` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Alligator SMMA periods for jaw/teeth/lips. | `13`, `8`, `5` |
| `JawShift`, `TeethShift`, `LipsShift` | Forward shifts for the Alligator lines. | `8`, `5`, `3` |
| `RsiPeriod` | RSI averaging length on the intermediate timeframe. | `14` |
| `CandleType` | Trading timeframe (default 5-minute candles). | `M5` |
| `AlligatorCandleType` | Higher timeframe for Alligator calculation (default 4-hour candles). | `H4` |
| `RsiCandleType` | Intermediate timeframe for RSI confirmation (default 1-hour candles). | `H1` |
| `UseTimeFilter` | Enables the session filter. | `true` |
| `StartHour` | Session start hour (inclusive). | `10` |
| `EndHour` | Session end hour (exclusive). | `15` |

## Usage Notes
- Ensure the selected security provides the three configured candle streams (M5, H1, H4 by default). StockSharp will automatically request all required subscriptions via `GetWorkingSecurities()`.
- The pip conversion relies on `Security.PriceStep`. Instruments with unusual tick sizes may need manual adjustment of stop/take parameters.
- Trailing stops require both `TrailingStopPips` and `TrailingStepPips` to be greater than zero. Setting either parameter to zero disables the trailing logic, consistent with the MQL expert.
