import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class baseline2_strategy(Strategy):
    def __init__(self):
        super(baseline2_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 9) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def fast_ema_length(self):
        return self._fast_ema_length.Value
    @fast_ema_length.setter
    def fast_ema_length(self, value):
        self._fast_ema_length.Value = value

    @property
    def slow_ema_length(self):
        return self._slow_ema_length.Value
    @slow_ema_length.setter
    def slow_ema_length(self, value):
        self._slow_ema_length.Value = value

    @property
    def rsi_length(self):
        return self._rsi_length.Value
    @rsi_length.setter
    def rsi_length(self, value):
        self._rsi_length.Value = value

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
        super(baseline2_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted(self, time):
        super(baseline2_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_ema_length
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_ema_length
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, rsi, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_value, slow_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self._bar_index += 1
        cooldown_ok = self._bar_index - self._last_trade_bar > self.cooldown_bars

        cross_up = self._prev_fast > 0 and self._prev_fast <= self._prev_slow and fast_value > slow_value and rsi_value > 50
        cross_down = self._prev_fast > 0 and self._prev_fast >= self._prev_slow and fast_value < slow_value and rsi_value < 50

        if cross_up and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif cross_down and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_fast = float(fast_value)
        self._prev_slow = float(slow_value)

    def CreateClone(self):
        return baseline2_strategy()
