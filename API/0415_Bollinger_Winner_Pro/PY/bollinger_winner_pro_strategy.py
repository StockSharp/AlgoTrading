import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class bollinger_winner_pro_strategy(Strategy):
    """Advanced Bollinger Band system with multiple filters."""
    def __init__(self):
        super(bollinger_winner_pro_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._use_rsi = self.Param("UseRSI", True).SetDisplay("Use RSI", "Enable RSI filter", "RSI")
        self._use_aroon = self.Param("UseAroon", False).SetDisplay("Use Aroon", "Enable Aroon filter", "Aroon")
        self._use_ma = self.Param("UseMA", True).SetDisplay("Use MA", "Enable moving average filter", "Moving Average")
        self._use_sl = self.Param("UseSL", True).SetDisplay("Use Stop Loss", "Enable stop loss", "Stop Loss")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(bollinger_winner_pro_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        # TODO: implement strategy logic

    def CreateClone(self):
        return bollinger_winner_pro_strategy()
