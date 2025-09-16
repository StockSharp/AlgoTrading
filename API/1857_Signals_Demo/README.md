# Signals Demo
[Русский](README_ru.md) | [中文](README_cn.md)

This sample strategy demonstrates how to work with copy-trading signal parameters in StockSharp. The original MQL5 script built a GUI to browse and subscribe to trading signals. In this C# version the focus is on handling parameters and market data subscriptions. It logs the configured signal settings and prints information about each finished candle.

## Strategy Parameters
- `CandleType` – type of candles to process.
- `EquityLimit` – maximum equity allowed when copying trades.
- `Slippage` – allowed slippage in ticks.
- `DepositPercent` – percent of the account to allocate for signal copying.

## Trading Logic
The strategy does not place orders. It subscribes to the selected candle stream and logs every finished candle. The example shows how to configure parameters and attach to market data without performing trades.
