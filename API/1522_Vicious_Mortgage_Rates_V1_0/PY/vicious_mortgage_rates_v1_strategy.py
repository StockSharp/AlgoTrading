import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vicious_mortgage_rates_v1_strategy(Strategy):
    def __init__(self):
        super(vicious_mortgage_rates_v1_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 8) \
            .SetDisplay("Fast EMA", "Fast EMA length", "General")
        self._slow_length = self.Param("SlowLength", 21) \
            .SetDisplay("Slow EMA", "Slow EMA length", "General")
        self._vol_length = self.Param("VolLength", TimeSpan.FromMinutes(15)) \
            .SetDisplay("Vol Length", "Volatility lookback", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown = 0

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def vol_length(self):
        return self._vol_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vicious_mortgage_rates_v1_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(vicious_mortgage_rates_v1_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_length
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown > 0:
            self._cooldown -= 1
        if self._prev_fast == 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        if self._cooldown > 0:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        long_cross = self._prev_fast <= self._prev_slow and fast > slow
        short_cross = self._prev_fast >= self._prev_slow and fast < slow
        if long_cross and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 30
        elif short_cross and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 30
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return vicious_mortgage_rates_v1_strategy()
