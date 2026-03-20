import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
class rsi_cci_williams_r_strategy(Strategy):
    def __init__(self):
        super(rsi_cci_williams_r_strategy, self).__init__()
        self._fast_period = self.Param("FastEmaPeriod", 120).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowEmaPeriod", 450).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(rsi_cci_williams_r_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0

    def OnStarted(self, time):
        super(rsi_cci_williams_r_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_slow = 0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_period.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_ema, slow_ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        if self._prev_fast <= self._prev_slow and fast_val > slow_val and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_fast >= self._prev_slow and fast_val < slow_val and self.Position >= 0:
            self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return rsi_cci_williams_r_strategy()
