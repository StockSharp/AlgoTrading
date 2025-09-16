# Volatility Pivot Strategy

## Overview
The Volatility Pivot strategy is a high-level StockSharp port of the original **Exp_VolatilityPivot.mq5** expert advisor. It recreates the custom Volatility Pivot indicator by projecting two adaptive stop lines that trail price using either Average True Range (ATR) volatility or a fixed price deviation. When the trend flips, the indicator emits single-bar breakout arrows that trigger position reversals. The strategy can follow those signals (`WithTrend`) or trade against them (`CounterTrend`), providing flexibility for breakout or mean-reversion styles.

Unlike the MQL implementation, this version relies entirely on finished candles supplied by `CandleType`. The ATR mode multiplies a smoothed ATR (EMA of ATR) by `AtrMultiplier`, while the price mode uses the raw `DeltaPrice` offset. The resulting pivot lines define bullish and bearish trailing levels that govern entries and exits.

## Market Data and Indicators
- **Primary candles (`CandleType`)** – all calculations are performed on this timeframe. The default is a 4-hour bar to match the source expert advisor.
- **ATR + EMA smoothing** – in `Atr` mode the strategy processes an `AverageTrueRange` with length `AtrPeriod` and then smooths it by an `ExponentialMovingAverage` of length `SmoothingPeriod`.
- **Price deviation mode** – in `PriceDeviation` mode the trailing offset is the fixed `DeltaPrice` amount, allowing deterministic stop distances when volatility smoothing is not desired.
- **Pivot state tracking** – the strategy keeps the latest bullish/bearish trail values and raises “signals” only on the bar where the trail flips from one side of price to the other, mirroring the indicator buffers of the MQL version.

## Trading Logic
1. **Pivot computation** – for every finished candle the strategy updates the trailing stop price according to the Volatility Pivot rules. A bullish trail is active when price closes above the calculated stop; a bearish trail is active when it closes below.
2. **Signal detection** – a new bullish (bearish) signal is fired when the bullish (bearish) trail becomes active after being inactive on the previous bar. The `SignalBar` parameter delays execution by the requested number of completed bars, replicating the `SignalBar` input of the MQL script.
3. **Direction filter (`TradeDirection`)** – when set to `WithTrend` the strategy buys on bullish signals and sells on bearish signals. When set to `CounterTrend` the interpretation is inverted: bullish arrows close shorts and open new shorts, and vice versa.
4. **Entry permissions** – `EnableBuyEntries` and `EnableSellEntries` gate whether new long or short positions may be opened.
5. **Exit permissions** – `AllowLongExits` and `AllowShortExits` control whether existing positions may be closed by either direct signals or by the opposing trail staying active.
6. **Position adjustment** – the strategy targets a net position of `+Volume` for longs, `-Volume` for shorts, and `0` when flattening. Orders are sized automatically to close any opposing exposure before establishing the new direction.
7. **Protective stops** – optional `StopLoss` and `TakeProfit` distances (expressed in absolute price units) monitor each finished candle. If the bar’s high/low breaches those levels the strategy immediately exits the position.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Candle series used for indicator processing and execution. | 4-hour candles |
| `AtrPeriod` | Length of the ATR component. | 100 |
| `SmoothingPeriod` | EMA smoothing length applied to ATR values. | 10 |
| `AtrMultiplier` | Multiplier applied to the smoothed ATR. | 3.0 |
| `DeltaPrice` | Fixed price offset used when `PivotMode = PriceDeviation`. | 0.002 |
| `PivotMode` | Chooses between ATR-based or fixed-deviation pivots. | `Atr` |
| `TradeDirection` | Follows (`WithTrend`) or fades (`CounterTrend`) pivot breakouts. | `WithTrend` |
| `SignalBar` | Number of completed bars to wait before acting on a signal. | 1 |
| `EnableBuyEntries` | Allow opening new long positions. | `true` |
| `EnableSellEntries` | Allow opening new short positions. | `true` |
| `AllowLongExits` | Allow closing existing long positions when bearish conditions persist. | `true` |
| `AllowShortExits` | Allow closing existing short positions when bullish conditions persist. | `true` |
| `StopLoss` | Optional stop-loss distance (absolute price units). Set to `0` to disable. | 0 |
| `TakeProfit` | Optional take-profit distance (absolute price units). Set to `0` to disable. | 0 |

> **Note:** The StockSharp `Strategy.Volume` property defines the position size. Configure it before starting the strategy to match the instrument’s contract or share size.

## Usage Guidelines
1. Attach the strategy to the desired `Security`, `Portfolio`, and set `Volume` to the intended lot size.
2. Ensure the data source can supply the selected `CandleType`. Without a continuous feed of finished candles the ATR smoothing and signal delay logic cannot form.
3. Choose `PivotMode` based on market behavior: ATR mode adapts to volatility, while price deviation mode keeps the trail fixed.
4. Adjust `SignalBar` to reproduce the exact timing of the original expert advisor (1 bar lag by default). Setting it to `0` executes on the most recent finished bar.
5. When using `StopLoss`/`TakeProfit`, calibrate the distances to instrument volatility (they are absolute prices, not points or percentages).
6. Monitor logs for informational messages about entries, exits, and protective stops triggered by pivot changes.

## Differences from the Original Expert Advisor
- Money-management options based on account balance/free margin were removed. Position size is controlled solely through `Strategy.Volume`.
- Order price “deviation” and manual time synchronization from the MQL helper library are unnecessary because StockSharp uses market orders on finished candles.
- Notification features, global variables, and manual history loading present in the MQL script are omitted.
- Protective stop and take-profit handling is simplified to candle-based checks; there is no intrabar order placement.

## Recommended Enhancements
- Add daily session filters or volatility filters to pause trading during low-liquidity hours.
- Extend the strategy with trailing-stop management that mirrors the pivot lines, or export the calculated lines to a chart for visualization.
- Incorporate portfolio-level risk controls if multiple instruments use the same strategy instance.
