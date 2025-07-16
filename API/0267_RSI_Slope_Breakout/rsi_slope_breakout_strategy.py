import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_slope_breakout_strategy(Strategy):
    """
    Strategy based on RSI (Relative Strength Index) Slope breakout
    Enters positions when the slope of RSI exceeds average slope plus a multiple of standard deviation
    """

    def __init__(self):
        super(rsi_slope_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._rsiPeriod = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for RSI indicator", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviationMultiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Indicators and state variables
        self._rsi = None
        self._prevRsiValue = 0.0
        self._currentSlope = 0.0
        self._avgSlope = 0.0
        self._stdDevSlope = 0.0
        self._slopes = None
        self._currentIndex = 0
        self._isInitialized = False

    @property
    def RsiPeriod(self):
        """RSI period"""
        return self._rsiPeriod.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsiPeriod.Value = value

    @property
    def LookbackPeriod(self):
        """Lookback period for slope statistics calculation"""
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def DeviationMultiplier(self):
        """Standard deviation multiplier for breakout detection"""
        return self._deviationMultiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviationMultiplier.Value = value

    @property
    def CandleType(self):
        """Candle type"""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage"""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    def OnStarted(self, time):
        super(rsi_slope_breakout_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        self._prevRsiValue = 0
        self._currentSlope = 0
        self._avgSlope = 0
        self._stdDevSlope = 0
        self._slopes = [0.0] * self.LookbackPeriod
        self._currentIndex = 0
        self._isInitialized = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

        # Set up position protection
        self.StartProtection(
            takeProfit=None,  # We'll handle exits via strategy logic
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
        )

    def ProcessCandle(self, candle, rsi_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if indicator is formed
        if not self._rsi.IsFormed:
            return

        # Initialize on first valid value
        if not self._isInitialized:
            self._prevRsiValue = rsi_value
            self._isInitialized = True
            return

        # Calculate current RSI slope (difference between current and previous RSI values)
        self._currentSlope = rsi_value - self._prevRsiValue

        # Store slope in array and update index
        self._slopes[self._currentIndex] = self._currentSlope
        self._currentIndex = (self._currentIndex + 1) % self.LookbackPeriod

        # Calculate statistics once we have enough data
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self.CalculateStatistics()

        # Trading logic
        if abs(self._avgSlope) > 0:  # Avoid division by zero
            # Long signal: RSI slope exceeds average + k*stddev (slope is positive and we don't have a long position)
            if self._currentSlope > 0 and \
                    self._currentSlope > self._avgSlope + self.DeviationMultiplier * self._stdDevSlope and \
                    self.Position <= 0:
                # Cancel existing orders
                self.CancelActiveOrders()

                # Enter long position
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)

                self.LogInfo(f"Long signal: RSI Slope {self._currentSlope} > Avg {self._avgSlope} + {self.DeviationMultiplier}*StdDev {self._stdDevSlope}")
            # Short signal: RSI slope exceeds average + k*stddev in negative direction (slope is negative and we don't have a short position)
            elif self._currentSlope < 0 and \
                    self._currentSlope < self._avgSlope - self.DeviationMultiplier * self._stdDevSlope and \
                    self.Position >= 0:
                # Cancel existing orders
                self.CancelActiveOrders()

                # Enter short position
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

                self.LogInfo(f"Short signal: RSI Slope {self._currentSlope} < Avg {self._avgSlope} - {self.DeviationMultiplier}*StdDev {self._stdDevSlope}")

            # Exit conditions - when slope returns to average
            if self.Position > 0 and self._currentSlope < self._avgSlope:
                # Exit long position
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo(f"Exit long: RSI Slope {self._currentSlope} < Avg {self._avgSlope}")
            elif self.Position < 0 and self._currentSlope > self._avgSlope:
                # Exit short position
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo(f"Exit short: RSI Slope {self._currentSlope} > Avg {self._avgSlope}")

        # Store current RSI value for next slope calculation
        self._prevRsiValue = rsi_value

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
        return rsi_slope_breakout_strategy()
