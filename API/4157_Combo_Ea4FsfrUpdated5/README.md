# Combo EA4 FSF R Updated 5 Strategy

## Overview
This strategy is a StockSharp conversion of the MetaTrader expert advisor "Combo_EA4FSFrUpdated5". It combines five different technical modules—moving averages, RSI, stochastic oscillator, parabolic SAR and a zero-lag MACD—to validate every trading decision. A position is opened only when **all** enabled modules point to the same direction, recreating the strict consensus logic of the original EA. Optional trailing management, automatic signal-based exits and the ability to flip into the opposite direction after closing are also preserved.

## Indicator stack
- **Moving averages** – Three configurable averages (MA1, MA2, MA3) with ATR-based buffers that reduce false crossover signals. Five different aggregation modes replicate the EA's "MA_MODE" options.
- **Relative Strength Index (RSI)** – Multiple confirmation modes including classic overbought/oversold, slope-based trend detection, a combined mode and zone-based validation.
- **Stochastic oscillator** – Fast/slow/slowdown lengths with optional high/low band filtering.
- **Parabolic SAR** – Provides a trend polarity check against the previous candle close.
- **Zero-lag MACD** – Uses zero-lag exponential moving averages to match the bundled `ZeroLag_MACD.mq4` indicator. Supports three signal modes (trend structure, zero-line cross or combined).
- **Average True Range (ATR)** – Drives stop-loss/take-profit distances and the MA crossover buffers.

## Trading logic
### Entry conditions
1. The indicator values for all enabled modules must be available (the strategy automatically waits for warm-up).
2. For each enabled module a bullish or bearish direction is computed according to its mode:
   - **Moving averages** – MA1/MA2/MA3 combinations with ATR buffers to confirm direction changes.
   - **RSI** – Four modes covering thresholds, momentum and zone logic.
   - **Stochastic** – K/D cross confirmation with optional high/low filters.
   - **Parabolic SAR** – Requires price to be above/below the SAR value of the previous candle.
   - **Zero-lag MACD** – Either trend alignment, zero-line cross confirmation or both.
3. If **every** enabled module returns `Buy`, the strategy sends a market buy order. If every module returns `Sell`, a market sell order is issued. Otherwise no trade is opened.

### Exit conditions
- **Signal-based exits** – When `AutoClose` is enabled the same consensus logic is evaluated using the dedicated exit flags (`UseMaClosing`, `UseMacdClosing`, etc.). A long position is closed when all enabled exit modules agree on a bearish signal; a short position is closed when they agree on a bullish signal. If `OpenOppositeAfterClose` is true, the opposite position is queued immediately after the closing fill.
- **Protective levels** – Initial stop-loss and take-profit levels are derived from the current ATR value (`AtrPeriod`) multiplied by `AtrMultiplier`. The EA's pip buffer is emulated with the instrument's step size. Long trades use `ATR × multiplier − buffer` for stops and `ATR × multiplier + buffer` for targets (mirrored for shorts).
- **Trailing stop** – When `UseTrailingStop` is enabled, the stop price is adjusted on every finished candle using the configured point distance (`TrailingStop`).
- **Hard exits** – If price reaches the stop-loss or take-profit intrabar, the position is closed immediately and no opposite entry is triggered.

### Position sizing
- **Static mode** – When `UseStaticVolume` is true, trades are placed with the fixed `StaticVolume` parameter.
- **Dynamic mode** – Otherwise the strategy derives an approximate size from the portfolio's current value and `RiskPercent`, falling back to the base `Volume` if portfolio or price data are unavailable.

## Parameters
| Group | Parameter | Description |
|-------|-----------|-------------|
| Entries | `UseMa` | Enable moving average confirmation. |
| Entries | `MaMode` | Selects the MA combination (fast/medium, medium/slow, combined, etc.). |
| Indicators | `Ma1Period`, `Ma2Period`, `Ma3Period` | Periods of the three moving averages. |
| Indicators | `Ma1BufferPeriod`, `Ma2BufferPeriod` | ATR periods used as buffer for MA cross checks. |
| Indicators | `Ma1Method`, `Ma2Method`, `Ma3Method` | Moving average calculation types (SMA, EMA, SMMA, LWMA). |
| Indicators | `Ma1Price`, `Ma2Price`, `Ma3Price` | Applied price for each moving average. |
| Entries | `UseRsi` | Enable RSI confirmation. |
| Indicators | `RsiPeriod` | RSI calculation period. |
| Entries | `RsiMode` | RSI confirmation mode (overbought/oversold, trend, combined, zone). |
| Entries | `RsiBuyLevel`, `RsiSellLevel` | Thresholds for oversold/overbought logic. |
| Entries | `RsiBuyZone`, `RsiSellZone` | Zone thresholds for mode 4. |
| Entries | `UseStochastic` | Enable stochastic confirmation. |
| Indicators | `StochasticK`, `StochasticD`, `StochasticSlowing` | K/D/slow parameters. |
| Entries | `UseStochasticHighLow` | Require stochastic to break configured high/low bands. |
| Entries | `StochasticHigh`, `StochasticLow` | Upper and lower stochastic thresholds. |
| Entries | `UseSar` | Enable parabolic SAR confirmation. |
| Indicators | `SarStep`, `SarMax` | SAR acceleration settings. |
| Entries | `UseMacd` | Enable zero-lag MACD confirmation. |
| Indicators | `MacdFast`, `MacdSlow`, `MacdSignal` | MACD parameters. |
| Indicators | `MacdPrice` | Applied price for MACD. |
| Entries | `MacdMode` | MACD confirmation mode. |
| Risk | `UseTrailingStop`, `TrailingStop` | Trailing stop toggle and distance (in points). |
| Risk | `UseStaticVolume`, `StaticVolume`, `RiskPercent` | Position sizing controls. |
| Risk | `AtrPeriod`, `AtrMultiplier` | ATR settings for risk management. |
| Exits | `AutoClose` | Enable exit consensus logic. |
| Exits | `OpenOppositeAfterClose` | Flip into the opposite direction after a signal-based exit. |
| Exits | `UseMaClosing`, `MaModeClosing` | Moving average exit configuration. |
| Exits | `UseMacdClosing`, `MacdModeClosing` | MACD exit configuration. |
| Exits | `UseRsiClosing`, `RsiModeClosing` | RSI exit configuration. |
| Exits | `UseStochasticClosing` | Stochastic exit toggle. |
| Exits | `UseSarClosing` | SAR exit toggle. |
| General | `CandleType` | Primary timeframe (default 5-minute candles). |

## Notes
- The strategy operates one net position at a time (long, short or flat), mirroring MetaTrader's "maximum same orders" restriction with a simpler StockSharp-friendly approach.
- Pending opposite entries are queued only for signal-based exits and are skipped if a stop-loss or take-profit closes the trade.
- Because account margin requirements are broker-specific, the dynamic position sizing uses an approximate risk-based formula; verify the resulting volume before live deployment.
- Ensure that the zero-lag MACD and ATR indicators have sufficient warm-up history before expecting trades, just as in the original EA.
