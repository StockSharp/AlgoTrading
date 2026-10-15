# Keltner Seasonal Filter
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
The **Keltner Seasonal Filter** strategy is built around that trades based on Keltner Channel breakouts with seasonal bias filter.

Testing indicates an average annual return of about 94%. It performs best in the stocks market.

Signals trigger when Keltner confirms filtered entries on intraday (5m) data. This makes the method suitable for active traders.

Stops rely on ATR multiples and factors like EmaPeriod, AtrPeriod. Adjust these defaults to balance risk and reward.

## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2m`
  - `SeasonalThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Keltner, Seasonal
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

## Exact trading rules (C# and Python)

Trading decisions are evaluated only for **finished** candles after the indicators have formed and trading is permitted. Define `C` as the candle close, `E = EMA(Close, EmaPeriod)`, `A = ATR(AtrPeriod)`, `M = AtrMultiplier`, `U = E + M × A`, and `L = E − M × A`. All comparisons below are strict unless shown otherwise.

The seasonal score `S` is selected from the **historical candle's opening month** (`candle.OpenTime.Month`), not the month of `OnStarted2` or the machine clock. This example uses fixed demonstration scores rather than calculating seasonality from history:

| Month | Jan | Feb | Mar | Apr | May | Jun | Jul | Aug | Sep | Oct | Nov | Dec |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `S` | 0.8 | 0.3 | 0.6 | 0.7 | 0.2 | −0.3 | −0.1 | −0.4 | −0.8 | 0.1 | 0.9 | 0.7 |

- **Long entry / short reversal:** `S > SeasonalThreshold` **and** `C > U` **and** `Position <= 0`. Submit a market buy of `Volume + abs(Position)`, then set the manual stop level to `C − M × A`.
- **Short entry / long reversal:** `S < −SeasonalThreshold` **and** `C < L` **and** `Position >= 0`. Submit a market sell of `Volume + abs(Position)`, then set the stop level to `C + M × A`.
- **Stop first:** with a positive stored stop level, close a long if `C <= stop`, or close a short if `C >= stop`. Reset the stop and do not process another signal for that candle.
- **EMA exit:** otherwise, close a long if `C < E`, or a short if `C > E`. Reset the stop. No separate take-profit or automatic trailing stop exists.

The stop is **manual and checked at finished-candle close**; it is not an exchange-native intrabar stop. Entry stop levels are based on the signal candle's close rather than the eventual execution price. For the default threshold `0.5`, January, March, April, November and December permit long signals, September permits short signals, and all other months forbid new entries; a permitted season still needs its corresponding channel breakout.
