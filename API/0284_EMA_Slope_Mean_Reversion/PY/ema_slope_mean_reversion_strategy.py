import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ema_slope_mean_reversion_strategy(Strategy):
    """
    EMA Slope Mean Reversion Strategy - strategy based on mean reversion of exponential moving average slope.

    """

    def __init__(self):
        super(ema_slope_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._emaPeriod = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Exponential Moving Average period", "EMA Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._slopeLookback = self.Param("SlopeLookback", 20) \
            .SetDisplay("Slope Lookback", "Period for slope statistics", "Slope Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._thresholdMultiplier = self.Param("ThresholdMultiplier", 2.0) \
            .SetDisplay("Threshold Multiplier", "Standard deviation multiplier for entry threshold", "Slope Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state variables
        self._previousEmaValue = 0
        self._currentSlope = 0
        self._averageSlope = 0
        self._slopeStdDev = 0
        self._slopeCount = 0
        self._sumSlopes = 0
        self._sumSquaredDiff = 0

    @property
    def EmaPeriod(self):
        """EMA period."""
        return self._emaPeriod.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._emaPeriod.Value = value

    @property
    def SlopeLookback(self):
        """Period for calculating slope statistics."""
        return self._slopeLookback.Value

    @SlopeLookback.setter
    def SlopeLookback(self, value):
        self._slopeLookback.Value = value

    @property
    def ThresholdMultiplier(self):
        """Threshold multiplier for standard deviation."""
        return self._thresholdMultiplier.Value

    @ThresholdMultiplier.setter
    def ThresholdMultiplier(self, value):
        self._thresholdMultiplier.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def CandleType(self):
        """Candle type."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value


    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(ema_slope_mean_reversion_strategy, self).OnReseted()
        self._previousEmaValue = 0
        self._currentSlope = 0
        self._averageSlope = 0
        self._slopeStdDev = 0
        self._slopeCount = 0
        self._sumSlopes = 0
        self._sumSquaredDiff = 0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(ema_slope_mean_reversion_strategy, self).OnStarted(time)

        # Reset variables

        # Create EMA indicator
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        # Subscribe to candles and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)

        subscription.Bind(ema, self.ProcessCandle).Start()

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, emaValue):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate EMA slope only if we have previous EMA value
        if self._previousEmaValue != 0:
            # Calculate current slope
            self._currentSlope = emaValue - self._previousEmaValue

            # Update statistics
            self._slopeCount += 1
            self._sumSlopes += self._currentSlope

            # Update average slope
            if self._slopeCount > 0:
                self._averageSlope = self._sumSlopes / self._slopeCount

            # Calculate sum of squared differences for std dev
            self._sumSquaredDiff += (self._currentSlope - self._averageSlope) * (self._currentSlope - self._averageSlope)

            # Calculate standard deviation after we have enough samples
            if self._slopeCount >= self.SlopeLookback:
                self._slopeStdDev = Math.Sqrt(self._sumSquaredDiff / self._slopeCount)

                # Remove oldest slope value contribution (simple approximation)
                if self._slopeCount > self.SlopeLookback:
                    self._slopeCount = self.SlopeLookback
                    self._sumSlopes = self._averageSlope * self.SlopeLookback
                    self._sumSquaredDiff = self._slopeStdDev * self._slopeStdDev * self.SlopeLookback

                # Calculate entry thresholds
                lowerThreshold = self._averageSlope - self.ThresholdMultiplier * self._slopeStdDev
                upperThreshold = self._averageSlope + self.ThresholdMultiplier * self._slopeStdDev

                # Trading logic
                if self._currentSlope < lowerThreshold and self.Position <= 0:
                    # Slope is below lower threshold (falling rapidly) - mean reversion buy signal
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo("BUY Signal: Slope {0:F6} < Lower Threshold {1:F6}".format(self._currentSlope, lowerThreshold))
                elif self._currentSlope > upperThreshold and self.Position >= 0:
                    # Slope is above upper threshold (rising rapidly) - mean reversion sell signal
                    self.SellMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo("SELL Signal: Slope {0:F6} > Upper Threshold {1:F6}".format(self._currentSlope, upperThreshold))
                elif self._currentSlope > self._averageSlope and self.Position > 0:
                    # Exit long position when slope returns to average (profit target)
                    self.SellMarket(self.Position)
                    self.LogInfo("EXIT LONG: Slope {0:F6} returned to average {1:F6}".format(self._currentSlope, self._averageSlope))
                elif self._currentSlope < self._averageSlope and self.Position < 0:
                    # Exit short position when slope returns to average (profit target)
                    self.BuyMarket(Math.Abs(self.Position))
                    self.LogInfo("EXIT SHORT: Slope {0:F6} returned to average {1:F6}".format(self._currentSlope, self._averageSlope))

        # Save current EMA value for next calculation
        self._previousEmaValue = emaValue

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ema_slope_mean_reversion_strategy()
