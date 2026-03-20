import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class universal_trailing_stop_strategy(Strategy):
    def __init__(self):
        super(universal_trailing_stop_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast Period", "Fast EMA period", "Entry")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow Period", "Slow EMA period", "Entry")
        self._trail_percent = self.Param("TrailPercent", 1.5) \
            .SetDisplay("Trail %", "Trailing stop distance in percent", "Trailing")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._entry_price = 0.0
        self._best_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def trail_percent(self):
        return self._trail_percent.Value

    def OnReseted(self):
        super(universal_trailing_stop_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._entry_price = 0.0
        self._best_price = 0.0

    def OnStarted(self, time):
        super(universal_trailing_stop_strategy, self).OnStarted(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._entry_price = 0.0
        self._best_price = 0.0
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_period
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fast_value = float(fast_value)
        slow_value = float(slow_value)
        if not self._is_initialized:
            self._prev_fast = fast_value
            self._prev_slow = slow_value
            self._is_initialized = True
            return
        price = float(candle.ClosePrice)
        trail_pct = float(self.trail_percent)
        # Trailing stop check
        if self.Position > 0:
            if price > self._best_price:
                self._best_price = price
            stop_level = self._best_price * (1.0 - trail_pct / 100.0)
            if price <= stop_level:
                self.SellMarket()
                self._prev_fast = fast_value
                self._prev_slow = slow_value
                return
        elif self.Position < 0:
            if price < self._best_price:
                self._best_price = price
            stop_level = self._best_price * (1.0 + trail_pct / 100.0)
            if price >= stop_level:
                self.BuyMarket()
                self._prev_fast = fast_value
                self._prev_slow = slow_value
                return
        # Entry signals: EMA crossover
        prev_cross_up = self._prev_fast <= self._prev_slow
        curr_cross_up = fast_value > slow_value
        prev_cross_down = self._prev_fast >= self._prev_slow
        curr_cross_down = fast_value < slow_value
        if prev_cross_up and curr_cross_up and not (self._prev_fast > self._prev_slow):
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
                self._entry_price = price
                self._best_price = price
        elif prev_cross_down and curr_cross_down and not (self._prev_fast < self._prev_slow):
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
                self._entry_price = price
                self._best_price = price
        self._prev_fast = fast_value
        self._prev_slow = slow_value

    def CreateClone(self):
        return universal_trailing_stop_strategy()
