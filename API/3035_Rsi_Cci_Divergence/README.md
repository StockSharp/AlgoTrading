# RSI & CCI Divergence Strategy

## Overview
The **RSI & CCI Divergence Strategy** is a conversion of the MetaTrader expert advisor `RSI&CCI_DIVERGENCE.mq4` (MQL ID 22266). The system searches for bearish or bullish divergences between price highs and two oscillators (Commodity Channel Index and Relative Strength Index), filters them with a linear weighted moving average trend filter, validates the signal with MACD alignment on three different timeframes, and confirms momentum strength using a higher timeframe momentum oscillator. Optional absolute stop-loss and take-profit targets can be applied to manage open positions.

The StockSharp implementation focuses on the high-level API. Indicators are bound directly to candle subscriptions, and all computations are driven by streaming candle updates without manual indicator value retrieval.

## Trading Logic
1. **Trend Filter**
   - Fast and slow linear weighted moving averages (LWMA) on the primary timeframe define the prevailing direction.
   - Bullish context requires the fast LWMA to be above the slow LWMA; bearish context requires the opposite.

2. **Divergence Detection**
   - The last closed candle is compared with up to `CandlesToRetrace` previous candles.
   - A bullish signal occurs if either CCI or RSI makes a higher low while the corresponding earlier candle shows a higher high than the last high (bullish divergence).
   - A bearish signal occurs if either CCI or RSI makes a lower high while the corresponding earlier candle shows a lower high than the last high (bearish divergence).

3. **MACD Confirmation**
   - MACD (12, 26, 9 by default) is evaluated on the primary, higher, and macro timeframes.
   - Long trades require MACD to be above the signal line on all timeframes.
   - Short trades require MACD to be below the signal line on all timeframes.

4. **Momentum Confirmation**
   - A momentum oscillator (length 14 by default) is sampled on a higher timeframe (default 1 hour).
   - The absolute deviation of recent momentum readings from the neutral 100 level must exceed the configured buy/sell thresholds to approve the trade.

5. **Price Structure Guard**
   - The strategy checks recent highs/lows to mimic the original EA constraints (`Low[2] < High[1]` for longs and `Low[1] < High[2]` for shorts).

6. **Order Execution**
   - When all filters align, the strategy enters using `BuyMarket` or `SellMarket` with volume equal to the base strategy volume plus the absolute value of the current position, allowing for immediate reversal.

7. **Risk Management**
   - Optional absolute stop-loss and take-profit distances are evaluated on every finished candle.
   - If configured, the strategy sends a market order sized to flatten the position when the stop or target is touched.

## Parameters
| Name | Default | Description |
| --- | --- | --- |
| `FastMaLength` | 6 | Period for the fast LWMA trend filter. |
| `SlowMaLength` | 85 | Period for the slow LWMA trend filter. |
| `CciLength` | 14 | Lookback for the Commodity Channel Index. |
| `RsiLength` | 14 | Lookback for the Relative Strength Index. |
| `CandlesToRetrace` | 10 | Number of completed candles used to detect divergences. |
| `MacdFastPeriod` | 12 | Fast moving average period in the MACD calculation. |
| `MacdSlowPeriod` | 26 | Slow moving average period in the MACD calculation. |
| `MacdSignalPeriod` | 9 | Signal line period for MACD. |
| `MomentumLength` | 14 | Length of the higher-timeframe momentum oscillator. |
| `MomentumBuyThreshold` | 0.3 | Minimum absolute deviation from 100 for bullish momentum confirmation. |
| `MomentumSellThreshold` | 0.3 | Minimum absolute deviation from 100 for bearish momentum confirmation. |
| `StopLoss` | 0 | Absolute price distance for an optional stop-loss (0 disables the stop). |
| `TakeProfit` | 0 | Absolute price distance for an optional take-profit (0 disables the target). |
| `CandleType` | 15-minute time frame | Primary candle type for divergence and trend analysis. |
| `MomentumCandleType` | 1-hour time frame | Candle type used for the momentum confirmation. |
| `HigherMacdCandleType` | 1-hour time frame | Secondary timeframe for MACD confirmation. |
| `MacroMacdCandleType` | 30-day time frame | Macro timeframe for MACD confirmation (adjust to match the instrument data availability). |

## Usage Notes
- Ensure that all referenced timeframes are available from the data provider; otherwise adjust the candle type parameters accordingly.
- The default stop-loss and take-profit values are disabled to reflect the original EA behaviour where risk was managed via trailing and equity stops. Set positive decimal values to enable hard stops.
- Because momentum confirmation compares values with the 100 baseline, it assumes the StockSharp `Momentum` indicator uses the classic definition (`100 * Close / Close[N]`). If a different normalization is preferred, adjust the thresholds to match the instrument volatility.
- The strategy sends market orders for both entries and exits, mirroring the immediate execution logic of the source expert advisor.

## Conversion Notes
- The conversion uses StockSharp's high-level indicator binding. No manual calls to `GetValue` are required; indicator values are provided by the binding callbacks.
- Equity-based stop management, trailing logic, and email/notification features from the MQL source are not ported. Instead, focus is placed on the primary signal generation and basic stop/target handling.
- Divergence detection is implemented using lightweight lists to maintain the recent price and indicator history necessary for pattern recognition.
