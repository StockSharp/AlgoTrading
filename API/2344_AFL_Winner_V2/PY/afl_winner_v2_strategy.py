import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class afl_winner_v2_strategy(Strategy):
    BUFFER_SIZE = 64

    def __init__(self):
        super(afl_winner_v2_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 5) \
            .SetDisplay("%K Period", "%K Period", "General")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("%D Period", "%D Period", "General")
        self._high_level = self.Param("HighLevel", 40.0) \
            .SetDisplay("High Level", "High Level", "General")
        self._low_level = self.Param("LowLevel", -40.0) \
            .SetDisplay("Low Level", "Low Level", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._highs = [0.0] * self.BUFFER_SIZE
        self._lows = [0.0] * self.BUFFER_SIZE
        self._raw_k = [0.0] * self.BUFFER_SIZE
        self._price_index = 0
        self._price_count = 0
        self._k_index = 0
        self._k_count = 0
        self._prev_color = -1

    @property
    def k_period(self):
        return self._k_period.Value

    @property
    def d_period(self):
        return self._d_period.Value

    @property
    def high_level(self):
        return self._high_level.Value

    @property
    def low_level(self):
        return self._low_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(afl_winner_v2_strategy, self).OnReseted()
        self._highs = [0.0] * self.BUFFER_SIZE
        self._lows = [0.0] * self.BUFFER_SIZE
        self._raw_k = [0.0] * self.BUFFER_SIZE
        self._price_index = 0
        self._price_count = 0
        self._k_index = 0
        self._k_count = 0
        self._prev_color = -1

    def OnStarted(self, time):
        super(afl_winner_v2_strategy, self).OnStarted(time)
        self._highs = [0.0] * self.BUFFER_SIZE
        self._lows = [0.0] * self.BUFFER_SIZE
        self._raw_k = [0.0] * self.BUFFER_SIZE
        self._price_index = 0
        self._price_count = 0
        self._k_index = 0
        self._k_count = 0
        self._prev_color = -1
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _push_price(self, high, low):
        self._highs[self._price_index] = high
        self._lows[self._price_index] = low
        self._price_index = (self._price_index + 1) % self.BUFFER_SIZE
        if self._price_count < self.BUFFER_SIZE:
            self._price_count += 1

    def _push_raw_k(self, value):
        self._raw_k[self._k_index] = value
        self._k_index = (self._k_index + 1) % self.BUFFER_SIZE
        if self._k_count < self.BUFFER_SIZE:
            self._k_count += 1

    def _get_highest(self, period):
        highest = -1e18
        count = min(period, self._price_count)
        for i in range(count):
            idx = (self._price_index - 1 - i + self.BUFFER_SIZE) % self.BUFFER_SIZE
            if self._highs[idx] > highest:
                highest = self._highs[idx]
        return highest

    def _get_lowest(self, period):
        lowest = 1e18
        count = min(period, self._price_count)
        for i in range(count):
            idx = (self._price_index - 1 - i + self.BUFFER_SIZE) % self.BUFFER_SIZE
            if self._lows[idx] < lowest:
                lowest = self._lows[idx]
        return lowest

    def _get_raw_k_average(self, period):
        count = min(period, self._k_count)
        s = 0.0
        for i in range(count):
            idx = (self._k_index - 1 - i + self.BUFFER_SIZE) % self.BUFFER_SIZE
            s += self._raw_k[idx]
        return s / count if count > 0 else 0.0

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        self._push_price(high, low)
        kp = int(self.k_period)
        dp = int(self.d_period)
        if self._price_count < kp:
            return
        highest = self._get_highest(kp)
        lowest = self._get_lowest(kp)
        rng = highest - lowest
        raw_k = (close - lowest) / rng * 100.0 if rng > 0 else 50.0
        self._push_raw_k(raw_k)
        if self._k_count < dp:
            return
        k = raw_k - 50.0
        d = self._get_raw_k_average(dp) - 50.0
        hl = float(self.high_level)
        ll = float(self.low_level)
        if k > d:
            color = 3 if (k > hl or (k > ll and d <= ll)) else 2
        else:
            color = 0 if (k < ll or (d > hl and k <= hl)) else 1
        if color == 3 and self._prev_color != 3 and self.Position <= 0:
            self.BuyMarket()
        elif color == 0 and self._prev_color != 0 and self.Position >= 0:
            self.SellMarket()
        elif color < 2 and self.Position > 0:
            self.SellMarket()
        elif color > 1 and self.Position < 0:
            self.BuyMarket()
        self._prev_color = color

    def CreateClone(self):
        return afl_winner_v2_strategy()
