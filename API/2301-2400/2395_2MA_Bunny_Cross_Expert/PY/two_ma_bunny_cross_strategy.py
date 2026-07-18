import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage as SMA
from StockSharp.Algo.Strategies import Strategy


class two_ma_bunny_cross_strategy(Strategy):
    def __init__(self):
        super(two_ma_bunny_cross_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._fast_length = self.Param("FastLength", 5)
        self._slow_length = self.Param("SlowLength", 20)
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def FastLength(self): return self._fast_length.Value
    @FastLength.setter
    def FastLength(self, v): self._fast_length.Value = v
    @property
    def SlowLength(self): return self._slow_length.Value
    @SlowLength.setter
    def SlowLength(self, v): self._slow_length.Value = v

    def OnStarted2(self, time):
        super(two_ma_bunny_cross_strategy, self).OnStarted2(time)
        fast_sma = SMA()
        fast_sma.Length = self.FastLength
        slow_sma = SMA()
        slow_sma.Length = self.SlowLength
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_sma, slow_sma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished: return
        f = float(fast)
        s = float(slow)
        close = float(candle.ClosePrice)
        if f > s and close > s and self.Position <= 0:
            self.BuyMarket()
        elif f < s and close < s and self.Position >= 0:
            self.SellMarket()
        self._prev_fast = f
        self._prev_slow = s

    def OnReseted(self):
        super(two_ma_bunny_cross_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def CreateClone(self):
        return two_ma_bunny_cross_strategy()
