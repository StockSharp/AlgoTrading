import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class long_short_exit_risk_management_strategy(Strategy):
    def __init__(self):
        super(long_short_exit_risk_management_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 25) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 5.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 5) \
            .SetDisplay("Cooldown Bars", "Min bars between signals", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(long_short_exit_risk_management_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bars_since_signal = 0

    def OnStarted2(self, time):
        super(long_short_exit_risk_management_strategy, self).OnStarted2(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bars_since_signal = 0
        self._fast = ExponentialMovingAverage()
        self._fast.Length = self._fast_period.Value
        self._slow = ExponentialMovingAverage()
        self._slow.Length = self._slow_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast, self._slow, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast)
            self.DrawIndicator(area, self._slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        fv = float(fast_val)
        sv = float(slow_val)
        if not self._fast.IsFormed or not self._slow.IsFormed or self._prev_fast == 0.0 or self._prev_slow == 0.0:
            self._prev_fast = fv
            self._prev_slow = sv
            return
        if self._bars_since_signal < self._cooldown_bars.Value:
            self._prev_fast = fv
            self._prev_slow = sv
            return
        cross_up = self._prev_fast <= self._prev_slow and fv > sv
        cross_down = self._prev_fast >= self._prev_slow and fv < sv
        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._bars_since_signal = 0
        elif cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._bars_since_signal = 0
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return long_short_exit_risk_management_strategy()
