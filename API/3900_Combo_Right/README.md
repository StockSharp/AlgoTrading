# Combo Right Strategy

This strategy is a faithful StockSharp port of the MetaTrader expert advisor **Combo_Right.mq4**. It mixes a base Commodity Channel Index (CCI) momentum filter with three perceptrons that analyse open-price momentum over configurable bar strides. Depending on the `PassMode` the perceptrons can override the CCI signal and instruct the supervisor to open long or short positions with their dedicated risk parameters.

## Trading Logic

1. Subscribe to the configured candle type and calculate the CCI on open prices. The last completed candle provides both the close price and the historical open values for perceptron inputs.
2. Maintain a circular buffer of open prices so that the perceptrons can access the open of `period`, `2*period`, `3*period` and `4*period` bars ago without relying on indicator history getters.
3. When a finished candle arrives:
   - Evaluate the CCI value. This acts as the default signal (`> 0` = long, `< 0` = short) with the base protective distances (`TakeProfit1` / `StopLoss1`).
   - Depending on `PassMode`, compute one or several perceptrons. Each perceptron uses weights derived from the original MQL inputs (`X** - 100`) and the differences between the most recent close and historical opens.
   - If a perceptron condition is satisfied it overrides the default signal and assigns its own stop-loss / take-profit distances before any order is sent.
4. Cancel working orders, flatten opposite exposure and open the new position using the configured `TradeVolume`. After the market order is sent, call `SetTakeProfit` and `SetStopLoss` with the calculated offsets so the protective orders reflect the active perceptron branch.

### Pass modes

- **Pass 1** – only the CCI value is considered. The signal is proportional to the latest indicator value.
- **Pass 2** – if the first perceptron (`Perceptron1Period`, `X12…X42`) produces a negative output the strategy immediately opens a short trade with the second risk profile. Otherwise it falls back to the CCI outcome.
- **Pass 3** – if the second perceptron is positive the strategy opens a long trade with the third risk profile. Otherwise it relies on the CCI output.
- **Pass 4** – first check the third perceptron. A positive value requires the second perceptron to be positive as well to permit a long entry with the bullish risk profile. If the third perceptron is negative and the first perceptron is below zero, the supervisor opens a short with the bearish risk profile. If neither branch triggers, the CCI output is used.

In all modes the strategy ignores signals until enough candles are collected to feed the deepest perceptron stride.

## Risk Management

Every entry calculates fresh price offsets based on the symbol `PriceStep`. If the instrument does not provide a step the raw point distance is used as-is. `SetTakeProfit` and `SetStopLoss` receive the desired offsets together with the resulting net position so the protective brackets stay in sync with the current exposure.

## Parameters

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `TakeProfit1`, `StopLoss1` | `decimal` | 50 / 50 | Profit and loss distances (in points) when the CCI output is used. |
| `CciPeriod` | `int` | 10 | Period of the CCI calculated on open prices. |
| `X12`, `X22`, `X32`, `X42` | `int` | 100 | Raw weights for the bearish perceptron; the strategy internally subtracts 100 as in the original code. |
| `TakeProfit2`, `StopLoss2` | `decimal` | 50 / 50 | Risk distances (points) applied when the bearish perceptron triggers. |
| `Perceptron1Period` | `int` | 20 | Stride between samples for the bearish perceptron (in bars). |
| `X13`, `X23`, `X33`, `X43` | `int` | 100 | Raw weights for the bullish perceptron. |
| `TakeProfit3`, `StopLoss3` | `decimal` | 50 / 50 | Risk distances (points) applied when the bullish perceptron triggers. |
| `Perceptron2Period` | `int` | 20 | Stride between samples for the bullish perceptron (in bars). |
| `X14`, `X24`, `X34`, `X44` | `int` | 100 | Raw weights for the confirmation perceptron used in `PassMode = 4`. |
| `Perceptron3Period` | `int` | 20 | Stride between samples for the confirmation perceptron (in bars). |
| `PassMode` | `int` | 1 | Supervisor mode (1–4) that reproduces the branching logic of the MQL expert. |
| `TradeVolume` | `decimal` | 0.01 | Volume used for new market entries. Opposite exposure is closed before entering. |
| `CandleType` | `DataType` | M1 | Candle series feeding the CCI and perceptron inputs. |

## Notes

- The implementation intentionally waits until all perceptrons have enough historical open prices before trading, preventing array-bound issues that were implicit in MetaTrader.
- Indicator values are never retrieved through random access. Instead, the required history is stored in a circular buffer compliant with the project guidelines.
- All comments and documentation are kept in English to match the repository requirements.
