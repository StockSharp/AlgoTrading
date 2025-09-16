# Smoothed MA Directional Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy is a StockSharp high-level API port of the MetaTrader 4 expert `oc08_vy_m0moqesu15` from the `MQL/8615` folder. The original expert aligns its position with a single smoothed moving average (SMMA) and attaches fixed stop-loss and take-profit levels to every order. The C# version keeps the same directional behaviour while adopting idiomatic StockSharp components.

## Trading idea

- **Directional bias:** Price closing above the smoothed moving average indicates an uptrend; closing below signals a downtrend.
- **Position alignment:** The strategy always tries to maintain a single position in the direction of the detected trend. If the market flips sides, it immediately reverses the position.
- **Risk control:** Every entry is protected by stop-loss and take-profit offsets expressed in price steps. StockSharp's `StartProtection` helper replaces the manual SL/TP assignment in the original MQ4 code.
- **Execution style:** Orders are submitted as market orders on candle close, replicating the `OrdersTotal()==0` logic of the MetaTrader expert.

## How it works

1. On startup the strategy subscribes to candles of the configured timeframe and binds a `SmoothedMovingAverage` indicator with the selected period.
2. When a candle finishes, the indicator value is compared with the candle close.
3. If the close is higher than the SMMA and the strategy is flat or short, it sends a market buy sized to cover the short exposure (if any) and open a long position.
4. If the close is lower than the SMMA and the strategy is flat or long, it sends a market sell sized to cover the long exposure (if any) and open a short position.
5. Protective stop-loss and take-profit distances are configured once at start using the current security `PriceStep`. If both offsets are set to zero the protection is disabled.
6. Chart output (candles, indicator, trades) is drawn automatically when the strategy runs inside environments that expose a chart area.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `StopLossPoints` | 100 | Stop-loss distance in price steps. Set to `0` to disable the stop.
| `TakeProfitPoints` | 100 | Take-profit distance in price steps. Set to `0` to disable the target.
| `MaPeriod` | 12 | Period of the smoothed moving average used to gauge the trend.
| `TradeVolume` | 1 | Market order volume. The strategy also writes this value into `Strategy.Volume` on start.
| `CandleType` | 15-minute time frame | Candle type (time frame) driving the indicator and signals.

All parameters are configurable through StockSharp Designer/Runner and include optimization ranges for automated testing.

## Differences from the MetaTrader version

- Margin-based lot sizing (`Lots`/`Prots`) is replaced with a fixed `TradeVolume` parameter. This keeps the behaviour deterministic and compatible with StockSharp's portfolio abstraction.
- Stop-loss and take-profit are handled by `StartProtection` instead of manual order amendments, matching the original offsets but using StockSharp primitives.
- The strategy ignores unfinished candles to avoid premature trades, mirroring the `New_Bar` flag in MQ4.

## Practical notes

- Ensure the connected security provides a valid `PriceStep`. If not, the strategy falls back to a unit step of `1` when computing SL/TP distances.
- The indicator length is synchronised with the current parameter value on every candle, allowing live parameter adjustments.
- To reproduce the original behaviour, configure the same timeframe as the chart that hosted the MQ4 expert and keep the trade volume consistent with your desired contract size.
