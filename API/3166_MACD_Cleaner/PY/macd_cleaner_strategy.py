import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class macd_cleaner_strategy(Strategy):
    """
    MACD Cleaner: fast/slow EMA crossover with cooldown.
    """

    def __init__(self):
        super(macd_cleaner_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12).SetDisplay("Fast Period", "Fast EMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 50).SetDisplay("Slow Period", "Slow EMA period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_cleaner_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(macd_cleaner_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        slow = float(slow_val)
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return
        if self._prev_fast <= self._prev_slow and fast > slow and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 100
        elif self._prev_fast >= self._prev_slow and fast < slow and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 100
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return macd_cleaner_strategy()
