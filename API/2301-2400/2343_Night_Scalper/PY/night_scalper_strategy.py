import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class night_scalper_strategy(Strategy):
    BUFFER_SIZE = 128

    def __init__(self):
        super(night_scalper_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger period", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("BB Deviation", "Bollinger deviation", "Indicators")
        self._range_threshold = self.Param("RangeThreshold", 3000.0) \
            .SetDisplay("Range Threshold", "Maximum band width", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._closes = [0.0] * self.BUFFER_SIZE
        self._close_index = 0
        self._close_count = 0

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value

    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value

    @property
    def range_threshold(self):
        return self._range_threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(night_scalper_strategy, self).OnReseted()
        self._closes = [0.0] * self.BUFFER_SIZE
        self._close_index = 0
        self._close_count = 0

    def OnStarted2(self, time):
        super(night_scalper_strategy, self).OnStarted2(time)
        self._closes = [0.0] * self.BUFFER_SIZE
        self._close_index = 0
        self._close_count = 0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _push_close(self, close):
        self._closes[self._close_index] = close
        self._close_index = (self._close_index + 1) % self.BUFFER_SIZE
        if self._close_count < self.BUFFER_SIZE:
            self._close_count += 1

    def _get_average(self, period):
        count = min(period, self._close_count)
        s = 0.0
        for i in range(count):
            idx = (self._close_index - 1 - i + self.BUFFER_SIZE) % self.BUFFER_SIZE
            s += self._closes[idx]
        return s / count if count > 0 else 0.0

    def _get_standard_deviation(self, period, mean):
        count = min(period, self._close_count)
        s = 0.0
        for i in range(count):
            idx = (self._close_index - 1 - i + self.BUFFER_SIZE) % self.BUFFER_SIZE
            diff = self._closes[idx] - mean
            s += diff * diff
        return math.sqrt(s / count) if count > 0 else 0.0

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        self._push_close(close)
        bp = int(self.bollinger_period)
        if self._close_count < bp:
            return
        mean = self._get_average(bp)
        deviation = self._get_standard_deviation(bp, mean)
        bd = float(self.bollinger_deviation)
        upper = mean + deviation * bd
        lower = mean - deviation * bd
        width = upper - lower
        rt = float(self.range_threshold)
        low_price = float(candle.LowPrice)
        high_price = float(candle.HighPrice)
        if self.Position == 0 and width <= rt:
            if low_price <= lower:
                self.BuyMarket()
            elif high_price >= upper:
                self.SellMarket()
        elif self.Position > 0 and close >= mean:
            self.SellMarket()
        elif self.Position < 0 and close <= mean:
            self.BuyMarket()

    def CreateClone(self):
        return night_scalper_strategy()
