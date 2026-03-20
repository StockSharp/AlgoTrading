import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class simple_ema_crossover_strategy(Strategy):
    def __init__(self):
        super(simple_ema_crossover_strategy, self).__init__()
        self._periods = self.Param("Periods", TimeSpan.FromHours(4)) \
            .SetDisplay("EMA Period", "Period for the fast EMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for analysis", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def periods(self):
        return self._periods.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(simple_ema_crossover_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(simple_ema_crossover_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.periods
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.periods
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        if self._has_prev:
            cross_up = self._prev_fast < self._prev_slow and fast > slow
            cross_down = self._prev_fast > self._prev_slow and fast < slow
            if cross_up and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and self.Position >= 0:
                self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow
        self._has_prev = True

    def CreateClone(self):
        return simple_ema_crossover_strategy()
