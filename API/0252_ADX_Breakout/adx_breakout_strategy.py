import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class adx_breakout_strategy(Strategy):
    """
    Strategy that trades on ADX breakouts.
    When ADX breaks out above its average, it enters position in the direction determined by price.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(adx_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._adx_period = self.Param("ADXPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 2)

        self._avg_period = self.Param("AvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for ADX average calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._multiplier = self.Param("Multiplier", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(0.0, 1.0, 0.1)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        # Internal indicators
        self._adx = None
        self._adx_average = None
        self._prev_adx_value = 0
        self._prev_adx_avg_value = 0

    @property
    def ADXPeriod(self):
        """ADX period."""
        return self._adx_period.Value

    @ADXPeriod.setter
    def ADXPeriod(self, value):
        self._adx_period.Value = value

    @property
    def AvgPeriod(self):
        """Period for ADX average calculation."""
        return self._avg_period.Value

    @AvgPeriod.setter
    def AvgPeriod(self, value):
        self._avg_period.Value = value

    @property
    def Multiplier(self):
        """Standard deviation multiplier for breakout detection."""
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLoss(self):
        """Stop-loss percentage."""
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(adx_breakout_strategy, self).OnStarted(time)

        self._prev_adx_value = 0
        self._prev_adx_avg_value = 0

        # Create indicators
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.ADXPeriod
        self._adx_average = SimpleMovingAverage()
        self._adx_average.Length = self.AvgPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)

        # First bind ADX to the candle subscription
        subscription.BindEx(self._adx, self.ProcessAdx).Start()

        # Enable stop loss protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLoss, UnitTypes.Percent)
        )
        # Create chart area for visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

    def ProcessAdx(self, candle, adxValue):
        if candle.State != CandleStates.Finished:
            return

        if not adxValue.IsFinal:
            return

        # Get current ADX value
        if adxValue.MovingAverage is None:
            return
        currentAdx = float(adxValue.MovingAverage)

        # Process ADX through average indicator
        adxAvgValue = process_float(self._adx_average, currentAdx, candle.ServerTime, candle.State == CandleStates.Finished)
        currentAdxAvg = float(to_float(adxAvgValue))

        # For first values, just save and skip
        if self._prev_adx_value == 0:
            self._prev_adx_value = currentAdx
            self._prev_adx_avg_value = currentAdxAvg
            return

        # Calculate standard deviation of ADX (simplified approach)
        stdDev = Math.Abs(currentAdx - currentAdxAvg) * 2  # Simplified approximation

        # Check if trading is allowed
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_adx_value = currentAdx
            self._prev_adx_avg_value = currentAdxAvg
            return

        # ADX breakout detection (ADX increases significantly above its average)
        if currentAdx > currentAdxAvg + self.Multiplier * stdDev:
            # Determine direction based on price movement
            priceDirection = candle.ClosePrice > candle.OpenPrice

            # Cancel active orders before placing new ones
            self.CancelActiveOrders()

            # Trade in the direction of price movement
            if priceDirection and self.Position <= 0:
                # Bullish breakout - Buy
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif not priceDirection and self.Position >= 0:
                # Bearish breakout - Sell
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        # Check for exit condition - ADX returns to average
        elif (self.Position > 0 and currentAdx < currentAdxAvg) or (
              self.Position < 0 and currentAdx < currentAdxAvg):
            # Exit position
            self.ClosePosition()

        # Update previous values
        self._prev_adx_value = currentAdx
        self._prev_adx_avg_value = currentAdxAvg

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adx_breakout_strategy()
