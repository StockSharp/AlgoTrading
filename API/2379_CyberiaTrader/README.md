# Cyberia Trader Strategy

This strategy is a simplified StockSharp port of the original **CyberiaTrader.mq5** system. It combines several classic technical indicators to evaluate market direction and open trades when most filters agree.

## Indicators

- **MACD** – Detects momentum shifts using fast/slow EMAs and a signal line.
- **Simple Moving Average** – Determines the prevailing trend.
- **Commodity Channel Index** – Screens overbought/oversold conditions.
- **Average Directional Index** – Confirms directional strength via +DI and -DI components.

## Parameters

| Name | Description |
| --- | --- |
| `MacdFast` | Fast EMA period for MACD. |
| `MacdSlow` | Slow EMA period for MACD. |
| `MacdSignal` | Signal line period for MACD. |
| `MaPeriod` | Length of the moving average trend filter. |
| `CciPeriod` | Period of Commodity Channel Index. |
| `AdxPeriod` | Period of Average Directional Index. |
| `EnableMacd` | Enable/disable the MACD filter. |
| `EnableMa` | Enable/disable the moving average filter. |
| `EnableCci` | Enable/disable the CCI filter. |
| `EnableAdx` | Enable/disable the ADX filter. |
| `CandleType` | Timeframe of input candles. |

## Trading Logic

1. Values for all enabled indicators are calculated on each finished candle.
2. Filters can block buying or selling based on their respective rules:
   - MACD above its signal blocks short entries; below blocks long entries.
   - Price above the moving average blocks shorts; below blocks longs.
   - CCI above +100 blocks longs; below -100 blocks shorts.
   - +DI greater than -DI blocks shorts; -DI greater than +DI blocks longs.
3. A trade is opened only if one side is allowed and the opposite side is blocked.
4. Basic position protection uses 2% take-profit and 1% stop-loss.

## Notes

This translation focuses on the core directional filters of the original algorithm. The extensive probability analysis and auxiliary modules from the MQL5 version are intentionally omitted for clarity.
