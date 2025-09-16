# Stufic Stochastic Strategy

This strategy combines trend detection using two moving averages with momentum signals from the Stochastic oscillator.
It buys when the fast moving average is above the slow moving average and the Stochastic %K line crosses above the %D line below an oversold threshold.
It sells when the fast moving average is below the slow moving average and %K crosses below %D above an overbought threshold.

## Logic
- Detects market trend by comparing a fast and a slow simple moving average.
- Uses Stochastic oscillator to find momentum reversals at extreme levels.
- Opens a long position when the trend is up and the oscillator exits the oversold zone with a bullish crossover.
- Opens a short position when the trend is down and the oscillator exits the overbought zone with a bearish crossover.
- Positions are closed or reversed on opposite signals. A stop-loss percentage is applied using built-in protection.

## Parameters
- **FastMaPeriod** – period of the fast moving average.
- **SlowMaPeriod** – period of the slow moving average.
- **StochKPeriod** – period for the %K line of the Stochastic.
- **StochDPeriod** – smoothing period for the %D line.
- **OverboughtLevel** – upper threshold for the Stochastic oscillator.
- **OversoldLevel** – lower threshold for the Stochastic oscillator.
- **StopLossPercent** – stop-loss distance expressed as percentage of entry price.
- **CandleType** – candle series used for calculations.

## Indicators
- Simple Moving Average (fast and slow).
- Stochastic Oscillator.

## Usage
Attach the strategy to a security. Configure the parameters to match the desired timeframe and risk level. Start the strategy to begin trading. The algorithm automatically manages positions based on the described conditions.

