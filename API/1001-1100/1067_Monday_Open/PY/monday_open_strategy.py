import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DayOfWeek
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class monday_open_strategy(Strategy):
    def __init__(self):
        super(monday_open_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._trade_opened = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(monday_open_strategy, self).OnReseted()
        self._trade_opened = False

    def OnStarted2(self, time):
        super(monday_open_strategy, self).OnStarted2(time)
        self._trade_opened = False
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        day = candle.OpenTime.DayOfWeek
        if day == DayOfWeek.Monday and not self._trade_opened and self.Position <= 0:
            self.BuyMarket()
            self._trade_opened = True
        elif day == DayOfWeek.Tuesday and self._trade_opened and self.Position > 0:
            self.SellMarket()
            self._trade_opened = False
        elif day != DayOfWeek.Monday and day != DayOfWeek.Tuesday:
            self._trade_opened = False

    def CreateClone(self):
        return monday_open_strategy()
