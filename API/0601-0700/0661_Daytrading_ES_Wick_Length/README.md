# Daytrading ES Wick Length Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy enters a long position when the total wick length of a candle exceeds its moving average plus an offset and exits after holding the position for a fixed number of bars.

### Executable signal contract (C# and Python)

For each **finished one-minute candle** define total wick length `W = max(0, High − max(Open, Close)) + max(0, min(Open, Close) − Low)`. Use the **previous 20 finished candles**, excluding the signal candle, to compute `VWMA(W) = sum(Wᵢ × Volumeᵢ) / sum(Volumeᵢ)` (fall back to arithmetic SMA if all volumes are zero). `MaType=Simple` selects the unweighted average instead. `MaOffset=10` is measured in **instrument minimum price ticks**, so the entry threshold is `VWMA(W) + 10 × Security.PriceStep`, not ten whole quote-currency units. After the 20-candle warmup, enter **long only**, and only from flat, if `W > threshold`. After **18 subsequently finished candles**, sell the entire remaining long. No short, no stop loss or take profit. Defaults: `MaLength=20`, `MaType=VolumeWeighted`, `MaOffset=10`, `HoldPeriods=18`, `CandleType=1 minute`.



## Details

- **Entry Criteria**: Total wick length greater than moving average with offset.
- **Exit Criteria**: Position closed after holding for `Hold periods` bars.
- **Long/Short**: Long only.
- **Stops**: None.
- **Default Values**:
  - `MA length` = 20
  - `MA type` = VolumeWeighted
  - `MA offset` = 10
  - `Hold periods` = 18
  - `Candle type` = 1-minute candles
- **Filters**:
  - Category: Volatility
  - Direction: Long
  - Indicators: Moving Average, Wick length
  - Stops: No
  - Complexity: Simple
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
