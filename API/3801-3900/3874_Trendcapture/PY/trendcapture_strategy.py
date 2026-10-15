import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
class trendcapture_strategy(Strategy):
    def __init__(self):
        super(trendcapture_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._adx_period = self.Param("AdxPeriod", 14).SetDisplay("ADX Period", "ADX lookback", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 30.0).SetDisplay("ADX Threshold", "Minimum ADX for trending", "Levels")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_fast = 0.0; self._prev_slow = 0.0; self._has_prev = False
    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(trendcapture_strategy, self).OnReseted()
        self._prev_fast = 0.0; self._prev_slow = 0.0; self._has_prev = False
    def OnStarted2(self, time):
        super(trendcapture_strategy, self).OnStarted2(time)
        self._has_prev = False
        fast = ExponentialMovingAverage(); fast.Length = self.ema_period
        slow = ExponentialMovingAverage(); slow.Length = self.ema_period * 3
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(fast, slow, self.process_candle).Start()
    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished: return
        f = float(fast); s = float(slow)
        if not self._has_prev: self._prev_fast = f; self._prev_slow = s; self._has_prev = True; return
        if self._prev_fast <= self._prev_slow and f > s and self.Position <= 0:
            if self.Position < 0: self.BuyMarket()
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and f < s and self.Position >= 0:
            if self.Position > 0: self.SellMarket()
            self.SellMarket()
        self._prev_fast = f; self._prev_slow = s
    def CreateClone(self): return trendcapture_strategy()
