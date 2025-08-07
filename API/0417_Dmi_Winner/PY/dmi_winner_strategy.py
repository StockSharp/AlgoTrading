import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class dmi_winner_strategy(Strategy):
    """Directional Movement Index based trend follower."""
    def __init__(self):
        super(dmi_winner_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._di_length = self.Param("DILength", 14).SetDisplay("DI Length", "Directional Indicator period", "DMI")
        self._key_level = self.Param("KeyLevel", 23.0).SetDisplay("Key Level", "ADX key level", "DMI")
        self._use_ma = self.Param("UseMA", True).SetDisplay("Use MA", "Enable moving average filter", "Moving Average")
        self._use_sl = self.Param("UseSL", False).SetDisplay("Use Stop Loss", "Enable stop loss", "Stop Loss")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(dmi_winner_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        # TODO: implement strategy logic

    def CreateClone(self):
        return dmi_winner_strategy()
