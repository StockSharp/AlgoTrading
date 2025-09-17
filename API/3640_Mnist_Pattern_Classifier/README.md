# Mnist Pattern Classifier Strategy

## Origin

The strategy is a StockSharp port of the MetaTrader 5 expert **TestMnistOnnx.mq5** (MQL ID 47225). The original script exposes an interactive
canvas where the user draws digits that are classified by a bundled MNIST ONNX model. The StockSharp version keeps the spirit of
pattern recognition but replaces the hand-drawn canvas with a rolling matrix built from finished candles.

## Concept

1. A rolling window of `LookbackPeriod` completed candles (default 28) is treated as a 28×28 grid similar to a MNIST image.
2. Several statistical features – range compression, trend strength, momentum, RSI deviation and ATR normalization – are blended
   into a synthetic "confidence" score that mimics the neural network probability produced by the MQL expert.
3. The resulting features are mapped to one of ten pattern classes (`0`–`9`). Each class represents a market regime
   (flat, trend, breakout, pullback, reversal, etc.).
4. When the detected class matches the user-selected `TargetClass` and the synthetic confidence is above `ConfidenceThreshold`,
   the strategy opens or reverses a position in the indicated direction. Positions are flattened if the class changes or the
   confidence falls below the threshold.

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `LookbackPeriod` | 28 | Number of finished candles that are converted into the MNIST-like grid. |
| `TargetClass` | 1 | Class index (0–9) that should trigger trading actions. |
| `ConfidenceThreshold` | 0.6 | Minimum synthetic probability that allows order submission. |
| `Volume` | 1 | Order volume for new positions. |
| `CandleType` | 5-minute timeframe | Data type subscribed for candle updates. |

## Pattern Classes

| Class | Meaning |
|-------|---------|
| 0 | Flat or low-volatility consolidation. |
| 1 | Sustained bullish trend. |
| 2 | Sustained bearish trend. |
| 3 | Breakout to the upside with strong follow-through. |
| 4 | Breakout to the downside with strong follow-through. |
| 5 | Wide volatile range without clear bias. |
| 6 | Bullish pullback inside an uptrend. |
| 7 | Bearish pullback inside a downtrend. |
| 8 | Bullish reversal after an extended decline. |
| 9 | Bearish reversal after an extended advance. |

## Trading Rules

- Trades only on completed candles to remain in sync with the original expert that reacted to finalized drawings.
- Uses market orders (`BuyMarket`, `SellMarket`) and flattens before reversing to mimic the single-position behaviour of the
  original script.
- Confidence scaling is bounded to `[0, 1]`. Raising `ConfidenceThreshold` filters out weaker signals.
- The strategy does not manage protective stops; risk management is expected to be configured externally in StockSharp.

## Usage Tips

- Select a candle type that reflects the market rhythm you want to analyse. Shorter timeframes react faster but are noisier.
- Optimise `TargetClass` and `ConfidenceThreshold` together – some classes are naturally rarer and may require lower thresholds.
- The synthetic pattern classifier is deterministic; there is no dependency on external ONNX runtime libraries.
- Combine with the built-in risk protection tools available in StockSharp (such as `StartProtection`) to control exposure.

## Differences from the Original

- Interactive drawing and ONNX inference are replaced by fully automated candle analysis.
- The "confidence" is a deterministic blend of indicators rather than a neural network probability.
- Trading logic is added to convert pattern recognition into actionable orders.
- The MNIST resource file is not required in the StockSharp environment.
