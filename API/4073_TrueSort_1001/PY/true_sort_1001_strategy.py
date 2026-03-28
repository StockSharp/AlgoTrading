import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class true_sort_1001_strategy(Strategy):
    """Trend-following: strict SMA alignment (fast>mid>slow) + rising ATR for entries."""
    def __init__(self):
        super(true_sort_1001_strategy, self).__init__()
        self._fast_len = self.Param("FastLength", 10).SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._mid_len = self.Param("MidLength", 50).SetDisplay("Mid SMA", "Medium SMA period", "Indicators")
        self._slow_len = self.Param("SlowLength", 200).SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._atr_len = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(true_sort_1001_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_mid = 0
        self._prev_slow = 0
        self._prev_atr = 0

    def OnStarted(self, time):
        super(true_sort_1001_strategy, self).OnStarted(time)
        self._prev_fast = 0
        self._prev_mid = 0
        self._prev_slow = 0
        self._prev_atr = 0

        fast = SimpleMovingAverage()
        fast.Length = self._fast_len.Value
        mid = SimpleMovingAverage()
        mid.Length = self._mid_len.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_len.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_len.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, mid, slow, atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, mid)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, mid_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)
        mid = float(mid_val)
        slow = float(slow_val)
        atr = float(atr_val)
        close = float(candle.ClosePrice)

        if self._prev_fast == 0 or self._prev_mid == 0 or self._prev_slow == 0:
            self._prev_fast = fast
            self._prev_mid = mid
            self._prev_slow = slow
            self._prev_atr = atr
            return

        bullish = fast > mid and mid > slow
        bearish = fast < mid and mid < slow
        atr_rising = atr > self._prev_atr

        # Exit on alignment lost
        if self.Position > 0 and not bullish:
            self.SellMarket()
        elif self.Position < 0 and not bearish:
            self.BuyMarket()

        # Entry
        if self.Position == 0:
            if bullish and atr_rising and close > fast:
                self.BuyMarket()
            elif bearish and atr_rising and close < fast:
                self.SellMarket()

        self._prev_fast = fast
        self._prev_mid = mid
        self._prev_slow = slow
        self._prev_atr = atr

    def CreateClone(self):
        return true_sort_1001_strategy()
