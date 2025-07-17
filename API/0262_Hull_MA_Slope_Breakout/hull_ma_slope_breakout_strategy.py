import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, ICandleMessage, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class hull_ma_slope_breakout_strategy(Strategy):
    """Strategy based on Hull Moving Average Slope breakout
    Enters positions when the slope of Hull MA exceeds average slope plus a multiple of standard deviation
    """

    def __init__(self):
        super(hull_ma_slope_breakout_strategy, self).__init__()

        # Constructor
        self._hull_length = self.Param("HullLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Hull MA Length", "Period for Hull Moving Average", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Absolute)) \
            .SetDisplay("Stop Loss", "Stop loss value in ATRs", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._hull_ma = None
        self._atr = None
        self._prev_hull_value = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        self._slopes = []
        self._current_index = 0
        self._is_initialized = False

    @property
    def HullLength(self):
        """Hull Moving Average length"""
        return self._hull_length.Value

    @HullLength.setter
    def HullLength(self, value):
        self._hull_length.Value = value

    @property
    def LookbackPeriod(self):
        """Lookback period for slope statistics calculation"""
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def DeviationMultiplier(self):
        """Standard deviation multiplier for breakout detection"""
        return self._deviation_multiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def CandleType(self):
        """Candle type"""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLoss(self):
        """Stop loss value"""
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        self._hull_ma = HullMovingAverage()
        self._hull_ma.Length = self.HullLength
        self._atr = AverageTrueRange()
        self._atr.Length = 14

        self._prev_hull_value = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        self._slopes = [0.0] * self.LookbackPeriod
        self._current_index = 0
        self._is_initialized = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._hull_ma, self._atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._hull_ma)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=None,
            stopLoss=self.StopLoss,
            isStopTrailing=False
        )
        super(hull_ma_slope_breakout_strategy, self).OnStarted(time)

    def ProcessCandle(self, candle, hullValue, atrValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if indicator is formed
        if not self._hull_ma.IsFormed:
            return

        current_hull_value = hullValue

        if not self._is_initialized:
            self._prev_hull_value = current_hull_value
            self._is_initialized = True
            return

        self._current_slope = current_hull_value - self._prev_hull_value

        self._slopes[self._current_index] = self._current_slope
        self._current_index = (self._current_index + 1) % self.LookbackPeriod

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_hull_value = current_hull_value
            return

        self.CalculateStatistics()

        if Math.Abs(self._avg_slope) > 0:
            if self._current_slope > 0 and \
               self._current_slope > self._avg_slope + self.DeviationMultiplier * self._std_dev_slope and \
               self.Position <= 0:
                self.CancelActiveOrders()
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)
                self.LogInfo("Long signal: Slope {0} > Avg {1} + {2}*StdDev {3}".format(
                    self._current_slope, self._avg_slope, self.DeviationMultiplier, self._std_dev_slope))
            elif self._current_slope < 0 and \
                 self._current_slope < self._avg_slope - self.DeviationMultiplier * self._std_dev_slope and \
                 self.Position >= 0:
                self.CancelActiveOrders()
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)
                self.LogInfo("Short signal: Slope {0} < Avg {1} - {2}*StdDev {3}".format(
                    self._current_slope, self._avg_slope, self.DeviationMultiplier, self._std_dev_slope))

            if self.Position > 0 and self._current_slope < self._avg_slope:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit long: Slope {0} < Avg {1}".format(self._current_slope, self._avg_slope))
            elif self.Position < 0 and self._current_slope > self._avg_slope:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Slope {0} > Avg {1}".format(self._current_slope, self._avg_slope))

        self._prev_hull_value = current_hull_value

    def CalculateStatistics(self):
        self._avg_slope = 0.0
        sum_squared_diffs = 0.0

        for i in range(self.LookbackPeriod):
            self._avg_slope += self._slopes[i]
        self._avg_slope /= self.LookbackPeriod

        for i in range(self.LookbackPeriod):
            diff = self._slopes[i] - self._avg_slope
            sum_squared_diffs += diff * diff

        self._std_dev_slope = Math.Sqrt(sum_squared_diffs / self.LookbackPeriod)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hull_ma_slope_breakout_strategy()
