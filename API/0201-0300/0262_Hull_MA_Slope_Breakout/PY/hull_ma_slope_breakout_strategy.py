import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class hull_ma_slope_breakout_strategy(Strategy):
    """
    Hull Moving Average slope breakout.
    Enters in the direction of the slope when it exceeds its average by a standard deviation multiplier
    and exits when the slope returns to its average.
    """

    def __init__(self):
        super(hull_ma_slope_breakout_strategy, self).__init__()

        self._hull_length = self.Param("HullLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Hull MA Length", "Period for Hull Moving Average", "Indicator Parameters") \
            .SetOptimize(5, 20, 1)
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics calculation", "Strategy Parameters") \
            .SetOptimize(10, 50, 5)
        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters") \
            .SetOptimize(1.0, 3.0, 0.5)
        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Protective stop-loss", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._slope_average = None
        self._slope_std_dev = None
        self._prev_hull = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(hull_ma_slope_breakout_strategy, self).OnReseted()
        self._slope_average = None
        self._slope_std_dev = None
        self._prev_hull = None

    def OnStarted2(self, time):
        super(hull_ma_slope_breakout_strategy, self).OnStarted2(time)

        hull = HullMovingAverage()
        hull.Length = self._hull_length.Value
        self._slope_average = SimpleMovingAverage()
        self._slope_average.Length = self._lookback_period.Value
        self._slope_std_dev = StandardDeviation()
        self._slope_std_dev.Length = self._lookback_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(hull, self._process_candle).Start()

        self.StartProtection(None, self._stop_loss.Value)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, hull_value):
        if candle.State != CandleStates.Finished:
            return

        hull_value = float(hull_value)

        if self._prev_hull is None:
            self._prev_hull = hull_value
            return

        slope = hull_value - self._prev_hull
        self._prev_hull = hull_value

        avg_slope = float(process_float(self._slope_average, slope, candle.ServerTime, True))
        std_slope = float(process_float(self._slope_std_dev, slope, candle.ServerTime, True))

        if not self._slope_average.IsFormed or not self._slope_std_dev.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        k = float(self._deviation_multiplier.Value)

        if slope > avg_slope + k * std_slope and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif slope < avg_slope - k * std_slope and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and slope < avg_slope:
            self.SellMarket(self.Position)
        elif self.Position < 0 and slope > avg_slope:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return hull_ma_slope_breakout_strategy()
