import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class adaptive_kdj_mtf_strategy(Strategy):
    def __init__(self):
        super(adaptive_kdj_mtf_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._kdj_length = self.Param("KdjLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("KDJ Length", "Base length for Stochastic", "Parameters")
        self._smoothing_length = self.Param("SmoothingLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Smoothing Length", "EMA smoothing length", "Parameters")
        self._buy_level = self.Param("BuyLevel", 30.0) \
            .SetDisplay("Buy Level", "J value threshold for buy signal", "Parameters")
        self._sell_level = self.Param("SellLevel", 70.0) \
            .SetDisplay("Sell Level", "J value threshold for sell signal", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_k = 50.0
        self._prev_d = 50.0
        self._smooth_k = 50.0
        self._smooth_d = 50.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(adaptive_kdj_mtf_strategy, self).OnReseted()
        self._prev_k = 50.0
        self._prev_d = 50.0
        self._smooth_k = 50.0
        self._smooth_d = 50.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adaptive_kdj_mtf_strategy, self).OnStarted(time)
        stoch = StochasticOscillator()
        stoch.K.Length = self._kdj_length.Value
        stoch.D.Length = 3
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stoch, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stoch)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return
        if stoch_value.IsEmpty:
            return
        sv = stoch_value
        k = sv.K
        d = sv.D
        if k is None or d is None:
            return
        k = float(k)
        d = float(d)
        j = 3.0 * k - 2.0 * d
        alpha = 2.0 / (float(self._smoothing_length.Value) + 1.0)
        self._smooth_k = alpha * k + (1.0 - alpha) * self._smooth_k
        self._smooth_d = alpha * d + (1.0 - alpha) * self._smooth_d
        smooth_j = j  # simplified
        if not self._has_prev:
            self._prev_k = self._smooth_k
            self._prev_d = self._smooth_d
            self._has_prev = True
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_k = self._smooth_k
            self._prev_d = self._smooth_d
            return
        cross_up = self._prev_k <= self._prev_d and self._smooth_k > self._smooth_d
        cross_down = self._prev_k >= self._prev_d and self._smooth_k < self._smooth_d
        buy_signal = smooth_j < float(self._buy_level.Value) and cross_up
        sell_signal = smooth_j > float(self._sell_level.Value) and cross_down
        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._prev_k = self._smooth_k
        self._prev_d = self._smooth_d

    def CreateClone(self):
        return adaptive_kdj_mtf_strategy()
