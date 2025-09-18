# Elli Ichimoku ADX Strategy

## Overview
The strategy is a C# port of the MetaTrader 5 expert "Elli" (barabashkakvn's edition). It combines Ichimoku Kinko Hyo structure with an Average Directional Index (+DI) breakout filter. Trades are opened only when a strong directional impulse is confirmed simultaneously by Ichimoku line alignment and a sudden surge in the positive directional index.

The StockSharp implementation keeps the original behaviour of working with two candle streams: Ichimoku analysis is performed on a higher timeframe (default 1 hour) while ADX is evaluated on a faster series (default 1 minute). Orders are entered with a fixed protective stop and target measured in price steps, identical to the original expert advisor.

## Indicators and data
- **Ichimoku** (Tenkan 19, Kijun 60, Senkou Span B 120 by default).
- **Average Directional Index (ADX)**, only the +DI line is used as in the source code.
- Optional chart areas display the candle series, Ichimoku cloud and the ADX line.

Two independent candle subscriptions are created:
1. `IchimokuCandleType` (default 1 hour) – drives Ichimoku calculations and generates trading decisions.
2. `AdxCandleType` (default 1 minute) – feeds the ADX indicator and supplies current/previous +DI values.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `TakeProfitPoints` | 60 | Take profit distance in price steps. Set to 0 to disable. |
| `StopLossPoints` | 30 | Stop loss distance in price steps. Set to 0 to disable. |
| `TenkanPeriod` | 19 | Length of the Ichimoku Tenkan-sen (conversion line). |
| `KijunPeriod` | 60 | Length of the Ichimoku Kijun-sen (base line). |
| `SenkouSpanBPeriod` | 120 | Length of the Ichimoku Senkou Span B line. |
| `AdxPeriod` | 10 | Period for the ADX indicator. |
| `PlusDiHighThreshold` | 13 | Threshold that the current +DI value must exceed. |
| `PlusDiLowThreshold` | 6 | Threshold that the previous +DI value must stay below. |
| `BaselineDistanceThreshold` | 20 | Minimum Tenkan/Kijun spread (in price steps) required to confirm momentum. |
| `IchimokuCandleType` | 1 hour candles | Candle series used for Ichimoku evaluation. |
| `AdxCandleType` | 1 minute candles | Candle series used for ADX calculation. |

## Trading logic
1. Wait for one finished Ichimoku candle.
2. Ensure ADX has at least two finished values and the last reading produced a +DI breakout (`previous +DI < PlusDiLowThreshold` and `current +DI > PlusDiHighThreshold`).
3. Convert the Tenkan/Kijun spread into price steps and verify it exceeds `BaselineDistanceThreshold`.
4. All orders are blocked if an open position already exists.
5. **Buy** when:
   - Tenkan > Kijun.
   - Kijun > Senkou Span A.
   - Senkou Span A > Senkou Span B (bullish cloud).
   - Closing price > Kijun.
6. **Sell** when the reverse alignment is observed (Tenkan < Kijun < Senkou Span A < Senkou Span B and the close is below Kijun).
7. Position exits rely on the protective stop and target configured via `StartProtection`. No discretionary exit is triggered; this mirrors the original EA that waited for stops/targets or manual intervention.

## Risk management
`StartProtection` is called once on start. If either stop or target is zero the respective protection is omitted. Orders are sent with market execution (`BuyMarket`/`SellMarket`), matching the MQL implementation that used market orders with attached SL/TP.

## Implementation notes
- Only the positive directional index is used for both long and short signals, replicating the logic of the MQL5 code (the original author commented out the -DI branch).
- The strategy does not track the Chikou span explicitly; instead, cloud alignment is validated by comparing Senkou Span A and B.
- Internal fields store the last two +DI values without calling `GetValue`, in accordance with the high-level API guidelines.
- If both candle parameters are identical, a single subscription is reused for Ichimoku and ADX to reduce overhead.

## Usage tips
- Keep `AdxCandleType` faster than `IchimokuCandleType` to emulate the MT5 version (e.g., M1 ADX vs. H1 Ichimoku).
- Raise `BaselineDistanceThreshold` on high-volatility instruments to demand wider Tenkan/Kijun separation.
- Because the expert opens only one position at a time, combine the strategy with portfolio-level risk controls when trading multiple symbols.
