import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class backward_number_of_bars_strategy(Strategy):
    def __init__(self):
        super(backward_number_of_bars_strategy, self).__init__()
        self._momentum_length = self.Param("MomentumLength", 50) \
            .SetDisplay("Momentum Length", "ROC lookback period", "Indicators")
        self._ema_length = self.Param("EmaLength", 40) \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_momentum = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    @property
    def momentum_length(self):
        return self._momentum_length.Value
    @momentum_length.setter
    def momentum_length(self, value):
        self._momentum_length.Value = value

    @property
    def ema_length(self):
        return self._ema_length.Value
    @ema_length.setter
    def ema_length(self, value):
        self._ema_length.Value = value

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
        super(backward_number_of_bars_strategy, self).OnReseted()
        self._prev_momentum = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted(self, time):
        super(backward_number_of_bars_strategy, self).OnStarted(time)
        roc = RateOfChange()
        roc.Length = self.momentum_length
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(roc, ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, roc_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1
        cooldown_ok = self._bar_index - self._last_trade_bar > self.cooldown_bars

        long_signal = self._prev_momentum <= 0 and roc_value > 0 and candle.ClosePrice > ema_value
        short_signal = self._prev_momentum >= 0 and roc_value < 0 and candle.ClosePrice < ema_value

        if long_signal and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif short_signal and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_momentum = float(roc_value)

    def CreateClone(self):
        return backward_number_of_bars_strategy()
