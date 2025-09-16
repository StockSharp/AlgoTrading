# MovingUp Strategy

This strategy implements a moving average crossover system with optional risk management.
It opens a long position when the fast moving average crosses above the slow moving average and opens a short position on the opposite crossover.

## Parameters
- **Fast MA** (`FastLength`): Period of the fast simple moving average.
- **Slow MA** (`SlowLength`): Period of the slow simple moving average.
- **Use TP** (`UseTakeProfit`): Enables the take-profit rule.
- **TP** (`TakeProfit`): Distance in price for taking profit.
- **Use SL** (`UseStopLoss`): Enables the stop-loss rule.
- **SL** (`StopLoss`): Distance in price for stop loss.
- **Use TS** (`UseTrailingStop`): Enables trailing stop logic.
- **TS** (`TrailingStop`): Trailing stop distance in price.
- **Candle** (`CandleType`): Candle type used for calculations.

## Trading Logic
1. Subscribe to candle data and calculate two SMA indicators.
2. Detect crossovers of fast and slow MAs.
3. Enter long when the fast MA crosses above the slow MA if no long position exists.
4. Enter short when the fast MA crosses below the slow MA if no short position exists.
5. Apply risk management on each new candle:
   - Take profit when price advances by the specified distance.
   - Stop loss when price moves against the position by the specified distance.
   - Trailing stop protects profit once price moves favorably.

## Original MQL Strategy
The original MQL4 script `ma_v_1_3_3.mq4` contains numerous additional features such as step up/down logic and complex position sizing. This C# version focuses on the core moving average crossover and essential risk controls.
