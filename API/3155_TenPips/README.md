# TenPips Strategy

The **TenPips Strategy** is a StockSharp port of the MetaTrader "10PIPS" expert advisor. It combines fast/slow linear weighted moving averages calculated on the trading timeframe with a multi-timeframe momentum confirmation and a macro (monthly) MACD filter. The conversion mirrors the original money-management module, including break-even protection, pip-based trailing, and equity/absolute profit targets.

## Signal Logic

1. **Primary timeframe** (parameter `CandleType`, default 15 minutes) supplies the price stream used for the fast and slow LWMAs computed on the typical price `(H + L + C) / 3`.
2. **Higher timeframe momentum** (`MomentumCandleType`, default 1 hour) converts the StockSharp momentum difference into the MetaTrader ratio. The absolute distance from `100` over the last three completed bars must exceed `MomentumThreshold` for a trade to arm.
3. **Macro MACD filter** (`MacdCandleType`, default 30-day candles approximating MetaTrader's monthly period) requires the MACD main line to be above the signal line for buys and below for sells.

A long position opens when the previous candle:
- closed above the fast LWMA after dipping below it,
- the fast LWMA is above the slow LWMA,
- any of the last three momentum readings meets the `MomentumThreshold`,
- the macro MACD is bullish.

A short position uses the symmetric conditions (previous close below the fast LWMA, fast below slow, momentum above the threshold, MACD bearish).

Because StockSharp operates with a net position model, the port opens at most one aggregate position per side. Sending a buy while short automatically closes the short portion and leaves the requested long volume.

## Risk and Money Management

- **Protective distances** – `StopLossPips` and `TakeProfitPips` translate MetaTrader pips into price offsets using the security `PriceStep`. When either boundary is hit, the strategy closes the entire position with a market order.
- **Trailing stop** – `TrailingStopPips` follows the highest (long) or lowest (short) price since entry.
- **Break-even** – when enabled, `BreakEvenTriggerPips` arms the stop and shifts it to the entry plus the optional `BreakEvenOffsetPips`.
- **Money targets** – the trio `UseMoneyTakeProfit`, `UsePercentTakeProfit`, and `EnableMoneyTrailing` replicate the EA's `TP_In_Money`, `TP_In_Percent`, and the balance-based trailing lock. Unrealized PnL is measured per candle close.
- **Equity stop** – `UseEquityStop` with `EquityRiskPercent` implements the original `UseEquityStop` / `TotalEquityRisk` guard by closing positions once the drawdown from the equity peak exceeds the threshold.
- **MACD exit flag** – `UseMacdExit` mirrors the EA's `Exit` switch, closing positions early when the macro MACD flips against the trade.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `TradeVolume` | `0.01` | Net position volume used for market orders (MetaTrader lot size equivalent). |
| `CandleType` | `15m` time frame | Primary timeframe for the fast/slow LWMAs and trade execution. |
| `MomentumCandleType` | `1h` time frame | Higher timeframe candles feeding the momentum confirmation. |
| `MacdCandleType` | `30d` time frame | Macro timeframe (monthly approximation) for MACD confirmation. |
| `FastMaPeriod` | `8` | Period of the fast linear weighted moving average. |
| `SlowMaPeriod` | `50` | Period of the slow linear weighted moving average. |
| `MomentumPeriod` | `14` | Lookback for the momentum ratio. |
| `MomentumThreshold` | `0.3` | Minimum absolute distance from `100` (MetaTrader momentum) required over the last three higher timeframe bars. |
| `StopLossPips` | `20` | Protective stop-loss in MetaTrader pips. Set to zero to disable. |
| `TakeProfitPips` | `50` | Protective take-profit in MetaTrader pips. Set to zero to disable. |
| `TrailingStopPips` | `40` | Trailing stop distance in pips (zero disables trailing). |
| `UseBreakEven` | `true` | Enables move-to-break-even behaviour. |
| `BreakEvenTriggerPips` | `30` | Profit (pips) required before break-even activates. |
| `BreakEvenOffsetPips` | `30` | Extra pips added to the break-even stop once activated. |
| `UseMoneyTakeProfit` | `false` | Close positions after reaching the absolute profit target `MoneyTakeProfit`. |
| `MoneyTakeProfit` | `10` | Profit target expressed in account currency. |
| `UsePercentTakeProfit` | `false` | Close positions after earning `PercentTakeProfit` percent of the initial equity. |
| `PercentTakeProfit` | `10` | Percentage target based on starting equity. |
| `EnableMoneyTrailing` | `true` | Enable balance-based trailing stop using `MoneyTrailTarget` / `MoneyTrailStop`. |
| `MoneyTrailTarget` | `40` | Profit (currency) required before the money trail is armed. |
| `MoneyTrailStop` | `10` | Allowed give-back after arming the money trail. |
| `UseEquityStop` | `true` | Enable equity drawdown protection. |
| `EquityRiskPercent` | `1` | Maximum drawdown from the equity peak before forcing a flat position. |
| `UseMacdExit` | `false` | Close positions on an opposite MACD signal from the macro timeframe. |

## Implementation Notes

- Pip conversion follows the EA logic: if the broker tick size is `0.00001` or `0.001`, a pip equals ten ticks; otherwise the raw `PriceStep` is used.
- StockSharp's momentum indicator outputs a price difference. The strategy converts it to the MetaTrader ratio `(Close / Close(period) * 100)` before applying `MomentumThreshold`.
- The port operates in a netting environment and therefore does not replicate the EA's multi-ticket martingale (`IncreaseFactor`, `LotExponent`, `Max_Trades`). Instead, it adjusts order volume automatically when flipping between long and short positions.
- Protective exits and profit management submit market orders, matching the original advisor's behaviour when modifying open tickets.
- Charts display the processed indicators (fast LWMA, slow LWMA, momentum, MACD) when visualization is available.

## Usage

1. Configure the candle timeframes to match the MetaTrader chart and higher timeframe used by the EA.
2. Adjust the pip-based risk parameters to the instrument point size. Zero disables the corresponding component.
3. Enable or disable money/percent targets, equity stop, and MACD exit according to your risk preferences.
4. Launch the strategy; it will subscribe to the three required timeframes, manage positions according to the original rules, and log any protective exits triggered by the balance-based or equity protections.
