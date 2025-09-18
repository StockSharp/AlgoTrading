# EA OBJPROP Chart ID Strategy

## Overview

The **EA OBJPROP Chart ID Strategy** recreates the chart-focused behavior of the original MetaTrader 5 example by displaying Donchian Channel envelopes on three synchronized timeframes. The primary chart hosts the trading timeframe while two auxiliary panels visualize the H4 and Daily context. This setup mirrors the original Expert Advisor that stacked multiple charts and indicators in a single workspace for visual analysis.

## Key Features

- **Multi-timeframe visualization** – automatically subscribes to primary, H4, and Daily candles for the selected security.
- **Unified Donchian Channel length** – applies the same channel period to every timeframe to keep the envelopes comparable.
- **High-level chart integration** – relies on StockSharp chart areas to render price series, Donchian Channels, and executed trades, reproducing the MQL layout without low-level object manipulation.
- **Extensible foundation** – stores the latest channel boundaries for each timeframe, making it straightforward to extend the strategy with breakout or confirmation logic in the future.

## Parameters

| Parameter | Description | Category | Default |
|-----------|-------------|----------|---------|
| `ChannelLength` | Length of the Donchian Channel used across all subscribed timeframes. | Indicators | 22 |
| `PrimaryCandleType` | Main timeframe used for trading and as the top chart panel. | General | 30-minute candles |
| `H4CandleType` | Auxiliary H4 timeframe displayed in a secondary panel. | General | 4-hour candles |
| `DailyCandleType` | Auxiliary Daily timeframe displayed in a tertiary panel. | General | 1-day candles |

All parameters are available through StockSharp parameter UI, support optimization, and may be fine-tuned without changing the code.

## Strategy Logic

1. Initializes three Donchian Channel indicators with the same length parameter.
2. Subscribes to the selected primary, H4, and Daily candle series for the current security.
3. Binds each subscription to its respective channel indicator using the high-level API, ensuring indicator values are computed incrementally.
4. Creates one main chart area and up to two auxiliary areas where candles, channels, and the strategy's trades are drawn.
5. Stores the most recent upper and lower channel boundaries for each timeframe, enabling custom decision rules to be added later on.

The current implementation is visualization-only and does not submit orders. This mirrors the original MetaTrader code, which focused on composing a dashboard of charts without automated trading logic.

## Usage Notes

- Ensure the selected security has historical data for every timeframe used by the strategy to populate all chart areas.
- You can change any of the timeframe parameters to other `TimeFrame` data types (e.g., 15 minutes or weekly candles) if different context panels are required.
- Additional trade logic can be layered in the processing methods (`ProcessPrimary`, `ProcessH4`, `ProcessDaily`) by reacting to the stored channel levels.

## Conversion Notes

- The MetaTrader example created child charts via `OBJ_CHART` objects; the StockSharp version replaces that with chart areas created by the high-level API, which is better integrated with the platform.
- Indicator management is performed via `BindEx` calls instead of manual handle creation, ensuring values are synchronized with incoming candles.
- Object deletion routines are not required because StockSharp automatically disposes subscriptions and chart bindings when the strategy stops.
