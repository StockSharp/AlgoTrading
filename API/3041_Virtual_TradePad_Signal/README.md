# Virtual TradePad Signal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy recreates the multi-indicator dashboard logic from the MetaTrader VirtualTradePad tool. It tracks twelve signals –
trend, momentum and channel based – and only trades when a configurable number of indicators agree. The goal is to mimic the
visual sentiment matrix of the original panel and convert it into a fully automated StockSharp strategy.

## How it works

- **Data**: trades a single instrument on the selected candle type (default 15 minutes).
- **Indicators**:
  - Fast/slow simple moving averages for crossover direction.
  - MACD line and signal crossover.
  - Stochastic %K oversold/overbought exits (20/80 levels).
  - RSI 30/70 threshold reversals.
  - CCI -100/+100 reversals.
  - Williams %R -80/-20 reversals.
  - Bollinger Bands breakout back inside the channel.
  - Moving average envelope breakout back inside the channel.
  - Bill Williams Alligator jaw/teeth/lips alignment.
  - Kaufman Adaptive Moving Average slope (rising/falling).
  - Awesome Oscillator zero-line crosses.
  - Ichimoku Tenkan-Kijun crossing.
- Each indicator produces a buy (+1), sell (-1) or neutral (0) vote. When the count of buy votes (or sell votes) reaches the
  **MinimumConfirmations** parameter and exceeds the opposite side, the strategy opens a position in that direction.
- Optional **CloseOnOpposite** closes the position when the opposite vote count reaches the threshold.
- **Risk management**: optional take profit and stop loss defined in instrument price steps.

## Parameters

- `FastMaLength`, `SlowMaLength` – lengths for the crossover moving averages.
- `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – MACD configuration.
- `StochasticLength`, `StochasticDLength`, `StochasticSlowing` – Stochastic oscillator setup.
- `RsiLength`, `CciLength`, `WilliamsLength` – oscillator lookbacks.
- `BollingerLength`, `BollingerDeviation` – Bollinger Bands.
- `EnvelopeLength`, `EnvelopeDeviation` – percentage envelopes around SMA.
- `AlligatorJawLength`, `AlligatorTeethLength`, `AlligatorLipsLength` – Alligator SMMAs.
- `AmaLength`, `AmaFastPeriod`, `AmaSlowPeriod` – Kaufman AMA configuration.
- `IchimokuTenkanLength`, `IchimokuKijunLength`, `IchimokuSenkouLength` – Ichimoku lines.
- `AoShortPeriod`, `AoLongPeriod` – Awesome Oscillator windows.
- `MinimumConfirmations` – number of aligned signals required to enter.
- `AllowLong`, `AllowShort` – enable long/short sides.
- `CloseOnOpposite` – exit when the opposite vote count satisfies the threshold.
- `TakeProfitPips`, `StopLossPips` – optional risk targets in price steps (0 disables).
- `CandleType` – timeframe/data type for analysis.

## Trading logic summary

1. Update all indicators when a candle closes.
2. Count bullish and bearish votes from the indicators.
3. Enter long/short when votes reach the confirmation threshold and exceed the opposite side.
4. Optionally flatten when the opposite side reaches the threshold.
5. Apply optional take profit/stop loss measured in price steps.

The strategy is designed for discretionary traders who liked the VirtualTradePad sentiment board but want an automated
implementation inside the StockSharp framework.
