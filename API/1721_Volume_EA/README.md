# Volume EA Strategy

## Overview
This strategy trades based on spikes in volume and the Commodity Channel Index (CCI). It opens positions at the start of a new hour when the previous candle's volume exceeds that of the candle before it by a configurable factor. CCI values must fall within specific bands to confirm the signal.

## Rules
- Only one position is open at a time.
- At the beginning of each hour:
  - **Long entry** when:
    - The previous candle is bullish.
    - Previous volume > prior volume × `Factor`.
    - CCI is between `CciLevel1` and `CciLevel2`.
  - **Short entry** when:
    - The previous candle is bearish.
    - Previous volume > prior volume × `Factor`.
    - CCI is between `CciLevel4` and `CciLevel3`.
- A trailing stop of `TrailingStop` price steps protects profits.
- All positions are closed when the hour equals 23.

## Parameters
- `Factor` – volume multiplier threshold.
- `TrailingStop` – trailing distance in price steps.
- `CciLevel1` / `CciLevel2` – CCI bounds for long trades.
- `CciLevel3` / `CciLevel4` – CCI bounds for short trades.
- `CandleType` – candle timeframe used for calculations.
