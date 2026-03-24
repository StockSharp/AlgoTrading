import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class break_the_range_bound_strategy(Strategy):
    def __init__(self):
        super(break_the_range_bound_strategy, self).__init__()
        self._fast_sma = self.Param("FastSma", 10) \
            .SetDisplay("Fast SMA", "Fast moving average period", "Parameters")
        self._slow_sma = self.Param("SlowSma", 50) \
            .SetDisplay("Slow SMA", "Slow moving average period", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def fast_sma(self):
        return self._fast_sma.Value

    @property
    def slow_sma(self):
        return self._slow_sma.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(break_the_range_bound_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(break_the_range_bound_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_sma
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_sma
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        if not self._has_prev:
            self._prev_fast = fast_value
            self._prev_slow = slow_value
            self._prev_close = close
            self._has_prev = True
            return
        # Cross above slow SMA => buy breakout
        if self._prev_close <= self._prev_slow and close > slow_value and fast_value > slow_value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Cross below slow SMA => sell breakout
        elif self._prev_close >= self._prev_slow and close < slow_value and fast_value < slow_value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_fast = fast_value
        self._prev_slow = slow_value
        self._prev_close = close

    def CreateClone(self):
        return break_the_range_bound_strategy()
