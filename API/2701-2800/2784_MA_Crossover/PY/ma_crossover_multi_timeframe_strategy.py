import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_crossover_multi_timeframe_strategy(Strategy):
    """
    MA crossover with EMA fast/slow and cooldown.
    Simplified single-timeframe version.
    """

    def __init__(self):
        super(ma_crossover_multi_timeframe_strategy, self).__init__()
        self._fast_period = self.Param("CurrentMaPeriod", 42).SetDisplay("Fast MA", "Fast MA period", "Indicators")
        self._slow_period = self.Param("SlowMaPeriod", 52).SetDisplay("Slow MA", "Slow MA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 20).SetDisplay("Cooldown", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_crossover_multi_timeframe_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(ma_crossover_multi_timeframe_strategy, self).OnStarted2(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        f = float(fast_val)
        s = float(slow_val)
        if self._cooldown > 0:
            self._cooldown -= 1
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_fast = f
            self._prev_slow = s
            self._has_prev = True
            return
        if not self._has_prev:
            self._prev_fast = f
            self._prev_slow = s
            self._has_prev = True
            return
        is_above = f > s
        was_above = self._prev_fast > self._prev_slow
        if is_above != was_above and self._cooldown == 0:
            if is_above and self.Position <= 0:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value
            elif not is_above and self.Position >= 0:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
        self._prev_fast = f
        self._prev_slow = s

    def CreateClone(self):
        return ma_crossover_multi_timeframe_strategy()
