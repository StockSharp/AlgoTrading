import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Array
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class double_supertrend_strategy(Strategy):
    """Two supertrend-like moving averages with ATR bands."""
    def __init__(self):
        super(double_supertrend_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._atr_period1 = self.Param("ATRPeriod1", 10).SetDisplay("MA1 Period", "First moving average period", "Moving Averages")
        self._factor1 = self.Param("Factor1", 3.0).SetDisplay("MA1 Factor", "First multiplier", "Moving Averages")
        self._atr_period2 = self.Param("ATRPeriod2", 20).SetDisplay("MA2 Period", "Second moving average period", "Moving Averages")
        self._factor2 = self.Param("Factor2", 5.0).SetDisplay("MA2 Factor", "Second multiplier", "Moving Averages")
        self._direction = self.Param("Direction", "Long").SetDisplay("Direction", "Trading direction", "Strategy")
        self._tp_type = self.Param("TPType", "Supertrend").SetDisplay("TP Type", "Take profit type", "Take Profit")
        self._tp_percent = self.Param("TPPercent", 1.5).SetDisplay("TP Percent", "Take profit percentage", "Take Profit")
        self._sl_percent = self.Param("SLPercent", 10.0).SetDisplay("Stop Loss %", "Stop loss percentage", "Stop Loss")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(double_supertrend_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        # TODO: implement strategy logic

    def CreateClone(self):
        return double_supertrend_strategy()
