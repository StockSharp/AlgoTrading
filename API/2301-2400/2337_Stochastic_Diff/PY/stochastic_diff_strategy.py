import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class stochastic_diff_strategy(Strategy):
    BUFFER_SIZE = 64

    def __init__(self):
        super(stochastic_diff_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type for analysis", "General")
        self._k_period = self.Param("KPeriod", 14) \
            .SetDisplay("%K Period", "Stochastic %K period", "Stochastic")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("%D Period", "Stochastic %D period", "Stochastic")
        self._smoothing_length = self.Param("SmoothingLength", 5) \
            .SetDisplay("Smoothing Length", "Length for diff smoothing", "Stochastic")
        self._cooldown_candles = self.Param("CooldownCandles", 2) \
            .SetDisplay("Cooldown Candles", "Minimum candles between entries", "Trading")
        self._highs = [0.0] * self.BUFFER_SIZE
        self._lows = [0.0] * self.BUFFER_SIZE
        self._raw_k = [0.0] * self.BUFFER_SIZE
        self._price_index = 0
        self._price_count = 0
        self._k_index = 0
        self._k_count = 0
        self._bars_since_signal = 0
        self._smoothed_diff = None
        self._prev_diff = None
        self._prev_prev_diff = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def k_period(self):
        return self._k_period.Value

    @property
    def d_period(self):
        return self._d_period.Value

    @property
    def smoothing_length(self):
        return self._smoothing_length.Value

    @property
    def cooldown_candles(self):
        return self._cooldown_candles.Value

    def OnReseted(self):
        super(stochastic_diff_strategy, self).OnReseted()
        self._highs = [0.0] * self.BUFFER_SIZE
        self._lows = [0.0] * self.BUFFER_SIZE
        self._raw_k = [0.0] * self.BUFFER_SIZE
        self._price_index = 0
        self._price_count = 0
        self._k_index = 0
        self._k_count = 0
        self._bars_since_signal = int(self.cooldown_candles)
        self._smoothed_diff = None
        self._prev_diff = None
        self._prev_prev_diff = None

    def OnStarted2(self, time):
        super(stochastic_diff_strategy, self).OnStarted2(time)
        self._highs = [0.0] * self.BUFFER_SIZE
        self._lows = [0.0] * self.BUFFER_SIZE
        self._raw_k = [0.0] * self.BUFFER_SIZE
        self._price_index = 0
        self._price_count = 0
        self._k_index = 0
        self._k_count = 0
        self._bars_since_signal = int(self.cooldown_candles)
        self._smoothed_diff = None
        self._prev_diff = None
        self._prev_prev_diff = None
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

    def _update_smoothed_diff(self, value):
        if self._smoothed_diff is None:
            self._smoothed_diff = value
            return value
        multiplier = 2.0 / (int(self.smoothing_length) + 1)
        self._smoothed_diff = self._smoothed_diff + ((value - self._smoothed_diff) * multiplier)
        return self._smoothed_diff

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        self._push_price(high, low)
        self._bars_since_signal += 1
        kp = int(self.k_period)
        dp = int(self.d_period)
        if self._price_count < kp:
            return
        highest = self._get_highest(kp)
        lowest = self._get_lowest(kp)
        rng = highest - lowest
        k = (close - lowest) / rng * 100.0 if rng > 0 else 50.0
        self._push_raw_k(k)
        if self._k_count < dp:
            return
        d = self._get_raw_k_average(dp)
        diff = k - d
        current = self._update_smoothed_diff(diff)
        if self._prev_prev_diff is not None and self._prev_diff is not None:
            turning_up = self._prev_diff < self._prev_prev_diff and current >= self._prev_diff
            turning_down = self._prev_diff > self._prev_prev_diff and current <= self._prev_diff
            cd = int(self.cooldown_candles)
            if self._bars_since_signal >= cd and k <= 25.0 and turning_up and self.Position <= 0:
                self.BuyMarket()
                self._bars_since_signal = 0
            elif self._bars_since_signal >= cd and k >= 75.0 and turning_down and self.Position >= 0:
                self.SellMarket()
                self._bars_since_signal = 0
        self._prev_prev_diff = self._prev_diff
        self._prev_diff = current

    def CreateClone(self):
        return stochastic_diff_strategy()
