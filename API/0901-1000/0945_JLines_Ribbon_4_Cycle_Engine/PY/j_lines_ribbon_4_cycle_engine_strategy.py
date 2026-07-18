import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class j_lines_ribbon_4_cycle_engine_strategy(Strategy):
    def __init__(self):
        super(j_lines_ribbon_4_cycle_engine_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_length = self.Param("FastLength", 72) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Length", "Fast EMA length", "Indicators")
        self._slow_length = self.Param("SlowLength", 89) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Length", "Slow EMA length", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum bars between signals", "Risk")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(j_lines_ribbon_4_cycle_engine_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._bars_since_signal = 0

    def OnStarted2(self, time):
        super(j_lines_ribbon_4_cycle_engine_strategy, self).OnStarted2(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.OnProcess).Start()

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast_val)
        slow = float(slow_val)
        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            self._bars_since_signal = self._cooldown_bars.Value
            return
        self._bars_since_signal += 1
        cd = self._cooldown_bars.Value
        if self._bars_since_signal >= cd:
            cross_up = self._prev_fast <= self._prev_slow and fast > slow
            cross_down = self._prev_fast >= self._prev_slow and fast < slow
            if cross_up:
                if self.Position < 0:
                    self.BuyMarket()
                elif self.Position == 0:
                    self.BuyMarket()
                self._bars_since_signal = 0
            elif cross_down:
                if self.Position > 0:
                    self.SellMarket()
                elif self.Position == 0:
                    self.SellMarket()
                self._bars_since_signal = 0
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return j_lines_ribbon_4_cycle_engine_strategy()
