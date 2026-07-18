import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class forex_line_strategy(Strategy):
    def __init__(self):
        super(forex_line_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10).SetDisplay("Fast WMA Length", "Fast line period", "Parameters")
        self._slow_length = self.Param("SlowLength", 30).SetDisplay("Slow WMA Length", "Slow line period", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles to analyze", "General")
        self._prev_fast = None
        self._prev_slow = None
    @property
    def fast_length(self): return self._fast_length.Value
    @property
    def slow_length(self): return self._slow_length.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(forex_line_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None
    def OnStarted2(self, time):
        super(forex_line_strategy, self).OnStarted2(time)
        fast = WeightedMovingAverage()
        fast.Length = self.fast_length
        slow = WeightedMovingAverage()
        slow.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)
    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished: return
        f = float(fast)
        s = float(slow)
        if self._prev_fast is not None and self._prev_slow is not None:
            if self._prev_fast <= self._prev_slow and f > s and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_fast >= self._prev_slow and f < s and self.Position >= 0:
                self.SellMarket()
        self._prev_fast = f
        self._prev_slow = s
    def CreateClone(self): return forex_line_strategy()
