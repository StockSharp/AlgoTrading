import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class swetten_strategy(Strategy):
    """Fast/slow SMA crossover (8/34)."""
    def __init__(self):
        super(swetten_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 8).SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 34).SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle type for strategy", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(swetten_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._is_ready = False

    def OnStarted(self, time):
        super(swetten_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._is_ready = False

        fast = SimpleMovingAverage()
        fast.Length = self._fast_period.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._is_ready:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._is_ready = True
            return

        if self._prev_fast <= self._prev_slow and fast_val > slow_val:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast_val < slow_val:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return swetten_strategy()
