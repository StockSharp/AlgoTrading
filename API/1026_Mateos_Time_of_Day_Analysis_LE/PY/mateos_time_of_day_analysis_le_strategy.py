import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mateos_time_of_day_analysis_le_strategy(Strategy):
    def __init__(self):
        super(mateos_time_of_day_analysis_le_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._start_hour = self.Param("StartHour", 9) \
            .SetDisplay("Start Hour", "Hour to enter", "General")
        self._end_hour = self.Param("EndHour", 16) \
            .SetDisplay("End Hour", "Hour to exit", "General")
        self._entry_date = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mateos_time_of_day_analysis_le_strategy, self).OnReseted()
        self._entry_date = None

    def OnStarted(self, time):
        super(mateos_time_of_day_analysis_le_strategy, self).OnStarted(time)
        self._entry_date = None
        dummy1 = ExponentialMovingAverage()
        dummy1.Length = 10
        dummy2 = ExponentialMovingAverage()
        dummy2.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(dummy1, dummy2, self.OnProcess).Start()

    def OnProcess(self, candle, d1, d2):
        if candle.State != CandleStates.Finished:
            return
        hour = candle.ServerTime.Hour
        date = candle.ServerTime.Date
        start = self._start_hour.Value
        end = self._end_hour.Value
        if hour >= start and hour < end:
            if self.Position <= 0 and self._entry_date != date:
                self.BuyMarket()
                self._entry_date = date
        elif hour >= end or hour < start:
            if self.Position > 0:
                self.SellMarket()
                self._entry_date = None

    def CreateClone(self):
        return mateos_time_of_day_analysis_le_strategy()
