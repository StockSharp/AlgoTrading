import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class exp_atr_trailing_strategy(Strategy):
    def __init__(self):
        super(exp_atr_trailing_strategy, self).__init__()
        self._std_period = self.Param("StdPeriod", 14) \
            .SetDisplay("StdDev Period", "StdDev period", "Indicators")
        self._std_factor = self.Param("StdFactor", 2.0) \
            .SetDisplay("StdDev Factor", "StdDev multiplier for trailing stop", "Indicators")
        self._fast_ema = self.Param("FastEma", 10) \
            .SetDisplay("Fast EMA", "Fast EMA for entry", "Indicators")
        self._slow_ema = self.Param("SlowEma", 30) \
            .SetDisplay("Slow EMA", "Slow EMA for entry", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._long_trail = 0.0
        self._short_trail = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def std_period(self):
        return self._std_period.Value

    @property
    def std_factor(self):
        return self._std_factor.Value

    @property
    def fast_ema(self):
        return self._fast_ema.Value

    @property
    def slow_ema(self):
        return self._slow_ema.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_atr_trailing_strategy, self).OnReseted()
        self._long_trail = 0.0
        self._short_trail = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(exp_atr_trailing_strategy, self).OnStarted2(time)
        fast = ExponentialMovingAverage()
        fast.Length = self.fast_ema
        slow = ExponentialMovingAverage()
        slow.Length = self.slow_ema
        std_dev = StandardDeviation()
        std_dev.Length = self.std_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, std_val):
        if candle.State != CandleStates.Finished:
            return
        # Trail management
        if self.Position > 0:
            stop = candle.ClosePrice - std_val * self.std_factor
            if stop > self._long_trail:
                self._long_trail = stop
            if candle.LowPrice <= self._long_trail:
                self.SellMarket()
                self._long_trail = 0
        elif self.Position < 0:
            stop = candle.ClosePrice + std_val * self.std_factor
            if stop < self._short_trail or self._short_trail == 0:
                self._short_trail = stop
            if candle.HighPrice >= self._short_trail:
                self.BuyMarket()
                self._short_trail = 0
        # Entry signals
        if self._has_prev and std_val > 0:
            cross_up = self._prev_fast <= self._prev_slow and fast > slow
            cross_down = self._prev_fast >= self._prev_slow and fast < slow
            if cross_up and self.Position <= 0:
                self.BuyMarket()
                self._long_trail = candle.ClosePrice - std_val * self.std_factor
                self._short_trail = 0
            elif cross_down and self.Position >= 0:
                self.SellMarket()
                self._short_trail = candle.ClosePrice + std_val * self.std_factor
                self._long_trail = 0
        self._prev_fast = fast
        self._prev_slow = slow
        self._has_prev = True

    def CreateClone(self):
        return exp_atr_trailing_strategy()
