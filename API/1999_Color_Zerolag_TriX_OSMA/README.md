# Color Zerolag TriX OSMA Strategy

## Overview

This strategy uses a zero-lag TRIX OSMA oscillator built from five different TRIX periods. Each TRIX component is weighted and smoothed to form a single oscillator that reacts to trend changes with minimal lag. A long position is opened when the oscillator turns upward while a short position is opened when it turns downward.

## How It Works

1. Calculate five TRIX values using triple exponential moving averages and rate of change.
2. Combine the TRIX values with their weights to form a fast trend value.
3. Smooth the fast trend twice to create a zero-lag OSMA oscillator.
4. Detect trend reversals by comparing the last two oscillator values.
5. Enter long on upward turn and short on downward turn; existing opposite positions are closed before opening a new one.

## Parameters

- `Smoothing1` – smoothing factor for the slow trend.
- `Smoothing2` – smoothing factor for the OSMA line.
- `Factor1..Factor5` – weights applied to each TRIX component.
- `Period1..Period5` – periods for the five TRIX calculations.
- `CandleType` – candle series used for calculations.

## Indicators

- TripleExponentialMovingAverage
- RateOfChange
- Custom zero-lag TRIX OSMA combination

## Notes

The strategy requires all five TRIX indicators to be formed before generating signals. Protection for stops and targets is enabled via `StartProtection`.
