import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange

class gandalf_pro_projection_strategy(Strategy):
    def __init__(self):
        super(gandalf_pro_projection_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._filter_length = self.Param("FilterLength", 24) \
            .SetDisplay("Filter Length", "Smoothing filter length", "Filter")
        self._price_factor = self.Param("PriceFactor", 0.18) \
            .SetDisplay("Price Factor", "Close price weight in filter", "Filter")
        self._trend_factor = self.Param("TrendFactor", 0.18) \
            .SetDisplay("Trend Factor", "Trend term weight in filter", "Filter")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period for entry buffer", "Indicators")

        self._close_buffer = []
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FilterLength(self):
        return self._filter_length.Value

    @property
    def PriceFactor(self):
        return self._price_factor.Value

    @property
    def TrendFactor(self):
        return self._trend_factor.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    def OnStarted(self, time):
        super(gandalf_pro_projection_strategy, self).OnStarted(time)

        self._close_buffer = []
        self._entry_price = 0.0

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_val)
        close = float(candle.ClosePrice)

        self._close_buffer.append(close)
        max_depth = self.FilterLength + 2
        while len(self._close_buffer) > max_depth:
            self._close_buffer.pop(0)

        fl = self.FilterLength
        if len(self._close_buffer) <= fl or av <= 0:
            return

        target = self._calculate_target()
        if target is None:
            return

        buffer_dist = av * 0.3

        # Manage position
        if self.Position > 0:
            if target < close - buffer_dist:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if target > close + buffer_dist:
                self.BuyMarket()
                self._entry_price = 0.0

        # Entry
        if self.Position == 0:
            if target > close + buffer_dist:
                self._entry_price = close
                self.BuyMarket()
            elif target < close - buffer_dist:
                self._entry_price = close
                self.SellMarket()

    def _calculate_target(self):
        n = self.FilterLength
        if n < 2 or len(self._close_buffer) < n + 1:
            return None

        total = 0.0
        for i in range(1, n + 1):
            total += self._get_close(i)
        sm = total / n

        weighted_sum = 0.0
        for i in range(n):
            price = self._get_close(i + 1)
            weight = n - i
            weighted_sum += price * weight

        denominator = n * (n + 1) / 2.0
        if denominator <= 0:
            return None

        lm = weighted_sum / denominator
        divisor = n - 1
        if divisor <= 0:
            return None

        pf = float(self.PriceFactor)
        tf = float(self.TrendFactor)

        s = [0.0] * (n + 2)
        t = [0.0] * (n + 2)

        tn = (6.0 * lm - 6.0 * sm) / divisor
        sn = 4.0 * sm - 3.0 * lm - tn
        s[n] = sn
        t[n] = tn

        for k in range(n - 1, 0, -1):
            c = self._get_close(k)
            s[k] = pf * c + (1.0 - pf) * (s[k + 1] + t[k + 1])
            t[k] = tf * (s[k] - s[k + 1]) + (1.0 - tf) * t[k + 1]

        return s[1] + t[1]

    def _get_close(self, index):
        idx = len(self._close_buffer) - 1 - index
        if idx < 0:
            idx = 0
        if idx >= len(self._close_buffer):
            idx = len(self._close_buffer) - 1
        return self._close_buffer[idx]

    def OnReseted(self):
        super(gandalf_pro_projection_strategy, self).OnReseted()
        self._close_buffer = []
        self._entry_price = 0.0

    def CreateClone(self):
        return gandalf_pro_projection_strategy()
