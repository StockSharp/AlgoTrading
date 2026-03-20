import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class zahorchak_measure_strategy(Strategy):
    def __init__(self):
        super(zahorchak_measure_strategy, self).__init__()
        self._points = self.Param("Points", 1) \
            .SetDisplay("Point Value", "Score per condition", "Scoring")
        self._ema_smoothing = self.Param("EmaSmoothing", 10) \
            .SetDisplay("EMA Smoothing", "Smoothing length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_score = 0.0
        self._has_prev = False
        self._ema_measure = 0.0

    @property
    def points(self):
        return self._points.Value

    @property
    def ema_smoothing(self):
        return self._ema_smoothing.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zahorchak_measure_strategy, self).OnReseted()
        self._prev_score = 0.0
        self._has_prev = False
        self._ema_measure = 0.0

    def OnStarted(self, time):
        super(zahorchak_measure_strategy, self).OnStarted(time)
        sma_short = SimpleMovingAverage()
        sma_short.Length = 25
        sma_medium = SimpleMovingAverage()
        sma_medium.Length = 75
        sma_long = SimpleMovingAverage()
        sma_long.Length = 200
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma_short, sma_medium, sma_long, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma_short)
            self.DrawIndicator(area, sma_medium)
            self.DrawOwnTrades(area)

    def on_process(self, candle, short_val, medium_val, long_val):
        if candle.State != CandleStates.Finished:
            return
        # Compute breadth score
        score = 0
        (self.points if score += candle.ClosePrice > short_val else -self.points)
        (self.points if score += candle.ClosePrice > medium_val else -self.points)
        (self.points if score += candle.ClosePrice > long_val else -self.points)
        (self.points if score += short_val > medium_val else -self.points)
        (self.points if score += medium_val > long_val else -self.points)
        (self.points if score += short_val > long_val else -self.points)
        max_score = self.points * 6
        normalized = (10 * score / max_score if max_score != 0 else 0)
        # EMA smoothing
        if not self._has_prev:
            self._ema_measure = normalized
            self._prev_score = normalized
            self._has_prev = True
            return
        k = 2 / (self.ema_smoothing + 1)
        self._ema_measure = normalized * k + self._ema_measure * (1 - k)
        measure = self._ema_measure
        # Trade on zero-line cross
        if self._prev_score <= 0 and measure > 0 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_score >= 0 and measure < 0 and self.Position >= 0:
            self.SellMarket()
        self._prev_score = measure

    def CreateClone(self):
        return zahorchak_measure_strategy()
