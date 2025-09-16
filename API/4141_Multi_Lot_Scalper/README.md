# Multi Lot Scalper Strategy

## Overview

The **Multi Lot Scalper Strategy** is a martingale-style averaging system converted from the classic MetaTrader expert advisor "Multi Lot Scalper". The original algorithm was designed for major FX pairs and relied on the slope of the MACD histogram to decide whether the market is entering a bullish or bearish phase. Once a direction is identified, the strategy opens a ladder of market orders, progressively increasing the volume after each adverse move. The StockSharp port keeps the original entry logic, money management rules, and protective mechanisms while leveraging the high-level candle subscription API.

The strategy works best on liquid instruments where spreads are tight and a pip definition is stable. By default it subscribes to 15-minute candles, but any other timeframe compatible with the instruments can be supplied through the `CandleType` parameter.

## Trading Logic

1. **Signal detection** – A MACD indicator (`MacdFastLength`, `MacdSlowLength`, `MacdSignalLength`) is evaluated on every finished candle. When the MACD main line rises relative to the previous value the strategy looks for long opportunities, otherwise it prepares to short. The `ReverseSignals` parameter flips this interpretation for users who prefer contrarian entries.
2. **Initial entry** – The first position in a new sequence is opened immediately after a valid signal as long as the date/time filter (`StartYear`, `StartMonth`, `EndYear`, `EndMonth`, `EndHour`, `EndMinute`) allows trading. Market orders are used, mirroring the MetaTrader implementation.
3. **Pyramiding** – Subsequent orders are triggered only if price moves against the latest fill by at least `EntryDistancePips`. Each additional trade multiplies the base volume either by 2 or by 1.5 (when `MaxTrades` is above 12) to reproduce the EA's martingale sizing.
4. **Stops and targets** – `InitialStopPips` and `TakeProfitPips` convert to price levels for the entire basket. A trailing stop activates after the move in favour exceeds `EntryDistancePips + TrailingStopPips`, tightening the exit as the market accelerates.
5. **Account protection** – When the basket is near its capacity (`MaxTrades - OrdersToProtect`) and the floating profit reaches `SecureProfit`, the strategy closes the most recent trade and temporarily blocks new entries if `UseAccountProtection` is enabled.

## Money Management

The original expert advisor optionally recalculated the base lot size as a function of the account balance. The StockSharp port keeps this behaviour through the `UseMoneyManagement`, `RiskPercent`, and `IsStandardAccount` parameters. When the feature is active, the base lot (`LotSize`) is ignored and instead derived from the portfolio value, scaled for mini or standard accounts just like the MQL code.

## Parameters

| Parameter | Description | Default |
| --- | --- | --- |
| `TakeProfitPips` | Take-profit distance applied to each entry, expressed in pips. | `40` |
| `LotSize` | Base lot size used when money management is disabled. | `0.1` |
| `InitialStopPips` | Initial stop-loss distance in pips. | `0` |
| `TrailingStopPips` | Trailing stop distance that activates after the threshold. | `20` |
| `MaxTrades` | Maximum number of martingale entries permitted simultaneously. | `10` |
| `EntryDistancePips` | Minimum adverse movement before adding a new order. | `15` |
| `SecureProfit` | Floating profit (in currency) required to trigger account protection. | `10` |
| `UseAccountProtection` | Enables closing the last trade when the secure profit threshold is met. | `true` |
| `OrdersToProtect` | Number of final trades affected by the secure profit rule. | `3` |
| `ReverseSignals` | Reverses the MACD interpretation (bullish becomes short, bearish becomes long). | `false` |
| `UseMoneyManagement` | Enables account-balance-based lot calculation. | `false` |
| `RiskPercent` | Risk percentage used when money management is active. | `12` |
| `IsStandardAccount` | Uses standard-lot scaling instead of mini-lot scaling. | `false` |
| `EurUsdPipValue` | Pip value override for EURUSD. | `10` |
| `GbpUsdPipValue` | Pip value override for GBPUSD. | `10` |
| `UsdChfPipValue` | Pip value override for USDCHF. | `10` |
| `UsdJpyPipValue` | Pip value override for USDJPY. | `9.715` |
| `DefaultPipValue` | Fallback pip value used for other instruments. | `5` |
| `StartYear` | First calendar year when new positions may be opened. | `2005` |
| `StartMonth` | First month allowed for new entries. | `1` |
| `EndYear` | Last calendar year for initiating trades. | `2006` |
| `EndMonth` | Last calendar month for initiating trades. | `12` |
| `EndHour` | Hour (24h) after which fresh entries are blocked. | `22` |
| `EndMinute` | Minute component of the daily cut-off time. | `30` |
| `CandleType` | Candle type used for signal generation (default is 15-minute). | `15-minute time frame` |
| `MacdFastLength` | Fast EMA length of the MACD indicator. | `14` |
| `MacdSlowLength` | Slow EMA length of the MACD indicator. | `26` |
| `MacdSignalLength` | Signal EMA length of the MACD indicator. | `9` |

## Usage Guidelines

- Ensure that the instrument's pip step matches the pip value configuration. Update the pip value parameters when applying the strategy to CFDs, metals, or crypto assets.
- The martingale scaling can grow exposure rapidly. Start with conservative `MaxTrades`, `EntryDistancePips`, and `TrailingStopPips` values before experimenting with larger baskets.
- Optimise the MACD settings and candle interval for the instrument being traded. Slower charts usually reduce the number of averaging steps, while faster charts increase activity.
- The account-protection rule is particularly important on markets prone to sudden reversals. If the secured profit is hit often, consider reducing `SecureProfit` or tightening `TrailingStopPips`.
- The trading window filter allows the strategy to be disabled after a chosen intraday time. This is useful for avoiding news releases or late-session volatility.

## Conversion Notes

- The StockSharp version uses the high-level candle subscription API (`SubscribeCandles().BindEx(...)`) instead of manual tick processing, keeping indicator management transparent.
- Trailing stops are handled internally by managing the aggregate stop level for the basket rather than modifying each child order individually, which mirrors the intended behaviour in a portfolio-aware environment.
- The EA's use of `AccountBalance` for position sizing is mapped to the `Portfolio.CurrentValue` property, maintaining parity between MetaTrader and StockSharp implementations.
