# BykovTrend + ColorX2MA MMRec Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This StockSharp strategy reproduces the MQL5 expert `Exp_BykovTrend_ColorX2MA_MMRec`. It combines two independent modules:
BykovTrend, which colours candles with a Williams %R filter, and ColorX2MA, which inspects the slope of a double-smoothed moving
average. Entries are issued whenever the selected module detects a fresh colour/slope change and money management is simplified
to use the strategy volume. Optional stop-loss and take-profit percentages can be enabled through StockSharp's built-in
protection block.

## Strategy Logic

### BykovTrend module
- Uses a Williams %R (`BykovTrendWprLength`) calculated on `BykovTrendCandleType` (default 2-hour candles).
- `BykovTrendRisk` controls the bullish/bearish thresholds (`33 - Risk` and `-Risk`).
- The indicator colour is evaluated on bar `BykovTrendSignalBar` (shift from the most recent closed bar).
- A bullish colour (< 2) closes shorts if `AllowBykovTrendCloseSell` is enabled and can open longs if
  `EnableBykovTrendBuy` is true and the previous colour was not bullish.
- A bearish colour (> 2) closes longs if `AllowBykovTrendCloseBuy` is enabled and can open shorts if
  `EnableBykovTrendSell` is true and the previous colour was not bearish.

### ColorX2MA module
- Two smoothing stages (`ColorX2MaMethod1`, `ColorX2MaLength1` and `ColorX2MaMethod2`, `ColorX2MaLength2`) are applied on
  the price defined by `ColorX2MaPriceType` using candles from `ColorX2MaCandleType`.
- The second stage output is compared with the prior value to generate slope states: rising (1), falling (2) or flat (0).
- The slope state is evaluated on bar `ColorX2MaSignalBar` (shift from the latest closed bar).
- A rising slope closes shorts (`AllowColorX2MaCloseSell`) and may open longs (`EnableColorX2MaBuy`) if the previous slope
  was not already rising.
- A falling slope closes longs (`AllowColorX2MaCloseBuy`) and may open shorts (`EnableColorX2MaSell`) if the previous slope
  was not already falling.

### Trade management
- Closing signals are executed before openings to emulate the original expert's order sequence.
- Orders use `Strategy.Volume` as the position size; the complex money-management recounter from the MQL version is not
  replicated.
- `StopLossPercent` and `TakeProfitPercent` enable `StartProtection` with percentage-based exits when greater than zero.

## Details

- **Long/Short**: Both directions supported.
- **Entry criteria**:
  - BykovTrend bullish colour transition.
  - ColorX2MA rising slope transition.
- **Exit criteria**:
  - Opposite colour/slope depending on enabled modules.
  - Optional percentage stop-loss/take-profit.
- **Filters**: None beyond the indicator logic.
- **Position sizing**: Fixed via `Strategy.Volume`.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `EnableBykovTrendBuy` | Allow BykovTrend to open long trades. | `true` |
| `EnableBykovTrendSell` | Allow BykovTrend to open short trades. | `true` |
| `AllowBykovTrendCloseBuy` | Close longs when BykovTrend turns bearish. | `true` |
| `AllowBykovTrendCloseSell` | Close shorts when BykovTrend turns bullish. | `true` |
| `BykovTrendRisk` | Williams %R sensitivity (smaller values react faster). | `3` |
| `BykovTrendWprLength` | Williams %R period. | `9` |
| `BykovTrendSignalBar` | Bar index (shift) to evaluate BykovTrend colour. | `1` |
| `BykovTrendCandleType` | Candle type/timeframe for BykovTrend. | `2h` time-frame |
| `EnableColorX2MaBuy` | Allow ColorX2MA to open long trades. | `true` |
| `EnableColorX2MaSell` | Allow ColorX2MA to open short trades. | `true` |
| `AllowColorX2MaCloseBuy` | Close longs when ColorX2MA slope turns bearish. | `true` |
| `AllowColorX2MaCloseSell` | Close shorts when ColorX2MA slope turns bullish. | `true` |
| `ColorX2MaMethod1` | Moving-average type for stage 1. | `Simple` |
| `ColorX2MaLength1` | Period for stage 1 smoothing. | `12` |
| `ColorX2MaPhase1` | Phase placeholder kept for documentation (not used). | `15` |
| `ColorX2MaMethod2` | Moving-average type for stage 2. | `Jurik` |
| `ColorX2MaLength2` | Period for stage 2 smoothing. | `5` |
| `ColorX2MaPhase2` | Phase placeholder kept for documentation (not used). | `15` |
| `ColorX2MaPriceType` | Price source for ColorX2MA smoothing. | `Close` |
| `ColorX2MaSignalBar` | Bar index (shift) to evaluate slope state. | `1` |
| `ColorX2MaCandleType` | Candle type/timeframe for ColorX2MA. | `2h` time-frame |
| `StopLossPercent` | Optional protective stop in percent (0 disables). | `0` |
| `TakeProfitPercent` | Optional protective take-profit in percent (0 disables). | `0` |

## Notes

- `ColorX2MaPhase1` and `ColorX2MaPhase2` are retained to mirror the original inputs but are not consumed because StockSharp's
  moving-average implementations do not expose a phase parameter.
- Only the smoothing methods available in StockSharp are provided; unsupported SmoothAlgorithms options fall back to the
  closest analogue.
- Money-management re-counters from `TradeAlgorithms.mqh` are not ported; position sizing should be handled by external risk
  controls or custom logic in StockSharp.

## Usage

1. Assign the desired security and set `Strategy.Volume` to the lot size you want to trade.
2. Configure candle types for BykovTrend and ColorX2MA if the default 2-hour timeframe is not appropriate.
3. Adjust smoothing methods/lengths and signal bar offsets to match the original setup or your own testing.
4. Optionally enable the protection block by setting `StopLossPercent` and/or `TakeProfitPercent` greater than zero.
5. Start the strategy; it will subscribe to the configured candle streams, monitor both modules and issue market orders in the
   sequence defined above.
