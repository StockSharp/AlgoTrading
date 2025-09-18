# Mc Valute Cloud Strategy

This folder contains the StockSharp port of the MetaTrader expert advisor "Mc_valute". The original robot combined a short
exponential moving average (EMA) with three smoothed moving averages, an Ichimoku cloud filter and multiple MACD instances while
scaling into the trend. The StockSharp implementation keeps the core trend confirmation stack but simplifies position management
to a single exposure in each direction so that the logic fits naturally into the high-level API.

## Trading logic

1. **Price filter EMA** – the `FilterMaLength` EMA must sit above (for longs) or below (for shorts) the two smoothed moving
   averages (`BlueMaLength` and `LimeMaLength`). The smoothed averages emulate the "blue" and "lime" lines from the MT4 template.
2. **Ichimoku cloud confirmation** – the EMA also has to be outside of the cloud. Long trades require the filter EMA above both
   Senkou spans while short trades demand that it remains below the cloud bottom.
3. **MACD momentum check** – the main MACD line has to be above its signal line for long entries and below it for short entries.
   Only the first MACD set from the original EA is kept because the remaining copies were disabled in the final MQL version.
4. **Single-position management** – whenever a new signal appears the strategy offsets any existing opposite position and opens a
   fresh trade with the configured `Volume`. Protective orders are updated immediately after the market order is sent.
5. **Candle-by-candle evaluation** – all indicators operate on the timeframe defined by `CandleType`. Trading decisions are taken
   only on finished candles to mirror the MT4 `start()` handler which processed closed bars.

## Risk management

- `TakeProfit` and `StopLoss` are measured in price points. After each entry the helper `SetTakeProfit` and `SetStopLoss`
  functions are called using the expected resulting position size, which mirrors the MT4 behaviour where stops were applied per
  ticket.
- The original expert advisor pyramided up to three additional orders using the `Step` distance. The StockSharp port keeps a
  single position to stay within the high-level order helpers. Users who need scaling can increase `Volume` or clone the
  strategy across several portfolios.

## Parameters

| Parameter | Description |
| --- | --- |
| `Volume` | Base trade size used by the high-level `BuyMarket`/`SellMarket` calls. |
| `CandleType` | Primary candle series driving the indicators and trade logic. |
| `FilterMaLength` | Length of the EMA trend filter. |
| `BlueMaLength`, `LimeMaLength` | Lengths of the two smoothed moving averages acting as the directional band. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | EMA lengths for the MACD confirmation. |
| `TenkanLength`, `KijunLength`, `SenkouLength` | Ichimoku Kinko Hyo settings for the cloud filter. |
| `TakeProfit`, `StopLoss` | Protective distances expressed in price points. |

## Usage notes

1. **Indicator shifts** – MetaTrader allowed non-zero "shift" parameters when building the smoothed moving averages. StockSharp's
   indicators work on the current bar, therefore the port ignores those shifts while keeping the original periods.
2. **MACD variants** – the source code declared three MACD blocks but only the first one participated in live signals. The port
   follows that behaviour; additional MACD filters can be re-enabled by duplicating the indicator bindings.
3. **Scaling trades** – the MT4 robot sent up to three averaging orders separated by `Step` points. This behaviour is documented
   but intentionally omitted because high-level strategies operate with a single aggregated position.
4. **Protective block** – `StartProtection()` is invoked once during startup so that the built-in infrastructure supervises stop
   and target orders even after reconnections.

## Files

- `CS/McValuteCloudStrategy.cs` – C# implementation using the high-level Strategy API with indicator bindings and detailed
  comments.
- `README.md` – English documentation (this file).
- `README_cn.md` – Simplified Chinese translation.
- `README_ru.md` – Russian translation.
