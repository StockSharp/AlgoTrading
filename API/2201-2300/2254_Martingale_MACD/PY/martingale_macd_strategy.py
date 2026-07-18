import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class martingale_macd_strategy(Strategy):
    def __init__(self):
        super(martingale_macd_strategy, self).__init__()
        self._macd1_fast = self.Param("Macd1Fast", 5) \
            .SetDisplay("MACD1 Fast", "Fast EMA for first MACD", "Indicators")
        self._macd1_slow = self.Param("Macd1Slow", 20) \
            .SetDisplay("MACD1 Slow", "Slow EMA for first MACD", "Indicators")
        self._macd2_fast = self.Param("Macd2Fast", 10) \
            .SetDisplay("MACD2 Fast", "Fast EMA for second MACD", "Indicators")
        self._macd2_slow = self.Param("Macd2Slow", 15) \
            .SetDisplay("MACD2 Slow", "Slow EMA for second MACD", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._macd1_prev1 = None
        self._macd1_prev2 = None
        self._macd2_prev = None

    @property
    def macd1_fast(self):
        return self._macd1_fast.Value

    @property
    def macd1_slow(self):
        return self._macd1_slow.Value

    @property
    def macd2_fast(self):
        return self._macd2_fast.Value

    @property
    def macd2_slow(self):
        return self._macd2_slow.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(martingale_macd_strategy, self).OnReseted()
        self._macd1_prev1 = None
        self._macd1_prev2 = None
        self._macd2_prev = None

    def OnStarted2(self, time):
        super(martingale_macd_strategy, self).OnStarted2(time)
        self._macd1_prev1 = None
        self._macd1_prev2 = None
        self._macd2_prev = None
        macd1 = MovingAverageConvergenceDivergence()
        macd1.ShortMa.Length = self.macd1_fast
        macd1.LongMa.Length = self.macd1_slow
        macd2 = MovingAverageConvergenceDivergence()
        macd2.ShortMa.Length = self.macd2_fast
        macd2.LongMa.Length = self.macd2_slow
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(macd1, macd2, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd1)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, macd1_val, macd2_val):
        if candle.State != CandleStates.Finished:
            return
        macd1_val = float(macd1_val)
        macd2_val = float(macd2_val)
        if self._macd1_prev1 is not None and self._macd1_prev2 is not None and self._macd2_prev is not None:
            t0 = macd1_val
            t1 = self._macd1_prev1
            t2 = self._macd1_prev2
            k0 = macd2_val
            k1 = self._macd2_prev
            if t0 > t1 and t1 < t2 and k1 > k0 and self.Position <= 0:
                self.BuyMarket()
            elif t0 < t1 and t1 > t2 and k1 < k0 and self.Position >= 0:
                self.SellMarket()
        self._macd1_prev2 = self._macd1_prev1
        self._macd1_prev1 = macd1_val
        self._macd2_prev = macd2_val

    def CreateClone(self):
        return martingale_macd_strategy()
