# DDE Example Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This sample strategy demonstrates how to send data from StockSharp to a Windows application through the DDE (Dynamic Data Exchange) interface. The strategy calculates an Exponential Moving Average (EMA) on the selected candle series and publishes the latest value via DDE.

## Details

- **Purpose**: Export indicator values to external software using the legacy DDE mechanism.
- **Indicator**: EMA.
- **Signals**: None; the strategy does not place orders.
- **DDE Items**:
  - `COMPANY!Value` – constant placeholder text.
  - `TIME!Value` – timestamp of the last processed candle.
  - `A!B` – EMA value formatted as string.

## Parameters

- `EmaLength` – period of the EMA (default 21).
- `CandleType` – candle type used for calculation (default 1-minute time frame).

The strategy requires a window named `MT4.DDE.2` to be present for DDE communication. Each finished candle triggers an update of the items above.
