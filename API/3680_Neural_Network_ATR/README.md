# Neural Network ATR Strategy

## Overview

The strategy replicates the "Neurotest" expert advisor by combining a lightweight neural
network layer with ATR based money management inside StockSharp. The model consumes the
latest completed M15 candle and transforms it into five normalized features: close to
close momentum, intraday range, candle body, volume expansion and volatility (ATR to
price ratio). A single hidden layer with a sigmoid output produces a probability score
that is scaled by a dynamic learning rate. The score is compared against user defined
buy and sell thresholds to open or flip positions.

## Trading Rules

1. Subscribe to 15 minute candles (configurable) and compute ATR of the same period.
2. Build the five normalized features from the previous candle and the current finished
   candle, then evaluate the neural network.
3. When the adjusted prediction is above the buy threshold and the current position is
   not long, enter a long trade (closing short exposure if required).
4. When the adjusted prediction is below the sell threshold and the current position is
   not short, enter a short trade.
5. Each entry attaches ATR based stop-loss and take-profit orders. If ATR is not formed,
   a fallback distance in points is used.
6. If the current spread exceeds the configured limit the candle is ignored.

## Risk Management

- Position size is calculated from portfolio equity and the ATR stop distance so that the
  loss at the stop equals `Max Risk %` of equity.
- Protective orders use a configurable risk-to-reward multiplier.
- Trading halts automatically when daily or total drawdown exceeds their limits.
- A penalty system decreases the learning rate by 10% (down to a minimum) when the daily
  profit target is not achieved, which dampens future signals until the next trading day.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **Max Risk %** | Maximum risk per trade as a percentage of equity. |
| **Daily Loss %** | Daily drawdown threshold that stops trading. |
| **Total Loss %** | Global drawdown threshold that stops trading. |
| **Daily Profit %** | Daily profit target before the penalty is skipped. |
| **Learning Rate** | Scaling factor applied to the neural output. |
| **Hidden Layer** | Number of neurons in the hidden layer. |
| **Buy Threshold / Sell Threshold** | Trigger levels for long and short entries. |
| **Candle Type** | Candle type and timeframe used for signals. |
| **ATR Period** | Period of the ATR indicator. |
| **Max Spread** | Maximum allowed spread in price steps. |
| **Risk Reward** | Take-profit multiplier relative to the stop distance. |
| **Fallback Stop** | Stop distance in points when ATR is unavailable. |

## Notes

- Level1 subscription is required to monitor bid/ask spread before each decision.
- The neural network weights are randomly initialized but deterministic (seed 42). The
  learning rate modulation emulates the adaptive behaviour of the original MQL expert.
- Ensure the connected portfolio provides `CurrentValue`, `StepPrice` and volume limits
  so that position sizing and protective orders operate correctly.
