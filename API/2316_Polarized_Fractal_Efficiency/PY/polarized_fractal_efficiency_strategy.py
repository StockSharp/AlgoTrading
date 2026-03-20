import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class polarized_fractal_efficiency_strategy(Strategy):
    def __init__(self):
        super(polarized_fractal_efficiency_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._pfe_period = self.Param("PfePeriod", 9) \
            .SetDisplay("PFE Period", "Indicator calculation period", "Indicators")
        self._closes = []
        self._prev_pfe = 0.0
        self._prev_prev_pfe = 0.0
        self._formed = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def pfe_period(self):
        return self._pfe_period.Value

    def OnReseted(self):
        super(polarized_fractal_efficiency_strategy, self).OnReseted()
        self._closes = []
        self._prev_pfe = 0.0
        self._prev_prev_pfe = 0.0
        self._formed = 0

    def OnStarted(self, time):
        super(polarized_fractal_efficiency_strategy, self).OnStarted(time)
        self._closes = []
        self._prev_pfe = 0.0
        self._prev_prev_pfe = 0.0
        self._formed = 0
        sma = SimpleMovingAverage()
        sma.Length = 1
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, _sma_val):
        if candle.State != CandleStates.Finished:
            return
        self._closes.append(float(candle.ClosePrice))
        period = int(self.pfe_period)
        if len(self._closes) < period + 1:
            return
        while len(self._closes) > period + 2:
            self._closes.pop(0)
        n = len(self._closes)
        close_now = self._closes[n - 1]
        close_past = self._closes[n - 1 - period]
        diff = close_now - close_past
        direct_dist = math.sqrt(diff * diff + period * period)
        sum_dist = 0.0
        for i in range(n - period, n):
            d = self._closes[i] - self._closes[i - 1]
            sum_dist += math.sqrt(d * d + 1.0)
        if sum_dist == 0:
            return
        sign = 1.0 if close_now >= close_past else -1.0
        pfe = 100.0 * sign * direct_dist / sum_dist
        self._formed += 1
        if self._formed < 3:
            self._prev_prev_pfe = self._prev_pfe
            self._prev_pfe = pfe
            return
        if self._prev_pfe < self._prev_prev_pfe and pfe > self._prev_pfe and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_pfe > self._prev_prev_pfe and pfe < self._prev_pfe and self.Position >= 0:
            self.SellMarket()
        self._prev_prev_pfe = self._prev_pfe
        self._prev_pfe = pfe

    def CreateClone(self):
        return polarized_fractal_efficiency_strategy()
