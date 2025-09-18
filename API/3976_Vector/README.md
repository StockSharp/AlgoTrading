# Vector Strategy (MT4 Port)

This folder contains the StockSharp high-level API port of the MetaTrader 4 expert advisor **Vector** (original script: `MQL/8305/Vector.mq4`). The strategy coordinates up to four major forex pairs — EURUSD (primary), GBPUSD, USDCHF, and USDJPY — and trades them in the same direction when a shared smoothed moving average alignment appears. The conversion keeps the core Vector ideas while adapting them to idiomatic StockSharp patterns.

## Trading logic

1. **Smoothed moving averages (SMMA)** – each instrument tracks a fast (3-period) and slow (7-period) SMMA calculated on median prices of the configurable trading timeframe (15 minutes by default).
2. **Vector trend filter** – the differences between every fast/slow pair are summed. A positive sum signals synchronized bullish momentum across the basket, while a negative sum implies collective bearish pressure.
3. **Entry rules** – the strategy opens or reverses positions with market orders only when:
   - The basket trend is positive and the instrument's fast SMMA stays above the slow SMMA (long entry).
   - The basket trend is negative and the fast SMMA is below the slow SMMA (short entry).
4. **Pip target from H4 range** – for every instrument a separate 4-hour candle subscription measures the previous range. One fifth of that range (capped at 13 pips) becomes the per-position profit objective, mirroring the fixed-pip exit from the MT4 code.
5. **Global equity guard** – percentage-based profit and drawdown thresholds (taken from the original `PrcProfit` and `PrcLose` inputs) close all open positions once triggered.

## Key differences vs. the original EA

- StockSharp's **high-level candle subscriptions and indicator binding** replace the low-level polling found in MT4 (`SubscribeCandles().Bind(...)`).
- The port supports **optional secondary instruments**: leave the GBPUSD / USDCHF / USDJPY slots empty to trade only the main security.
- The dynamic lot sizing tied to MT4 account margin has been replaced with a clean `BaseVolume` parameter that is normalized to each security's `VolumeStep`, `MinVolume`, and `MaxVolume`.
- Trade management stores entry prices through `OnNewMyTrade` callbacks, avoiding disallowed direct indicator value lookups.

## Parameters

| Name | Default | Description |
| ---- | ------- | ----------- |
| `CandleType` | `TimeSpan.FromMinutes(15)` | Timeframe used for the SMMA calculations and entry checks. |
| `RangeCandleType` | `TimeSpan.FromHours(4)` | Higher timeframe used to derive the adaptive pip target. |
| `SecondSecurity` | `null` | Optional GBPUSD slot (set a `Security` before start). |
| `ThirdSecurity` | `null` | Optional USDCHF slot. |
| `FourthSecurity` | `null` | Optional USDJPY slot. |
| `BaseVolume` | `1` | Requested trade volume per order, normalized to exchange limits. |
| `TakeProfitPercent` | `0.5` | Global equity gain (in %) that triggers a portfolio-wide exit. |
| `MaxDrawdownPercent` | `30` | Maximum allowed equity drawdown (in %) before all positions close. |

## Usage notes

- Assign the same connector and portfolio to every security referenced by the parameters before starting the strategy.
- Make sure the data source delivers both the trading timeframe and the range timeframe for all instruments.
- When the optional securities are not provided, the vector calculation automatically adapts to the available instruments.
- Exits always happen with market orders to match the original MT4 behaviour.

## Files

- `CS/VectorStrategy.cs` – C# implementation following the StockSharp high-level guidelines.
- `README.md`, `README_ru.md`, `README_cn.md` – strategy documentation in English, Russian, and Chinese.
