import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, ICandleMessage, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class hull_ma_slope_mean_reversion_strategy(Strategy):
    """
    Hull Moving Average Slope Mean Reversion Strategy.
    This strategy trades based on the mean reversion of the Hull Moving Average slope.
    """
    def __init__(self):
        super(hull_ma_slope_mean_reversion_strategy, self).__init__()

        # Constructor.
        self._hullPeriod = self.Param("HullPeriod", 9) \
            .SetDisplay("Hull MA Period", "Hull Moving Average period", "Hull MA") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Lookback period for calculating the average and standard deviation of slope", "Mean Reversion") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviationMultiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Mean Reversion") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for stop loss calculation", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._atrMultiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stop loss calculation", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._hullMa = None
        self._atr = None
        self._slopeAverage = None
        self._slopeStdDev = None

        self._currentHullMa = 0
        self._prevHullMa = 0
        self._currentSlope = 0
        self._prevSlope = 0
        self._prevSlopeAverage = 0
        self._prevSlopeStdDev = 0
        self._currentAtr = 0

    @property
    def HullPeriod(self):
        """Hull Moving Average period."""
        return self._hullPeriod.Value

    @HullPeriod.setter
    def HullPeriod(self, value):
        self._hullPeriod.Value = value

    @property
    def LookbackPeriod(self):
        """Lookback period for calculating the average and standard deviation of slope."""
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def DeviationMultiplier(self):
        """Deviation multiplier for mean reversion detection."""
        return self._deviationMultiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviationMultiplier.Value = value

    @property
    def AtrPeriod(self):
        """ATR period for stop loss calculation."""
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def AtrMultiplier(self):
        """ATR multiplier for stop loss calculation."""
        return self._atrMultiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atrMultiplier.Value = value

    @property
    def CandleType(self):
        """Candle type."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        super(hull_ma_slope_mean_reversion_strategy, self).OnStarted(time)

        # Initialize indicators
        self._hullMa = HullMovingAverage()
        self._hullMa.Length = self.HullPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod
        self._slopeAverage = SimpleMovingAverage()
        self._slopeAverage.Length = self.LookbackPeriod
        self._slopeStdDev = StandardDeviation()
        self._slopeStdDev.Length = self.LookbackPeriod

        # Reset stored values
        self._currentHullMa = 0
        self._prevHullMa = 0
        self._currentSlope = 0
        self._prevSlope = 0
        self._prevSlopeAverage = 0
        self._prevSlopeStdDev = 0
        self._currentAtr = 0

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)

        # First binding for Hull MA
        subscription.BindEx(self._hullMa, self._atr, self.ProcessIndicators).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._hullMa)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle: ICandleMessage, hullValue, atrValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self._currentAtr = to_float(atrValue)

        # Get the Hull MA value
        self._currentHullMa = to_float(hullValue)

        # First value handling
        if self._prevHullMa == 0:
            self._prevHullMa = self._currentHullMa
            return

        # Calculate the slope of Hull MA
        self._currentSlope = (self._currentHullMa - self._prevHullMa) / self._prevHullMa * 100  # As percentage

        # Calculate average and standard deviation of slope
        slopeAverage = to_float(process_float(self._slopeAverage, self._currentSlope, candle.ServerTime, candle.State == CandleStates.Finished))
        slopeStdDev = to_float(process_float(self._slopeStdDev, self._currentSlope, candle.ServerTime, candle.State == CandleStates.Finished))

        # Skip until we have enough slope data
        if self._prevSlope == 0:
            self._prevSlope = self._currentSlope
            self._prevSlopeAverage = slopeAverage
            self._prevSlopeStdDev = slopeStdDev
            return

        # Calculate thresholds for slope
        highThreshold = self._prevSlopeAverage + self._prevSlopeStdDev * self.DeviationMultiplier
        lowThreshold = self._prevSlopeAverage - self._prevSlopeStdDev * self.DeviationMultiplier

        # Trading logic:
        # When slope is falling below the lower threshold (mean reversion - slope will rise)
        if self._currentSlope < lowThreshold and self._prevSlope >= lowThreshold and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Hull MA slope fallen below threshold: {0} < {1}. Buying at {2}".format(
                self._currentSlope, lowThreshold, candle.ClosePrice))
        # When slope is rising above the upper threshold (mean reversion - slope will fall)
        elif self._currentSlope > highThreshold and self._prevSlope <= highThreshold and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Hull MA slope risen above threshold: {0} > {1}. Selling at {2}".format(
                self._currentSlope, highThreshold, candle.ClosePrice))
        # Exit positions when slope returns to average
        elif self.Position > 0 and self._currentSlope > self._prevSlopeAverage and self._prevSlope <= self._prevSlopeAverage:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Hull MA slope returned to average from below: {0} > {1}. Closing long position at {2}".format(
                self._currentSlope, self._prevSlopeAverage, candle.ClosePrice))
        elif self.Position < 0 and self._currentSlope < self._prevSlopeAverage and self._prevSlope >= self._prevSlopeAverage:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Hull MA slope returned to average from above: {0} < {1}. Closing short position at {2}".format(
                self._currentSlope, self._prevSlopeAverage, candle.ClosePrice))
        # Dynamic ATR-based stop loss
        elif self._currentAtr > 0:
            stopLevel = candle.ClosePrice - self._currentAtr * self.AtrMultiplier if self.Position > 0 else \
                        candle.ClosePrice + self._currentAtr * self.AtrMultiplier if self.Position < 0 else 0
            if (self.Position > 0 and candle.LowPrice <= stopLevel) or \
               (self.Position < 0 and candle.HighPrice >= stopLevel):
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                    self.LogInfo("ATR Stop Loss triggered for long position: {0} <= {1}. Closing at {2}".format(
                        candle.LowPrice, stopLevel, candle.ClosePrice))
                elif self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                    self.LogInfo("ATR Stop Loss triggered for short position: {0} >= {1}. Closing at {2}".format(
                        candle.HighPrice, stopLevel, candle.ClosePrice))

        # Store current values for next comparison
        self._prevHullMa = self._currentHullMa
        self._prevSlope = self._currentSlope
        self._prevSlopeAverage = slopeAverage
        self._prevSlopeStdDev = slopeStdDev

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return hull_ma_slope_mean_reversion_strategy()
