import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class multi_timeframe_ema_alignment_strategy(Strategy):
    def __init__(self):
        super(multi_timeframe_ema_alignment_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10).SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30).SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._trend_period = self.Param("TrendPeriod", 100).SetDisplay("Trend EMA", "Trend EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_fast = 0.0; self._prev_slow = 0.0; self._has_prev = False
    @property
    def fast_period(self): return self._fast_period.Value
    @property
    def slow_period(self): return self._slow_period.Value
    @property
    def trend_period(self): return self._trend_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(multi_timeframe_ema_alignment_strategy, self).OnReseted()
        self._prev_fast = 0.0; self._prev_slow = 0.0; self._has_prev = False
    def OnStarted(self, time):
        super(multi_timeframe_ema_alignment_strategy, self).OnStarted(time)
        self._has_prev = False
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        trend = ExponentialMovingAverage()
        trend.Length = self.trend_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, trend, self.process_candle).Start()
    def process_candle(self, candle, fast, slow, trend):
        if candle.State != CandleStates.Finished: return
        close = float(candle.ClosePrice)
        f = float(fast); s = float(slow); t = float(trend)
        if not self._has_prev:
            self._prev_fast = f; self._prev_slow = s; self._has_prev = True; return
        if self._prev_fast <= self._prev_slow and f > s and close > t and self.Position <= 0:
            if self.Position < 0: self.BuyMarket()
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and f < s and close < t and self.Position >= 0:
            if self.Position > 0: self.SellMarket()
            self.SellMarket()
        self._prev_fast = f; self._prev_slow = s
    def CreateClone(self): return multi_timeframe_ema_alignment_strategy()
