import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, TripleExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class two_pole_ideal_ma_strategy(Strategy):
    def __init__(self):
        super(two_pole_ideal_ma_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast Period", "Fast MA length", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow Period", "Slow MA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._min_spread_percent = self.Param("MinSpreadPercent", 0.001) \
            .SetDisplay("Minimum Spread %", "Minimum normalized spread between fast and slow averages", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown_remaining = 0

    @property
    def fast_period(self):
        return self._fast_period.Value
    @property
    def slow_period(self):
        return self._slow_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def min_spread_percent(self):
        return self._min_spread_percent.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(two_pole_ideal_ma_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(two_pole_ideal_ma_strategy, self).OnStarted2(time)
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_period
        slow_ma = TripleExponentialMovingAverage()
        slow_ma.Length = self.slow_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, self.process_candle).Start()

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast)
        slow = float(slow)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow
        close = float(candle.ClosePrice)
        spread_percent = abs(fast - slow) / close if close != 0 else 0.0
        self._prev_fast = fast
        self._prev_slow = slow
        min_sp = float(self.min_spread_percent)
        if cross_up and self.Position <= 0 and spread_percent >= min_sp and self._cooldown_remaining == 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif cross_down and self.Position >= 0 and spread_percent >= min_sp and self._cooldown_remaining == 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return two_pole_ideal_ma_strategy()
