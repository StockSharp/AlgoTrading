# CCI MACD Scalper

## Overview
The CCI MACD Scalper ports the MetaTrader 5 expert advisor "CCI + MACD Scalper" to the StockSharp high-level strategy API. The conversion keeps the original indicator stack—an EMA trend filter, a CCI zero-line trigger, and a MACD divergence check—while translating the money-management logic into StockSharp conventions. Orders size themselves from portfolio equity, stops are rejected when the distance is too tight, and an optional trailing stop can secure profits by partially closing positions after the first adjustment. A five-candle cooldown prevents the strategy from re-entering immediately after an execution, replicating the MQL timer behaviour.

## Strategy logic
### Indicators and data processing
* **Candles** – one configurable timeframe drives every calculation. Signals are evaluated exclusively on completed candles to avoid repainting.
* **EMA(34)** – closing price exponential moving average acts as directional filter. Longs require the latest close to sit above the previous EMA value, shorts require a close below it.
* **CCI(50)** – used as a momentum trigger. The strategy waits for a zero-line cross that occurred on the two most recent finished candles (the current candle confirms the setup but does not participate in the logical comparison).
* **MACD(12,26,9)** – the MACD main and signal lines must both stay on the same side of zero for the previous two candles. Entry requires the MACD signal line to cross the main line in favour of the position between those two bars (bullish crossover for longs, bearish crossover for shorts).
* **Swing buffers** – the last five finished candle highs and lows form the stop-loss reference. Longs anchor to the lowest low, shorts to the highest high, exactly matching the MetaTrader `iLowest/iHighest` calls with a shift of one bar.

### Entry rules
* **Session control** – trading is allowed only when the candle close time falls within `[MinHour, MaxHour]` in local terminal time.
* **Cooldown** – after each filled entry the system waits for five candle durations before allowing a new trade, mirroring `EventSetTimer` from the original code.
* **Long setup**
  * No active long position (`Position <= 0`).
  * Close price above the previous EMA value.
  * CCI crossed from negative to positive on the two most recent closed candles.
  * MACD crossover occurred below zero during the same two bars (signal rose above MACD).
  * Stop loss positioned at the most recent swing low satisfies the minimal distance constraint.
* **Short setup**
  * No active short position (`Position >= 0`).
  * Close price below the previous EMA value.
  * CCI crossed from positive to negative across the last two completed candles.
  * MACD crossover occurred above zero (signal fell below MACD).
  * Stop loss at the swing high respects the minimal distance requirement.

### Risk and trade management
* **Dynamic position sizing** – trade size is derived from the configured `RiskPercent` of the portfolio equity. The risk per contract is computed from the stop-loss distance, security price step and step value. The result is snapped to the instrument's volume step and clamped between the minimum and maximum volume.
* **Stop loss / take profit** – stop loss uses the chosen swing extreme and is rejected when the distance is below `MinimalStopLossPoints`. Take profit equals `entry ± RiskReward × stopDistance`, matching the EA's reward-to-risk calculation.
* **Trailing stop (optional)** – when enabled, the stop moves by `TrailingStopPoints` once the price closes far enough beyond the previous stop. The first trailing adjustment triggers a partial exit that closes half of the original volume, faithfully mirroring the MetaTrader implementation.
* **Protective exits** – for longs the position closes if price pierces the stop level (candle low) or reaches the take-profit level (candle high). Shorts mirror the logic using candle highs and lows respectively.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `CandleType` | Timeframe driving the indicator calculations. | 15-minute candles |
| `RiskPercent` | Percentage of portfolio equity risked on each trade. | 2% |
| `RiskReward` | Reward-to-risk multiplier for the take-profit level. | 1.5 |
| `EmaPeriod` | Length of the EMA trend filter. | 34 |
| `CciPeriod` | Length of the Commodity Channel Index. | 50 |
| `MinHour` | Earliest hour (inclusive) when new trades may be opened. | 0 |
| `MaxHour` | Latest hour (inclusive) when new trades may be opened. | 24 |
| `MinimalStopLossPoints` | Minimal allowed distance between entry and stop loss expressed in price points. | 100 |
| `UseTrailingStop` | Enables the trailing stop module and partial take profit. | Disabled |
| `TrailingStopPoints` | Trailing stop distance measured in price points. | 100 |

## Additional notes
* The price-point conversion relies on the security's `PriceStep`. Symbols without a valid step fall back to a distance of one price unit.
* Portfolio equity is obtained from `Portfolio.CurrentValue` and falls back to `BeginValue` when the current valuation is not available. If both are missing, the strategy reverts to the base `Volume` property.
* There is no Python port for this strategy; only the C# version is included in the API package.
