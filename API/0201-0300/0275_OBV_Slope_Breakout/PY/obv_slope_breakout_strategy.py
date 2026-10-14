import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from collections import deque
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import OnBalanceVolume, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class obv_slope_breakout_strategy(Strategy):
    """
    On-Balance Volume slope breakout.
    The slope is the OBV change over SlopeLength bars. Enters in the direction of the slope
    when it exceeds its average by a standard deviation multiplier and exits when it returns to the average.
    """

    def __init__(self):
        super(obv_slope_breakout_strategy, self).__init__()

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics", "Strategy")
        self._slope_length = self.Param("SlopeLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Length", "Bars used to measure the OBV slope", "Indicators")
        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Standard deviation multiplier for breakout", "Strategy")
        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._obv_history = deque()
        self._slope_average = None
        self._slope_std_dev = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(obv_slope_breakout_strategy, self).OnReseted()
        self._obv_history.clear()
        self._slope_average = None
        self._slope_std_dev = None

    def OnStarted2(self, time):
        super(obv_slope_breakout_strategy, self).OnStarted2(time)

        obv = OnBalanceVolume()
        self._slope_average = SimpleMovingAverage()
        self._slope_average.Length = self._lookback_period.Value
        self._slope_std_dev = StandardDeviation()
        self._slope_std_dev.Length = self._lookback_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(obv, self._process_candle).Start()

        stop = float(self._stop_loss.Value)
        self.StartProtection(None, Unit(stop, UnitTypes.Percent) if stop > 0 else None)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

            obv_area = self.CreateChartArea()
            if obv_area is not None:
                self.DrawIndicator(obv_area, obv)

    def _process_candle(self, candle, obv_value):
        if candle.State != CandleStates.Finished:
            return

        obv_value = float(obv_value)
        self._obv_history.append(obv_value)

        if len(self._obv_history) <= self._slope_length.Value:
            return

        slope = obv_value - self._obv_history.popleft()
        avg_slope = float(process_float(self._slope_average, slope, candle.ServerTime, True))
        std_slope = float(process_float(self._slope_std_dev, slope, candle.ServerTime, True))

        if not self._slope_average.IsFormed or not self._slope_std_dev.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        k = float(self._multiplier.Value)

        if slope > avg_slope + k * std_slope and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif slope < avg_slope - k * std_slope and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and slope < avg_slope:
            self.SellMarket(self.Position)
        elif self.Position < 0 and slope > avg_slope:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return obv_slope_breakout_strategy()
