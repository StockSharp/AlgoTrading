import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Momentum
from StockSharp.Algo.Strategies import Strategy

class glam_trader_simple_strategy(Strategy):
    def __init__(self):
        super(glam_trader_simple_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 8).SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 21).SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 14).SetDisplay("Momentum Period", "Momentum lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
    @property
    def fast_period(self): return self._fast_period.Value
    @property
    def slow_period(self): return self._slow_period.Value
    @property
    def momentum_period(self): return self._momentum_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(glam_trader_simple_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
    def OnStarted2(self, time):
        super(glam_trader_simple_strategy, self).OnStarted2(time)
        self._has_prev = False
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        mom = Momentum()
        mom.Length = self.momentum_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, mom, self.process_candle).Start()
    def process_candle(self, candle, fast, slow, mom):
        if candle.State != CandleStates.Finished:
            return
        fast_val = float(fast)
        slow_val = float(slow)
        mom_val = float(mom)
        if not self._has_prev:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._has_prev = True
            return
        if self._prev_fast <= self._prev_slow and fast_val > slow_val and mom_val > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and mom_val < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_fast = fast_val
        self._prev_slow = slow_val
    def CreateClone(self):
        return glam_trader_simple_strategy()
