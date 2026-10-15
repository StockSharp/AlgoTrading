import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_pullback_strategy(Strategy):
    def __init__(self):
        super(ema_pullback_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 8) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_close = 0.0
        self._has_prev = False
        self._bullish_cross = False
        self._bearish_cross = False

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_pullback_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_close = 0.0
        self._has_prev = False
        self._bullish_cross = False
        self._bearish_cross = False

    def OnStarted2(self, time):
        super(ema_pullback_strategy, self).OnStarted2(time)
        self._has_prev = False
        self._bullish_cross = False
        self._bearish_cross = False
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.process_candle).Start()

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast)
        slow_val = float(slow)
        close = float(candle.ClosePrice)
        if not self._has_prev:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._prev_close = close
            self._has_prev = True
            return
        if self._prev_fast <= self._prev_slow and fast_val > slow_val:
            self._bullish_cross = True
            self._bearish_cross = False
        elif self._prev_fast >= self._prev_slow and fast_val < slow_val:
            self._bearish_cross = True
            self._bullish_cross = False
        if self._bullish_cross and fast_val > slow_val and self._prev_close > self._prev_fast and close <= fast_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._bullish_cross = False
        elif self._bearish_cross and fast_val < slow_val and self._prev_close < self._prev_fast and close >= fast_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._bearish_cross = False
        elif self.Position > 0 and fast_val < slow_val:
            self.SellMarket()
        elif self.Position < 0 and fast_val > slow_val:
            self.BuyMarket()
        self._prev_fast = fast_val
        self._prev_slow = slow_val
        self._prev_close = close

    def CreateClone(self):
        return ema_pullback_strategy()
