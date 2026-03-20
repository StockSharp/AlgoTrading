import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class moving_average_crossover_strategy(Strategy):
    def __init__(self):
        super(moving_average_crossover_strategy, self).__init__()
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
        super(moving_average_crossover_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(moving_average_crossover_strategy, self).OnStarted(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._bars_since_signal = 0
        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self._fast_length.Value
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self._slow_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self.OnProcess).Start()

    def OnProcess(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fv = float(fast)
        sv = float(slow)
        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed:
            return
        if not self._initialized:
            self._prev_fast = fv
            self._prev_slow = sv
            self._initialized = True
            self._bars_since_signal = self._cooldown_bars.Value
            return
        self._bars_since_signal += 1
        if self._bars_since_signal >= self._cooldown_bars.Value:
            cross_up = self._prev_fast <= self._prev_slow and fv > sv
            cross_down = self._prev_fast >= self._prev_slow and fv < sv
            if cross_up:
                if self.Position <= 0:
                    self.BuyMarket()
                self._bars_since_signal = 0
            elif cross_down:
                if self.Position >= 0:
                    self.SellMarket()
                self._bars_since_signal = 0
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return moving_average_crossover_strategy()
