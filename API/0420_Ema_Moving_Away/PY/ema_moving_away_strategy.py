import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Array
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ema_moving_away_strategy(Strategy):
    """EMA moving away with body size and streak filters."""
    def __init__(self):
        super(ema_moving_away_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_length = self.Param("EmaLength", 55).SetDisplay("EMA Length", "EMA period", "Moving Average")
        self._moving_away_percent = self.Param("MovingAwayPercent", 2.0).SetDisplay("Moving away (%)", "Distance from EMA", "Strategy")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetDisplay("Stop Loss (%)", "Stop loss percentage", "Stop Loss")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(ema_moving_away_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        # TODO: implement strategy logic

    def CreateClone(self):
        return ema_moving_away_strategy()
