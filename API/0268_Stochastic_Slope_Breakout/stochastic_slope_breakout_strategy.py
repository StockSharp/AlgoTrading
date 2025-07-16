import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class stochastic_slope_breakout_strategy(Strategy):
    """ 
    Strategy based on Stochastic Oscillator %K Slope breakout
    Enters positions when the slope of Stochastic %K exceeds average slope plus a multiple of standard deviation
    """

    def __init__(self):
        super(stochastic_slope_breakout_strategy, self).__init__()

        # Stochastic period
        self._stochPeriod = self.Param("StochPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Period for Stochastic Oscillator", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        # Stochastic %K smoothing period
        self._kPeriod = self.Param("KPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("K Period", "Smoothing period for %K line", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        # Stochastic %D period
        self._dPeriod = self.Param("DPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("D Period", "Period for %D line", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        # Lookback period for slope statistics calculation
        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # Standard deviation multiplier for breakout detection
        self._deviationMultiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Stop loss percentage
        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        # Candle type
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal variables
        self._stochastic = None
        self._prevStochKValue = 0.0
        self._currentSlope = 0.0
        self._avgSlope = 0.0
        self._stdDevSlope = 0.0
        self._slopes = []
        self._currentIndex = 0
        self._isInitialized = False

    @property
    def StochPeriod(self):
        return self._stochPeriod.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stochPeriod.Value = value

    @property
    def KPeriod(self):
        return self._kPeriod.Value

    @KPeriod.setter
    def KPeriod(self, value):
        self._kPeriod.Value = value

    @property
    def DPeriod(self):
        return self._dPeriod.Value

    @DPeriod.setter
    def DPeriod(self, value):
        self._dPeriod.Value = value

    @property
    def LookbackPeriod(self):
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def DeviationMultiplier(self):
        return self._deviationMultiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviationMultiplier.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    def GetWorkingSecurities(self):
        """Return the security and candle type this strategy works with."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """Initialize indicators, subscriptions and charting."""
        super(stochastic_slope_breakout_strategy, self).OnStarted(time)

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.KPeriod
        self._stochastic.D.Length = self.DPeriod

        self._prevStochKValue = 0.0
        self._currentSlope = 0.0
        self._avgSlope = 0.0
        self._stdDevSlope = 0.0
        self._slopes = [0.0] * self.LookbackPeriod
        self._currentIndex = 0
        self._isInitialized = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._stochastic, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._stochastic)
            self.DrawOwnTrades(area)

        # Set up position protection
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, stochValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if indicator is formed
        if not self._stochastic.IsFormed:
            return

        # Extract Stochastic %K value - the main line that we'll track the slope of
        # Stochastic returns a complex value with both %K and %D values
        stochTyped = stochValue
        kValue = stochTyped.K
        if kValue is None:
            return

        # Initialize on first valid value
        if not self._isInitialized:
            self._prevStochKValue = kValue
            self._isInitialized = True
            return

        # Calculate current Stochastic %K slope (difference between current and previous values)
        self._currentSlope = kValue - self._prevStochKValue

        # Store slope in array and update index
        self._slopes[self._currentIndex] = self._currentSlope
        self._currentIndex = (self._currentIndex + 1) % self.LookbackPeriod

        # Calculate statistics once we have enough data
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self.CalculateStatistics()

        # Trading logic
        if Math.Abs(self._avgSlope) > 0:  # Avoid division by zero
            # Long signal: Stochastic %K slope exceeds average + k*stddev (slope is positive and we don't have a long position)
            if self._currentSlope > 0 and \
                    self._currentSlope > self._avgSlope + self.DeviationMultiplier * self._stdDevSlope and \
                    self.Position <= 0:
                # Cancel existing orders
                self.CancelActiveOrders()

                # Enter long position
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)

                self.LogInfo("Long signal: %K Slope {0} > Avg {1} + {2}*StdDev {3}".format(
                    self._currentSlope, self._avgSlope, self.DeviationMultiplier, self._stdDevSlope))
            # Short signal: Stochastic %K slope exceeds average + k*stddev in negative direction (slope is negative and we don't have a short position)
            elif self._currentSlope < 0 and \
                    self._currentSlope < self._avgSlope - self.DeviationMultiplier * self._stdDevSlope and \
                    self.Position >= 0:
                # Cancel existing orders
                self.CancelActiveOrders()

                # Enter short position
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

                self.LogInfo("Short signal: %K Slope {0} < Avg {1} - {2}*StdDev {3}".format(
                    self._currentSlope, self._avgSlope, self.DeviationMultiplier, self._stdDevSlope))

            # Exit conditions - when slope returns to average
            if self.Position > 0 and self._currentSlope < self._avgSlope:
                # Exit long position
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit long: %K Slope {0} < Avg {1}".format(self._currentSlope, self._avgSlope))
            elif self.Position < 0 and self._currentSlope > self._avgSlope:
                # Exit short position
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: %K Slope {0} > Avg {1}".format(self._currentSlope, self._avgSlope))

        # Store current Stochastic %K value for next slope calculation
        self._prevStochKValue = kValue

    def CalculateStatistics(self):
        # Reset statistics
        self._avgSlope = 0
        sumSquaredDiffs = 0

        # Calculate average slope
        for i in range(self.LookbackPeriod):
            self._avgSlope += self._slopes[i]
        self._avgSlope /= self.LookbackPeriod

        # Calculate standard deviation of slopes
        for i in range(self.LookbackPeriod):
            diff = self._slopes[i] - self._avgSlope
            sumSquaredDiffs += diff * diff

        self._stdDevSlope = Math.Sqrt(sumSquaredDiffs / self.LookbackPeriod)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return stochastic_slope_breakout_strategy()
