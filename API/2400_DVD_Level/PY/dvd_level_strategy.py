import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage as EMA
from StockSharp.Algo.Strategies import Strategy


class dvd_level_strategy(Strategy):
    def __init__(self):
        super(dvd_level_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._prev_ravi = 0.0
        self._has_prev = False

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnStarted(self, time):
        super(dvd_level_strategy, self).OnStarted(time)
        self._prev_ravi = 0.0
        self._has_prev = False
        self._ema_fast = EMA()
        self._ema_fast.Length = 2
        self._ema_slow = EMA()
        self._ema_slow.Length = 24
        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._ema_fast, self._ema_slow, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, ema_fast, ema_slow):
        if candle.State != CandleStates.Finished:
            return
        ef = float(ema_fast)
        es = float(ema_slow)
        if es == 0:
            return
        ravi = (ef - es) / es * 100.0
        if not self._has_prev:
            self._prev_ravi = ravi
            self._has_prev = True
            return
        cross_above = self._prev_ravi <= 0 and ravi > 0
        cross_below = self._prev_ravi >= 0 and ravi < 0
        if cross_below and self.Position <= 0:
            self.BuyMarket()
        elif cross_above and self.Position >= 0:
            self.SellMarket()
        self._prev_ravi = ravi

    def OnReseted(self):
        super(dvd_level_strategy, self).OnReseted()
        self._prev_ravi = 0.0
        self._has_prev = False

    def CreateClone(self):
        return dvd_level_strategy()
