# Neural Network MACD Strategy

This strategy combines a simple four-weight perceptron filter with a classic MACD crossover. A position is opened only when both the MACD and the neural network agree on the direction.

## How it works

1. **Perceptron filter**  
   Three perceptrons evaluate price momentum using differences between the current close and a set of past open prices. Each perceptron has four integer weights (`X11`…`X34`) where `0` means no influence. The perceptron output is a weighted sum of the price differences.  
   Depending on the `Pass` parameter, one, two or all three perceptrons participate in decision making. The filter also defines stop-loss and take-profit distances (`Sl1`, `Tp1`, `Sl2`, `Tp2`).
2. **MACD confirmation**  
   A standard MACD (12, 26, 9) is calculated. A buy signal appears when the MACD line is below zero and crosses above the signal line. A sell signal is when the line is above zero and crosses below the signal line.
3. **Trade execution**  
   - Long position is opened if both the MACD and the perceptron filter are positive.  
   - Short position is opened if both are negative.  
   The position is closed when either a stop-loss or take-profit level is reached.

## Parameters

| Name | Description |
| ---- | ----------- |
| `X11…X34` | Weights for perceptron inputs. |
| `Tp1`, `Sl1` | Take-profit and stop-loss for the first perceptron. |
| `Tp2`, `Sl2` | Take-profit and stop-loss for the second perceptron. |
| `P1`, `P2`, `P3` | Shifts in bars used to calculate perceptron inputs. |
| `Pass` | Number of perceptrons to use (1-3). |
| `CandleType` | Candle series for calculations. |

