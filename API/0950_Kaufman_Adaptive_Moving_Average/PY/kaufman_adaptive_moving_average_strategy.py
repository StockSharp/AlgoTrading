import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KaufmanAdaptiveMovingAverage
from StockSharp.Algo.Strategies import Strategy


class kaufman_adaptive_moving_average_strategy(Strategy):
    def __init__(self):
        super(kaufman_adaptive_moving_average_strategy, self).__init__()
        self._length = self.Param("Length", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Length", "KAMA lookback period", "KAMA")
        self._fast = self.Param("Fast", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast period", "Fast EMA length for KAMA", "KAMA")
        self._slow = self.Param("Slow", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow period", "Slow EMA length for KAMA", "KAMA")
        self._rising_period = self.Param("RisingPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Rising period", "Bars for KAMA rising condition", "Strategy")
        self._falling_period = self.Param("FallingPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Falling period", "Bars for KAMA falling condition", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle type", "Type of candles", "General")
        self._prev_kama = 0.0
        self._rising_count = 0
        self._falling_count = 0
        self._is_first = True
        self._was_rising = False
        self._was_falling = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(kaufman_adaptive_moving_average_strategy, self).OnReseted()
        self._prev_kama = 0.0
        self._rising_count = 0
        self._falling_count = 0
        self._is_first = True
        self._was_rising = False
        self._was_falling = False

    def OnStarted(self, time):
        super(kaufman_adaptive_moving_average_strategy, self).OnStarted(time)
        kama = KaufmanAdaptiveMovingAverage()
        kama.Length = self._length.Value
        kama.FastSCPeriod = self._fast.Value
        kama.SlowSCPeriod = self._slow.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(kama, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, kama)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, kama_val):
        if candle.State != CandleStates.Finished:
            return
        kv = float(kama_val)
        if self._is_first:
            self._prev_kama = kv
            self._is_first = False
            return
        if kv > self._prev_kama:
            self._rising_count += 1
            self._falling_count = 0
        elif kv < self._prev_kama:
            self._falling_count += 1
            self._rising_count = 0
        else:
            self._rising_count = 0
            self._falling_count = 0
        is_rising = self._rising_count >= self._rising_period.Value
        is_falling = self._falling_count >= self._falling_period.Value
        rising_edge = is_rising and not self._was_rising
        falling_edge = is_falling and not self._was_falling
        if rising_edge:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position == 0:
                self.BuyMarket()
        if falling_edge:
            if self.Position > 0:
                self.SellMarket()
            if self.Position == 0:
                self.SellMarket()
        self._was_rising = is_rising
        self._was_falling = is_falling
        self._prev_kama = kv

    def CreateClone(self):
        return kaufman_adaptive_moving_average_strategy()
