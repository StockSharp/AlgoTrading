# AltrTrend Signal v2.2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp port of the MetaTrader expert advisor **Exp_AltrTrend_Signal_v2_2**. It recreates the adaptive
channel logic of the original AltrTrend Signal indicator and executes trades on delayed bars just like the MQL5 version. The
ADX value contracts or widens the channel so that breakouts only fire when trend strength supports them.

## How it works

1. A dynamic channel is calculated on every completed candle of the configured timeframe. The channel width is defined by the
   highest and lowest price inside a lookback that expands or contracts according to the previous ADX value (`KPeriod / ADX`).
2. The inner boundaries (`smin`, `smax`) are pulled toward the center by `KPercent`. Price must close outside these inner
   bounds to establish a directional trend state.
3. When the trend flips from bearish to bullish and the close is above the upper bound, a buy signal is generated. A bearish
   flip below the lower bound issues a sell signal. Signals are executed on the bar defined by the `SignalBar` delay, matching
the original expert advisor behaviour.
4. Optional stop-loss and take-profit levels are mapped from points to price steps so that protective exits mimic the original
   order placement with fixed SL/TP values.

## Details

- **Entry Criteria**:
  - **Long**: Previous trend was bearish or neutral, price closes above the upper contracted bound, and long entries are
    enabled. Short positions can be closed automatically if allowed.
  - **Short**: Previous trend was bullish or neutral, price closes below the lower contracted bound, and short entries are
    enabled. Long positions can be closed automatically if allowed.
- **Exit Criteria**:
  - Opposite breakout signal when exits are permitted for the current direction.
  - Stop-loss or take-profit distances expressed in price steps.
- **Long/Short**: Dual direction with independent enable/disable switches for entries and exits.
- **Risk Management**:
  - `StopLossPoints` and `TakeProfitPoints` replicate the original MM module by applying distance-based exits after market
    orders are filled.
- **Indicator Settings**:
  - `KPercent` controls how much the channel edges are contracted toward the mid-range.
  - `KStop` keeps the original arrow projection value for charting and logging.
  - `KPeriod` is the base lookback before ADX modulation.
  - `AdxPeriod` sets the length of the Average Directional Index that adapts the channel width.
  - `SignalBar` delays order execution by the specified number of completed candles.
- **Recommended Markets**:
  - Works best on instruments with clear swing phases where trend strength varies over time (forex majors, gold, and index
    futures). Default timeframe is H1 as in the MQL5 template.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `CandleType` | Timeframe used to build the adaptive channel. |
| `KPercent` | Percentage pulling the inner channel boundaries inward. |
| `KStop` | Multiplier for projected arrow prices (kept for compatibility). |
| `KPeriod` | Base number of candles examined before ADX adjustment. |
| `AdxPeriod` | Period for the Average Directional Index driving channel width. |
| `SignalBar` | Number of completed candles to wait before executing a signal. |
| `AllowBuyEntries` / `AllowSellEntries` | Enable or disable opening positions in each direction. |
| `AllowBuyExits` / `AllowSellExits` | Allow automatic closing of positions on opposite signals. |
| `StopLossPoints` | Stop-loss distance measured in price steps (0 disables). |
| `TakeProfitPoints` | Take-profit distance measured in price steps (0 disables). |

This port keeps the discretionary switches and risk parameters of the original expert advisor, making it easy to reproduce the
same behaviour inside StockSharp Designer, Shell, or Runner.