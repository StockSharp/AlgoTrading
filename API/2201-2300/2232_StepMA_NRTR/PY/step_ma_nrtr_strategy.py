import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class step_ma_nrtr_strategy(Strategy):
    def __init__(self):
        super(step_ma_nrtr_strategy, self).__init__()
        self._length = self.Param("Length", 10) \
            .SetDisplay("Length", "Volatility length", "Indicator")
        self._kv = self.Param("Kv", 1.0) \
            .SetDisplay("Sensitivity", "Sensitivity factor", "Indicator")
        self._step_size = self.Param("StepSize", 0) \
            .SetDisplay("Step Size", "Constant step size, 0 - auto", "Indicator")
        self._use_high_low = self.Param("UseHighLow", True) \
            .SetDisplay("Use High/Low", "Use high/low range", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type for processing", "General")
        self._ranges = []
        self._smax1 = 0.0
        self._smin1 = 0.0
        self._trend1 = 0
        self._first = True

    @property
    def length(self):
        return self._length.Value

    @property
    def kv(self):
        return self._kv.Value

    @property
    def step_size(self):
        return self._step_size.Value

    @property
    def use_high_low(self):
        return self._use_high_low.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(step_ma_nrtr_strategy, self).OnReseted()
        self._ranges = []
        self._smax1 = 0.0
        self._smin1 = 0.0
        self._trend1 = 0
        self._first = True

    def OnStarted2(self, time):
        super(step_ma_nrtr_strategy, self).OnStarted2(time)
        warmup = ExponentialMovingAverage()
        warmup.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(warmup, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, _warmup_val):
        if candle.State != CandleStates.Finished:
            return
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        rng = h - l
        self._ranges.append(rng)
        if len(self._ranges) > self.length:
            self._ranges.pop(0)
        if len(self._ranges) < self.length:
            return
        kv = float(self.kv)
        if self.step_size == 0:
            atr_max = max(self._ranges)
            atr_min = min(self._ranges)
            step = 0.5 * kv * (atr_max + atr_min)
        else:
            step = kv * float(self.step_size)
        if step == 0:
            return
        size2p = 2.0 * step
        if self._first:
            self._trend1 = 0
            self._smax1 = l + size2p
            self._smin1 = h - size2p
            self._first = False
        if self.use_high_low:
            smax0 = l + size2p
            smin0 = h - size2p
        else:
            smax0 = c + size2p
            smin0 = c - size2p
        trend0 = self._trend1
        if c > self._smax1:
            trend0 = 1
        elif c < self._smin1:
            trend0 = -1
        if trend0 > 0:
            if smin0 < self._smin1:
                smin0 = self._smin1
        else:
            if smax0 > self._smax1:
                smax0 = self._smax1
        buy_signal = trend0 > 0 and self._trend1 < 0
        sell_signal = trend0 < 0 and self._trend1 > 0
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
        self._smax1 = smax0
        self._smin1 = smin0
        self._trend1 = trend0

    def CreateClone(self):
        return step_ma_nrtr_strategy()
