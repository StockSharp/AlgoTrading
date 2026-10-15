import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class gselector_pattern_probability_strategy(Strategy):
    def __init__(self):
        super(gselector_pattern_probability_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10).SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30).SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._overbought = self.Param("Overbought", 75.0).SetDisplay("Overbought", "RSI overbought level", "Levels")
        self._oversold = self.Param("Oversold", 25.0).SetDisplay("Oversold", "RSI oversold level", "Levels")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
    @property
    def fast_period(self): return self._fast_period.Value
    @property
    def slow_period(self): return self._slow_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(gselector_pattern_probability_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
    def OnStarted2(self, time):
        super(gselector_pattern_probability_strategy, self).OnStarted2(time)
        self._has_prev = False
        fast = SimpleMovingAverage()
        fast.Length = self.fast_period
        slow = SimpleMovingAverage()
        slow.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.process_candle).Start()
    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast)
        slow_val = float(slow)
        if not self._has_prev:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._has_prev = True
            return
        if self._prev_fast <= self._prev_slow and fast_val > slow_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()

        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_fast = fast_val
        self._prev_slow = slow_val
    def CreateClone(self):
        return gselector_pattern_probability_strategy()
