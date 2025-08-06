import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class volatility_risk_premium_strategy(Strategy):
    """
    Volatility risk‑premium (options) – placeholder
    """

    def __init__(self):
        super(volatility_risk_premium_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1440))             .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(volatility_risk_premium_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(volatility_risk_premium_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        # TODO: implement strategy logic

    def CreateClone(self):
        return volatility_risk_premium_strategy()
