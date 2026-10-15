import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
class ak47_a1_strategy(Strategy):
    def __init__(self):
        super(ak47_a1_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 5).SetDisplay("Fast SMA", "Lips period", "Indicators")
        self._med_period = self.Param("MedPeriod", 8).SetDisplay("Medium SMA", "Teeth period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 13).SetDisplay("Slow SMA", "Jaw period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_fast = 0.0; self._prev_med = 0.0; self._has_prev = False
    @property
    def fast_period(self): return self._fast_period.Value
    @property
    def med_period(self): return self._med_period.Value
    @property
    def slow_period(self): return self._slow_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(ak47_a1_strategy, self).OnReseted()
        self._prev_fast = 0.0; self._prev_med = 0.0; self._has_prev = False
    def OnStarted2(self, time):
        super(ak47_a1_strategy, self).OnStarted2(time)
        self._has_prev = False
        fast = SimpleMovingAverage(); fast.Length = self.fast_period
        med = SimpleMovingAverage(); med.Length = self.med_period
        slow = SimpleMovingAverage(); slow.Length = self.slow_period
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(fast, med, slow, self.process_candle).Start()
    def process_candle(self, candle, fast, med, slow):
        if candle.State != CandleStates.Finished: return
        f = float(fast); m = float(med); s = float(slow)
        if not self._has_prev: self._prev_fast = f; self._prev_med = m; self._has_prev = True; return
        if self._prev_fast <= self._prev_med and f > m and m > s and self.Position <= 0:
            if self.Position < 0: self.BuyMarket()
            self.BuyMarket()
        elif self._prev_fast >= self._prev_med and f < m and m < s and self.Position >= 0:
            if self.Position > 0: self.SellMarket()
            self.SellMarket()
        self._prev_fast = f; self._prev_med = m
    def CreateClone(self): return ak47_a1_strategy()
