import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class average_force_strategy(Strategy):
    def __init__(self):
        super(average_force_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 18) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 50) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def fast_length(self):
        return self._fast_length.Value
    @fast_length.setter
    def fast_length(self, value):
        self._fast_length.Value = value

    @property
    def slow_length(self):
        return self._slow_length.Value
    @slow_length.setter
    def slow_length(self, value):
        self._slow_length.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value
    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    @property
    def candle_type(self):
        return self._candle_type.Value
    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(average_force_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted2(self, time):
        super(average_force_strategy, self).OnStarted2(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_length
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1
        cooldown_ok = self._bar_index - self._last_trade_bar > self.cooldown_bars

        cross_up = self._prev_fast > 0 and self._prev_fast <= self._prev_slow and fast_value > slow_value
        cross_down = self._prev_fast > 0 and self._prev_fast >= self._prev_slow and fast_value < slow_value

        if cross_up and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif cross_down and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_fast = float(fast_value)
        self._prev_slow = float(slow_value)

    def CreateClone(self):
        return average_force_strategy()
