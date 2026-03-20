import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
class ma2_cci_classic_strategy(Strategy):
    def __init__(self):
        super(ma2_cci_classic_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12).SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 26).SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._cci_period = self.Param("CciPeriod", 14).SetDisplay("CCI Period", "CCI lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_fast = 0.0; self._prev_slow = 0.0; self._has_prev = False
    @property
    def fast_period(self): return self._fast_period.Value
    @property
    def slow_period(self): return self._slow_period.Value
    @property
    def cci_period(self): return self._cci_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(ma2_cci_classic_strategy, self).OnReseted()
        self._prev_fast = 0.0; self._prev_slow = 0.0; self._has_prev = False
    def OnStarted(self, time):
        super(ma2_cci_classic_strategy, self).OnStarted(time)
        self._has_prev = False
        fast = SimpleMovingAverage(); fast.Length = self.fast_period
        slow = SimpleMovingAverage(); slow.Length = self.slow_period
        cci = CommodityChannelIndex(); cci.Length = self.cci_period
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(fast, slow, cci, self.process_candle).Start()
    def process_candle(self, candle, fast, slow, cci):
        if candle.State != CandleStates.Finished: return
        f = float(fast); s = float(slow); c = float(cci)
        if not self._has_prev: self._prev_fast = f; self._prev_slow = s; self._has_prev = True; return
        if self._prev_fast <= self._prev_slow and f > s and c > 0 and self.Position <= 0:
            if self.Position < 0: self.BuyMarket()
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and f < s and c < 0 and self.Position >= 0:
            if self.Position > 0: self.SellMarket()
            self.SellMarket()
        self._prev_fast = f; self._prev_slow = s
    def CreateClone(self): return ma2_cci_classic_strategy()
