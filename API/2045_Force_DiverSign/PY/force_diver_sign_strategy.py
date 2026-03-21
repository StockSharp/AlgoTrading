import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class force_diver_sign_strategy(Strategy):
    """
    Force DiverSign: detects divergence between fast and slow Force Index.
    Uses candle pattern (alternating bull/bear) combined with force divergence.
    """

    def __init__(self):
        super(force_diver_sign_strategy, self).__init__()
        self._period1 = self.Param("Period1", 3) \
            .SetDisplay("Fast Period", "Period for fast Force index", "Indicators")
        self._period2 = self.Param("Period2", 7) \
            .SetDisplay("Slow Period", "Period for slow Force index", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._opens = [0.0] * 5
        self._closes = [0.0] * 5
        self._f1 = [0.0] * 5
        self._f2 = [0.0] * 5
        self._prev_close = 0.0
        self._count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(force_diver_sign_strategy, self).OnReseted()
        self._opens = [0.0] * 5
        self._closes = [0.0] * 5
        self._f1 = [0.0] * 5
        self._f2 = [0.0] * 5
        self._prev_close = 0.0
        self._count = 0

    def OnStarted(self, time):
        super(force_diver_sign_strategy, self).OnStarted(time)

        self._ma1 = ExponentialMovingAverage()
        self._ma1.Length = self._period1.Value
        self._ma2 = ExponentialMovingAverage()
        self._ma2.Length = self._period2.Value
        self.Indicators.Add(self._ma1)
        self.Indicators.Add(self._ma2)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _shift(self, arr, value):
        for i in range(len(arr) - 1, 0, -1):
            arr[i] = arr[i - 1]
        arr[0] = value

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._count == 0:
            self._prev_close = float(candle.ClosePrice)
            self._shift(self._opens, float(candle.OpenPrice))
            self._shift(self._closes, float(candle.ClosePrice))
            self._count += 1
            return

        close = float(candle.ClosePrice)
        volume = float(candle.TotalVolume)
        force = (close - self._prev_close) * volume
        self._prev_close = close

        f1v = self._ma1.Process(force, candle.OpenTime, True)
        f2v = self._ma2.Process(force, candle.OpenTime, True)

        self._shift(self._opens, float(candle.OpenPrice))
        self._shift(self._closes, close)

        if f1v.IsEmpty or f2v.IsEmpty:
            self._count += 1
            return

        f1 = float(f1v.ToDecimal())
        f2 = float(f2v.ToDecimal())
        self._shift(self._f1, f1)
        self._shift(self._f2, f2)

        if self._count < 5:
            self._count += 1
            return

        sell_signal = (self._opens[3] < self._closes[3] and
                       self._opens[2] > self._closes[2] and
                       self._opens[1] < self._closes[1] and
                       self._f1[4] < self._f1[3] and self._f1[3] > self._f1[2] and self._f1[2] < self._f1[1] and
                       self._f2[4] < self._f2[3] and self._f2[3] > self._f2[2] and self._f2[2] < self._f2[1] and
                       ((self._f1[3] > self._f1[1] and self._f2[3] < self._f2[1]) or
                        (self._f1[3] < self._f1[1] and self._f2[3] > self._f2[1])))

        buy_signal = (self._opens[3] > self._closes[3] and
                      self._opens[2] < self._closes[2] and
                      self._opens[1] > self._closes[1] and
                      self._f1[4] > self._f1[3] and self._f1[3] < self._f1[2] and self._f1[2] > self._f1[1] and
                      self._f2[4] > self._f2[3] and self._f2[3] < self._f2[2] and self._f2[2] > self._f2[1] and
                      ((self._f1[3] > self._f1[1] and self._f2[3] < self._f2[1]) or
                       (self._f1[3] < self._f1[1] and self._f2[3] > self._f2[1])))

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return force_diver_sign_strategy()
