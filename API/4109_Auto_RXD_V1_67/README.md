# Auto RXD V1.67 Strategy (C#)

This folder contains the StockSharp C# port of the MetaTrader 4 expert advisor **Auto_RXD_V1.67**. The original MQL robot combines three moving-average perceptrons with a rich confirmation block made of classical indicators. The conversion keeps the adaptive perceptron logic, the ATR driven risk template, and the main indicator filters so that the behaviour matches the legacy implementation while fitting naturally into the StockSharp high-level API.

## Core idea

* Three neural-style perceptrons built on linear weighted moving averages (LWMA) generate directional signals:
  * **Supervisor perceptron** gates both directions when `Mode = AiFilter`.
  * **Long perceptron** evaluates bullish momentum when `Mode = AiLong`.
  * **Short perceptron** evaluates bearish momentum when `Mode = AiShort`.
* An optional **indicator manager** confirms entries with ADX, MACD (with optional crossover rule), OsMA histogram, Parabolic SAR, RSI, CCI, Awesome Oscillator and Accelerator Oscillator.
* Protective exits can be static (distance expressed in points) or dynamically derived from ATR multiples.

## Trading modes

| Mode | Description |
|------|-------------|
| `Indicator` | Only the indicator manager generates entries. |
| `Grid` | Disabled in the port (kept for compatibility, no orders are placed). |
| `AiShort` | Uses the short perceptron to trigger short trades. |
| `AiLong` | Uses the long perceptron to trigger long trades. |
| `AiFilter` | Requires the supervisor perceptron to agree with the long/short perceptrons. |

## Parameters

All parameters are exposed through the `Param` API and support optimisation in the Designer. Key parameter groups:

* **General** – candle type, base order volume, trading window and master switch for new orders.
* **Risk** – ATR usage, ATR length, static TP/SL distances for long and short positions.
* **Perceptrons** – LWMA length, shift and four weights plus activation threshold for supervisor/long/short blocks.
* **Filters** – toggles and settings for ADX, MACD, OsMA, Parabolic SAR, RSI, CCI, Awesome Oscillator and Accelerator Oscillator confirmations.

## Trading logic

1. Subscribe to the selected timeframe via the high-level candle API.
2. Bind LWMA indicators for each perceptron, ATR, MACD, ADX, SAR, RSI, CCI, AO and AC.
3. As soon as all indicator histories are formed, evaluate the perceptron scores and indicator filters according to the current mode.
4. When a direction is approved, flatten opposite exposure, open a new market order, then assign stop-loss and take-profit distances via the helper functions `SetStopLoss` and `SetTakeProfit`.
5. Optionally close open trades when Parabolic SAR flips against the position.

## Notes

* The grid money management module from the original EA is intentionally disabled. All other core trading conditions and risk handling rules are preserved.
* Hourly trading windows handle wrap-around ranges (e.g., 22:00 → 06:00).
* Indicator filters can be mixed freely; when all are disabled the perceptrons work standalone.

## Usage

1. Open the solution in StockSharp Designer.
2. Add **AutoRxdV167Strategy** to a scheme and link it to the desired instrument and timeframe.
3. Configure perceptron weights, confirmation filters and risk parameters.
4. Run backtests or live trading as usual. The chart area shows the main LWMA curves, MACD, ADX and performed trades.

## Files

* `CS/AutoRxdV167Strategy.cs` – implementation of the strategy.
* `README.md` – English documentation.
* `README_cn.md` – Simplified Chinese overview.
* `README_ru.md` – Russian overview.
