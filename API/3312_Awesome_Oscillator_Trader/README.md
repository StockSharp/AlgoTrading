# Awesome Oscillator Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Awesome Oscillator Trader strategy is a direct conversion of the MetaTrader "AwesomeOscTrader" expert advisor. It combines Bill Williams' Awesome Oscillator with Bollinger Band width and Stochastic oscillator filters to time breakouts after deep momentum contractions. The system is designed for single-symbol, hourly trading on highly liquid FX pairs such as EURUSD, mirroring the original recommendation.

The strategy waits for the Bollinger Band spread to enter a configurable range, signalling that volatility has contracted but not disappeared. During that squeeze the Awesome Oscillator histogram must print a distinctive five-bar reversal pattern: four consecutive down-histogram bars that remain below zero, followed by a new bar that flips to the upward colour while still negative. When this structure forms and the Stochastic oscillator crosses back above an oversold level, the strategy opens a long position expecting the squeeze to resolve upward. The inverse pattern — four positive up-histogram bars above zero and a new down-coloured bar while still positive — combined with the Stochastic falling below an upper threshold, triggers a short entry.

Positions are protected with an ATR-based stop distance. Every bar the system reads the 3-period Average True Range, multiplies it by a configurable factor, and converts the result to pips based on the instrument's tick size. That value defines both the initial stop-loss and the take-profit targets, reproducing the symmetric exit logic of the MetaTrader version. An optional trailing stop tightens the protective level once price moves favourably by the configured number of pips, while the `CloseOnReversal` switch closes positions whenever the opposite Awesome Oscillator pattern or colour change appears. A profit filter allows closing only winning, only losing, or all trades on reversal signals, replicating the EA's "ProfitTypeClTrd" behaviour.

## Trading rules

- **Timeframe:** default 1-hour candles (fully configurable).
- **Filters:**
  - Bollinger Band width must be between `BollingerSpreadLower` and `BollingerSpreadUpper` pips.
  - Stochastic %K is compared against `StochasticLowerLevel` for longs and `StochasticUpperLevel` for shorts.
  - Awesome Oscillator must build the five-bar reversal structure with the most recent bar changing colour while remaining on the opposite side of zero, and its normalized magnitude must exceed `AoStrengthLimit`.
- **Entries:**
  - **Long:** conditions above plus the current bar is within the allowed trading hours window.
  - **Short:** mirrored conditions.
- **Exits:**
  - ATR-derived stop-loss and take-profit levels set symmetrically at entry.
  - Trailing stop (if `TrailingStopPips` &gt; 0) ratchets in the direction of profit.
  - Optional closure on opposite signal or oscillator colour change depending on `CloseOnReversal` and `ProfitFilter`.

## Key parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 1-hour | Timeframe used for all indicators. |
| `BollingerPeriod` | 20 | Period of the Bollinger Bands volatility filter. |
| `BollingerSigma` | 2.0 | Standard deviation multiplier for the Bollinger Bands. |
| `BollingerSpreadLower` | 24 pips | Minimum band spread required to trade. |
| `BollingerSpreadUpper` | 230 pips | Maximum band spread permitted. |
| `AoFastPeriod` / `AoSlowPeriod` | 4 / 28 | Fast and slow periods of the Awesome Oscillator. |
| `AoStrengthLimit` | 0.0 | Minimum normalized AO magnitude to confirm entries. |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | 1 / 4 / 1 | Stochastic oscillator lengths reproducing the MetaTrader defaults. |
| `StochasticLowerLevel` / `StochasticUpperLevel` | 12 / 21 | Oversold and overbought thresholds for confirming signals. |
| `EntryHour` / `OpenHours` | 16 / 13 | Start hour and duration of the trading window. Handles wrap-around at midnight just like the EA. |
| `RiskPercent` | 0.5% | Risk percentage used for position sizing when account data is available. |
| `AtrMultiplier` | 4.5 | Multiplier applied to the 3-period ATR to compute stop distance. |
| `TrailingStopPips` | 40 pips | Distance for the optional trailing stop (set to 0 to disable). |
| `ProfitFilter` | OnlyProfitable | Selects whether reversal exits can close any, only profitable, or only losing trades. |
| `MaxOpenOrders` | 1 | Maximum number of simultaneous positions (kept at 1 to match the EA). |

## Implementation notes

- Uses StockSharp `BollingerBands`, `StochasticOscillator`, `AwesomeOscillator`, `AverageTrueRange`, and `Highest` indicators; there are no manual indicator calculations.
- AO values are normalized over the last 100 bars to mimic the MetaTrader indicator buffers and reproduce the colour logic without custom code.
- Position sizing respects `Security.StepVolume`, `Security.MinVolume`, `Security.MaxVolume`, and `Security.StepPrice` when available, falling back to the strategy's default volume otherwise.
- Protective levels are managed entirely inside the strategy: stop and take-profit checks execute on every finished candle, matching the EA's tick-level management while avoiding the need for broker-side orders.
- All comments within the code are in English, and indentation uses tabs as required by the project guidelines.
