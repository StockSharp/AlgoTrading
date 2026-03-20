import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class weighted_standard_deviation_strategy(Strategy):
    def __init__(self):
        super(weighted_standard_deviation_strategy, self).__init__()
        self._fast_ema_period = self.Param("FastEmaPeriod", 120) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 450) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(weighted_standard_deviation_strategy, self).OnReseted()
        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0

    def OnStarted(self, time):
        super(weighted_standard_deviation_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_period.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast_v = float(fast_val)
        slow_v = float(slow_val)
        if self._prev_fast_ema == 0 or self._prev_slow_ema == 0:
            self._prev_fast_ema = fast_v
            self._prev_slow_ema = slow_v
            return
        if self._prev_fast_ema <= self._prev_slow_ema and fast_v > slow_v and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast_ema >= self._prev_slow_ema and fast_v < slow_v and self.Position >= 0:
            self.SellMarket()
        self._prev_fast_ema = fast_v
        self._prev_slow_ema = slow_v

    def CreateClone(self):
        return weighted_standard_deviation_strategy()
