import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class big_mover_catcher_strategy(Strategy):
    def __init__(self):
        super(big_mover_catcher_strategy, self).__init__()
        self._fast_ema_period = self.Param("FastEmaPeriod", 120) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 450) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")
        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0

    @property
    def fast_ema_period(self):
        return self._fast_ema_period.Value
    @fast_ema_period.setter
    def fast_ema_period(self, value):
        self._fast_ema_period.Value = value

    @property
    def slow_ema_period(self):
        return self._slow_ema_period.Value
    @slow_ema_period.setter
    def slow_ema_period(self, value):
        self._slow_ema_period.Value = value

    @property
    def candle_type(self):
        return self._candle_type.Value
    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(big_mover_catcher_strategy, self).OnReseted()
        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0

    def OnStarted(self, time):
        super(big_mover_catcher_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_ema_period
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_ema_value, slow_ema_value):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_fast_ema == 0 or self._prev_slow_ema == 0:
            self._prev_fast_ema = float(fast_ema_value)
            self._prev_slow_ema = float(slow_ema_value)
            return

        if self._prev_fast_ema <= self._prev_slow_ema and fast_ema_value > slow_ema_value and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast_ema >= self._prev_slow_ema and fast_ema_value < slow_ema_value and self.Position >= 0:
            self.SellMarket()

        self._prev_fast_ema = float(fast_ema_value)
        self._prev_slow_ema = float(slow_ema_value)

    def CreateClone(self):
        return big_mover_catcher_strategy()
