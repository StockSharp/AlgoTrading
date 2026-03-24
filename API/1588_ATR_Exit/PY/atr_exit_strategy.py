import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class atr_exit_strategy(Strategy):
    def __init__(self):
        super(atr_exit_strategy, self).__init__()
        self._fast_len = self.Param("FastLength", 10) \
            .SetDisplay("Fast EMA", "Fast EMA length", "General")
        self._slow_len = self.Param("SlowLength", 30) \
            .SetDisplay("Slow EMA", "Slow EMA length", "General")
        self._atr_len = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to process", "General")
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    @property
    def fast_length(self):
        return self._fast_len.Value

    @property
    def slow_length(self):
        return self._slow_len.Value

    @property
    def atr_length(self):
        return self._atr_len.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(atr_exit_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0

    def OnStarted(self, time):
        super(atr_exit_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_length
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_length
        atr = AverageTrueRange()
        atr.Length = self.atr_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, atr):
        if candle.State != CandleStates.Finished:
            return
        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow
        if self.Position == 0:
            if cross_up:
                self._entry_price = candle.ClosePrice
                self._stop_price = self._entry_price - 1.5 * atr
                self.BuyMarket()
            elif cross_down:
                self._entry_price = candle.ClosePrice
                self._stop_price = self._entry_price + 1.5 * atr
                self.SellMarket()
        elif self.Position > 0:
            new_stop = candle.ClosePrice - 1.5 * atr
            if new_stop > self._stop_price:
                self._stop_price = new_stop
            if candle.ClosePrice < self._stop_price or cross_down:
                self.SellMarket()
        elif self.Position < 0:
            new_stop = candle.ClosePrice + 1.5 * atr
            if new_stop < self._stop_price:
                self._stop_price = new_stop
            if candle.ClosePrice > self._stop_price or cross_up:
                self.BuyMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return atr_exit_strategy()
