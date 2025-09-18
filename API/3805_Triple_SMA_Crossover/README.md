# Triple SMA Crossover Strategy

## Overview
The Triple SMA Crossover strategy replicates the original MQL expert advisor `3sma.mq4`. The system analyses three simple moving averages (SMA) calculated on the closing price and trades when the short-term trend aligns with the medium- and long-term averages. The conversion keeps the original trading rules while adapting them to the StockSharp high-level strategy API.

## Trading Logic
1. Calculate three SMAs with configurable periods.
2. Exit existing long positions when the fast SMA drops below the medium SMA.
3. Exit existing short positions when the fast SMA rises above the medium SMA.
4. Enter a new long position when:
   - Fast SMA is above the medium SMA by at least the configured spread.
   - Medium SMA is above the slow SMA by at least the configured spread.
   - No long position is currently open.
5. Enter a new short position when:
   - Fast SMA is below the medium SMA by at least the configured spread.
   - Medium SMA is below the slow SMA by at least the configured spread.
   - No short position is currently open.

## Parameters
- **Candle Type** – Primary timeframe used to compute the moving averages.
- **Fast SMA Length** – Period for the fast SMA (MQL input `SMA1`).
- **Medium SMA Length** – Period for the medium SMA (MQL input `SMA2`).
- **Slow SMA Length** – Period for the slow SMA (MQL input `SMA3`).
- **SMA Spread Steps** – Additional filter requiring SMAs to diverge by a number of price steps (MQL input `SMAspread`).
- **Trade Volume** – Order volume used when opening positions (MQL input `lots`).

## Notes
- Stop loss handling from the MQL version is omitted because it was disabled in the source script.
- All exits are market orders to align with the straightforward behaviour of the original expert.
