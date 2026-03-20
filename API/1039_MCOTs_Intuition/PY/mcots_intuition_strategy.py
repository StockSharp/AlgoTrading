import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class mcots_intuition_strategy(Strategy):
    def __init__(self):
        super(mcots_intuition_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI calculation period", "General")
        self._momentum_threshold = self.Param("MomentumThreshold", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Threshold", "Minimum RSI momentum", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._prev_rsi = 0.0
        self._prev_momentum = 0.0
        self._take_profit_price = 0.0
        self._stop_loss_price = 0.0
        self._has_prev = False
        self._bars_from_signal = 0
        self._bar_index = 0
        self._entry_bar = -1

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mcots_intuition_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_momentum = 0.0
        self._take_profit_price = 0.0
        self._stop_loss_price = 0.0
        self._has_prev = False
        self._bars_from_signal = 0
        self._bar_index = 0
        self._entry_bar = -1

    def OnStarted(self, time):
        super(mcots_intuition_strategy, self).OnStarted(time)
        self._prev_rsi = 0.0
        self._prev_momentum = 0.0
        self._has_prev = False
        self._take_profit_price = 0.0
        self._stop_loss_price = 0.0
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._bar_index = 0
        self._entry_bar = -1
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self.OnProcess).Start()

    def OnProcess(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        rv = float(rsi_value)
        if not self._rsi.IsFormed:
            self._prev_rsi = rv
            return
        momentum = rv - self._prev_rsi
        self._bar_index += 1
        if not self._has_prev:
            self._prev_rsi = rv
            self._prev_momentum = momentum
            self._has_prev = True
            return
        self._bars_from_signal += 1
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        th = float(self._momentum_threshold.Value)
        if self.Position == 0:
            long_signal = self._prev_momentum <= th and momentum > th and rv >= 50.0
            cd = self._signal_cooldown_bars.Value
            if self._bars_from_signal >= cd and long_signal:
                self.BuyMarket()
                self._take_profit_price = close * 1.03
                self._stop_loss_price = close * 0.98
                self._bars_from_signal = 0
                self._entry_bar = self._bar_index
        elif self.Position > 0:
            timed_exit = self._entry_bar >= 0 and self._bar_index - self._entry_bar >= 16
            if high >= self._take_profit_price or low <= self._stop_loss_price or timed_exit:
                self.SellMarket()
        self._prev_rsi = rv
        self._prev_momentum = momentum

    def CreateClone(self):
        return mcots_intuition_strategy()
