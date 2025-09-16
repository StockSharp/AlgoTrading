# Ichimoku Barabashkakvn Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy recreates Vladimir Karputov's Ichimoku expert advisor (barabashkakvn edition) on top of the StockSharp high-level API. It blends the classic Tenkan/Kijun crossover with confirmation from the Kumo cloud and adds detailed risk management identical to the MetaTrader original.

## How It Works

- **Indicator stack** – a single Ichimoku Kinko Hyo indicator supplies Tenkan-sen, Kijun-sen, Senkou Span A, and Senkou Span B values. The default periods remain 9/26/52.
- **Long entries** – triggered when Tenkan crosses up through Kijun and the closing price is above Senkou Span B. Cross detection uses the previous Tenkan value, mirroring the bar-by-bar logic from the EA.
- **Short entries** – appear when Tenkan crosses down through Kijun while the close is below Senkou Span A.
- **Position management** – only one net position is maintained. Opposite signals close existing trades first, reproducing the two-step reversal flow of the script.
- **Trading window** – optional hour filter lets the system trade only between configured start/end hours (inclusive) using the same comparison as the MQL version.

## Risk Management

- **Directional stops and targets** – long and short positions use independent stop-loss/take-profit distances in pips. Pips are converted to price units using the instrument step size with a 10× adjustment for 3- and 5-decimal quotes, matching the EA's point handling.
- **Trailing stop** – each direction has its own trailing distance plus a common trailing step. The stop advances only after the move exceeds `(trailing distance + trailing step)`, exactly as in the original code.
- **Protective execution** – stop-loss and take-profit checks occur on every finished candle so that virtual protective levels behave like the broker-managed orders from MetaTrader.

## Parameters

- `TenkanPeriod` *(default 9)* – Tenkan-sen length.
- `KijunPeriod` *(default 26)* – Kijun-sen length.
- `SenkouSpanBPeriod` *(default 52)* – Senkou Span B length.
- `CandleType` *(default 1-hour candles)* – data source for calculations.
- `OrderVolume` *(default 1 lot)* – trade size.
- `BuyStopLossPips` / `SellStopLossPips` *(default 100)* – stop-loss distances in pips.
- `BuyTakeProfitPips` / `SellTakeProfitPips` *(default 300)* – take-profit distances in pips.
- `BuyTrailingStopPips` / `SellTrailingStopPips` *(default 50)* – trailing distances in pips.
- `TrailingStepPips` *(default 5)* – minimal profit increment required to shift the trailing stop.
- `UseTradeHours` *(default false)* – enable the session filter.
- `StartHour` / `EndHour` *(defaults 0/23)* – inclusive trading window boundaries (0–23).

These defaults match the published EA. All parameters are exposed through `StrategyParam<T>` objects, so they can be optimized or tuned inside StockSharp Designer without touching the source.
