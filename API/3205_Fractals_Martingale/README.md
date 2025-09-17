# Fractals Martingale Strategy

This folder contains the StockSharp port of the MetaTrader expert advisor "Fractals Martingale". The strategy mixes Bill Williams
fractals, an Ichimoku-based trend filter and a monthly MACD confirmation. Position sizing follows a classic martingale sequence
that multiplies the trade volume after every losing cycle while an optional cool-down prevents runaway exposure.

## Trading logic

1. **Fractal detection on the working timeframe** – finished candles are buffered to detect local highs and lows separated by
   `FractalDepth` neighbours. A bullish setup is registered when the next candle opens above the fractal high, while a bearish
   setup requires the next open below the fractal low. Detected levels stay valid for `FractalLookback` processed candles.
2. **Ichimoku trend filter** – the fractal must align with the Ichimoku trend calculated on the higher timeframe defined by
   `IchimokuCandleType`. Long trades demand Tenkan-sen above Kijun-sen, short trades require Tenkan-sen below Kijun-sen.
3. **Monthly MACD confirmation** – the original EA used a monthly MACD to decide whether buyers or sellers dominate. The port
   subscribes to the `MacdCandleType` series (30-day candles by default) and only accepts long signals when the MACD line is above
   the signal line; short signals need the opposite condition.
4. **Session filter** – orders are placed only between `StartHour` (inclusive) and `EndHour` (exclusive). A wrap-around window is
   supported for overnight trading sessions.
5. **Martingale volume scaling** – the base order size comes from `TradeVolume`. After every losing round the next order volume is
   multiplied by `Multiplier` and aligned to the instrument volume step. Winning trades reset the sequence. When
   `MaxConsecutiveLosses` is exceeded the algorithm pauses for `PauseMinutes` before resuming with the base volume.
6. **Direction switching** – whenever a new trade is sent the strategy automatically offsets any opposite position before opening
   exposure in the requested direction.

## Risk management

- `StopLossPips` and `TakeProfitPips` are converted to absolute price distances using the detected pip size and applied through
  `StartProtection`. This mirrors the original EA where both stops were defined in pips.
- The original implementation exposed optional money-based trailing stops. The StockSharp port relies on the built-in protective
  block because real portfolio currency handling is broker-specific.

## Parameters

| Parameter | Description |
| --- | --- |
| `TradeVolume` | Base order size used for the first entry of a sequence. |
| `Multiplier` | Factor applied to the next trade volume after a loss. |
| `StopLossPips`, `TakeProfitPips` | Protective stop and target distances measured in pips. |
| `FractalDepth` | Number of candles on each side required to confirm a fractal high/low. |
| `FractalLookback` | Maximum number of processed candles for which a detected fractal remains valid. |
| `StartHour`, `EndHour` | Trading window expressed in exchange hours. When both values match the filter is disabled. |
| `MaxConsecutiveLosses` | Number of losing trades before the strategy pauses. |
| `PauseMinutes` | Duration of the cool-down period activated after exceeding the loss cap. |
| `TenkanPeriod`, `KijunPeriod`, `SenkouPeriod` | Ichimoku Kinko Hyo lengths used on the higher timeframe. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | EMA lengths for the higher-timeframe MACD confirmation. |
| `CandleType` | Primary candle series where fractals and executions are evaluated. |
| `IchimokuCandleType` | Higher timeframe used to calculate Tenkan and Kijun lines. |
| `MacdCandleType` | Timeframe used to calculate the MACD filter (monthly by default). |

## Usage notes

1. **Pip size calculation** – the pip value is derived from `Security.PriceStep`. Five-digit forex quotes are automatically scaled
   to match the MetaTrader definition used in the source EA.
2. **Indicator subscriptions** – the strategy consumes up to three candle series. Ensure the data feed can supply all requested
   timeframes to keep the filters in sync.
3. **Martingale precautions** – doubling the volume quickly increases exposure. Use the cool-down parameters or lower the
   multiplier if the account cannot withstand prolonged losing streaks.
4. **Differences vs. the MT4 EA** – mail/notification alerts, balance-based trailing stops and explicit margin checks were removed
   because StockSharp already handles connectivity, portfolio safety and order execution. The core entry/exit logic matches the
   MQL implementation.

## Files

- `CS/FractalsMartingaleStrategy.cs` – C# implementation using the high-level Strategy API.
- `README.md` – English documentation (this file).
- `README_cn.md` – Simplified Chinese translation.
- `README_ru.md` – Russian translation.
