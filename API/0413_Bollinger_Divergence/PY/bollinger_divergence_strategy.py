import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class bollinger_divergence_strategy(Strategy):
    """Bollinger Bands divergence strategy."""
    def __init__(self):
        super(bollinger_divergence_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_length = self.Param("BBLength", 20).SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 2.0).SetDisplay("BB StdDev", "StdDev multiplier", "Bollinger Bands")
        self._candle_percent = self.Param("CandlePercent", 30.0).SetDisplay("Candle %", "Candle percentage", "Strategy")
        self._take_profit = self.Param("TakeProfit", 5.0).SetDisplay("Take Profit %", "Take profit percentage", "Strategy")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(bollinger_divergence_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        # TODO: implement strategy logic

    def CreateClone(self):
        return bollinger_divergence_strategy()
