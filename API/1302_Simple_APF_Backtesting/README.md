# Simple APF Strategy Backtesting
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy implements a simplified Autocorrelation Price Forecasting model. It detects price cycles via autocorrelation and forecasts future price using a linear regression of recent returns. A long position is opened when the predicted gain exceeds a specified threshold. The position is closed when the target price is reached.

## Parameters

- `Length` – number of bars used for autocorrelation and regression.
- `Threshold Gain` – minimum expected price increase to enter a trade.
- `Signal Threshold` – autocorrelation level required to store a forecast.
- `Candle Type` – type of candles for calculations.
