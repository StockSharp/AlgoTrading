import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from collections import deque


class area_macd_strategy(Strategy):
    def __init__(self):
        super(area_macd_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast Length", "Fast EMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow Length", "Slow EMA period", "Indicators")
        self._history_length = self.Param("HistoryLength", 20) \
            .SetDisplay("History Length", "Area accumulation window", "Indicators")

        self._diff_history = deque()
        self._pos_area = 0.0
        self._neg_area = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @property
    def HistoryLength(self):
        return self._history_length.Value

    def OnReseted(self):
        super(area_macd_strategy, self).OnReseted()
        self._diff_history = deque()
        self._pos_area = 0.0
        self._neg_area = 0.0

    def OnStarted(self, time):
        super(area_macd_strategy, self).OnStarted(time)
        self._diff_history = deque()
        self._pos_area = 0.0
        self._neg_area = 0.0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastLength
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fv = float(fast_value)
        sv = float(slow_value)
        diff = fv - sv

        self._diff_history.append(diff)
        if diff > 0:
            self._pos_area += diff
        else:
            self._neg_area += abs(diff)

        if len(self._diff_history) > self.HistoryLength:
            old = self._diff_history.popleft()
            if old > 0:
                self._pos_area -= old
            else:
                self._neg_area -= abs(old)
        if len(self._diff_history) < self.HistoryLength:
            return

        if self._pos_area > self._neg_area * 1.25 and self.Position <= 0:
            self.BuyMarket()
        elif self._neg_area > self._pos_area * 1.25 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return area_macd_strategy()
