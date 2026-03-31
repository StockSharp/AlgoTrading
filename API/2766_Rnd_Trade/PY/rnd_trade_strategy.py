import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class rnd_trade_strategy(Strategy):
    def __init__(self):
        super(rnd_trade_strategy, self).__init__()
        self._interval_minutes = self.Param("IntervalMinutes", 360).SetGreaterThanZero().SetDisplay("Interval Minutes", "Minutes between trades", "General")

    def OnStarted2(self, time):
        super(rnd_trade_strategy, self).OnStarted2(time)

        tf = DataType.TimeFrame(TimeSpan.FromMinutes(self._interval_minutes.Value))
        sub = self.SubscribeCandles(tf)
        sub.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

        h = int(float(candle.ClosePrice) * 1000) ^ int(candle.TotalVolume)
        if (h & 1) == 0:
            if self.Position <= 0:
                self.BuyMarket()
        else:
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return rnd_trade_strategy()
