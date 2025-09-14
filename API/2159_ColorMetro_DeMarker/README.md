# ColorMetro DeMarker Strategy

The **ColorMetro DeMarker Strategy** is a StockSharp implementation of the MQL5 expert advisor `Exp_ColorMETRO_DeMarker`.
It uses the DeMarker indicator combined with step levels to generate trading signals.

## Parameters
- **DeMarker Period** – period of the DeMarker indicator.
- **Fast Step** – step size used to build the fast level (MPlus).
- **Slow Step** – step size used to build the slow level (MMinus).
- **Candle Type** – time frame of candles for analysis.
- **Enable Buy Open** – allow opening long positions.
- **Enable Sell Open** – allow opening short positions.
- **Enable Buy Close** – allow closing long positions.
- **Enable Sell Close** – allow closing short positions.

## Trading Logic
1. The DeMarker value is scaled to 0–100 and two dynamic levels (MPlus and MMinus) are calculated using fast and slow step sizes.
2. When the previous fast level is above the previous slow level and the current fast level crosses below the slow level, the strategy buys and optionally closes short positions.
3. When the previous fast level is below the previous slow level and the current fast level crosses above the slow level, the strategy sells and optionally closes long positions.
4. All calculations use completed candles only.

This approach allows following trend shifts indicated by the stepped DeMarker levels.
