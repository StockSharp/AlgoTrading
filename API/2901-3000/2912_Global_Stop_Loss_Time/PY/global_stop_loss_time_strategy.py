import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class global_stop_loss_time_strategy(Strategy):
    def __init__(self):
        super(global_stop_loss_time_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._ema_fast_length = self.Param("EmaFastLength", 8) \
            .SetDisplay("EMA Fast", "Fast EMA period", "Indicators")
        self._ema_slow_length = self.Param("EmaSlowLength", 21) \
            .SetDisplay("EMA Slow", "Slow EMA period", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def EmaFastLength(self):
        return self._ema_fast_length.Value

    @property
    def EmaSlowLength(self):
        return self._ema_slow_length.Value

    def OnReseted(self):
        super(global_stop_loss_time_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(global_stop_loss_time_strategy, self).OnStarted2(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self.EmaFastLength
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self.EmaSlowLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema_fast, ema_slow, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_fast)
            self.DrawIndicator(area, ema_slow)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fv = float(fast_value)
        sv = float(slow_value)
        if not self._has_prev:
            self._prev_fast = fv
            self._prev_slow = sv
            self._has_prev = True
            return
        if self._prev_fast <= self._prev_slow and fv > sv and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fv < sv and self.Position >= 0:
            self.SellMarket()
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return global_stop_loss_time_strategy()
