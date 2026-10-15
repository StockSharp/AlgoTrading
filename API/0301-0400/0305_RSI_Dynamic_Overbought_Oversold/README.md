# RSI Dynamic Overbought Oversold
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
The **RSI Dynamic Overbought Oversold** strategy is built around RSI with dynamic overbought/oversold levels.

Testing indicates an average annual return of about 178%. It performs best in the stocks market.

### Executed trading rules (C# and Python)

On **finished 5-minute candles**, calculate RSI(14), price SMA(50), and both SMA(50) and standard deviation(50) of RSI itself. The adaptive bands are `upper = min(RSI_MA + 2 * RSI_STD, 85)`, `lower = max(RSI_MA - 2 * RSI_STD, 15)`. When flat, buy only if `RSI <= lower` **and** `Close >= Price_SMA * 0.995`; sell only if `RSI >= upper` **and** `Close <= Price_SMA * 1.005`. A held long exits if `RSI >= RSI_MA` or `Close < Price_SMA * 0.995`; a held short exits if `RSI <= RSI_MA` or `Close > Price_SMA * 1.005`. An order starts a cooldown of **48 finished candles**. Filled positions are protected by a **native 2% percent stop**, not an ATR stop. Published return estimates above are not verified by the BTC/TON replay.



## Details
- **Entry Criteria**: see implementation for indicator conditions.
- **Long/Short**: Both directions.
- **Exit Criteria**: opposite signal or stop logic.
- **Stops**: Yes, using indicator-based calculations.
- **Default Values**:
  - `RsiPeriod = 14`
  - `MovingAvgPeriod = 50`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Overbought, Oversold
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

