import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_cross_strategy(Strategy):
    """
    MA Cross strategy: EMA crossover.
    Buys when fast crosses above slow, sells on cross below.
    """

    def __init__(self):
        super(ma_cross_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10).SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30).SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_cross_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(ma_cross_strategy, self).OnStarted2(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        f = float(fast_val)
        s = float(slow_val)
        if self._has_prev:
            if self._prev_fast <= self._prev_slow and f > s and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_fast >= self._prev_slow and f < s and self.Position >= 0:
                self.SellMarket()
        else:
            if not self.IsFormedAndOnlineAndAllowTrading():
                self._prev_fast = f
                self._prev_slow = s
                self._has_prev = True
                return
            if f > s and self.Position <= 0:
                self.BuyMarket()
            elif f < s and self.Position >= 0:
                self.SellMarket()
        self._prev_fast = f
        self._prev_slow = s
        self._has_prev = True

    def CreateClone(self):
        return ma_cross_strategy()
