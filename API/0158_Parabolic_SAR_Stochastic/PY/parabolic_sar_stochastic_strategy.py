import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class parabolic_sar_stochastic_strategy(Strategy):
    """
    Strategy combining Parabolic SAR trend direction with Stochastic entry confirmation.
    """

    def __init__(self):
        super(parabolic_sar_stochastic_strategy, self).__init__()

        self._acceleration_factor = self.Param("AccelerationFactor", 0.02) \
            .SetRange(0.01, 0.2) \
            .SetDisplay("Acceleration Factor", "Initial acceleration factor for SAR", "SAR")
        self._max_acceleration_factor = self.Param("MaxAccelerationFactor", 0.2) \
            .SetRange(0.05, 0.5) \
            .SetDisplay("Max Acceleration Factor", "Maximum acceleration factor for SAR", "SAR")
        self._stoch_k = self.Param("StochK", 3) \
            .SetRange(1, 10) \
            .SetDisplay("Stochastic %K", "Stochastic %K smoothing period", "Stochastic")
        self._stoch_d = self.Param("StochD", 3) \
            .SetRange(1, 10) \
            .SetDisplay("Stochastic %D", "Stochastic %D smoothing period", "Stochastic")
        self._stoch_oversold = self.Param("StochOversold", 20.0) \
            .SetDisplay("Oversold Level", "Stochastic oversold level", "Stochastic")
        self._stoch_overbought = self.Param("StochOverbought", 80.0) \
            .SetDisplay("Overbought Level", "Stochastic overbought level", "Stochastic")
        self._cooldown_bars = self.Param("CooldownBars", 160) \
            .SetRange(5, 500) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._sar_value = 0.0
        self._last_stoch_k = 50.0
        self._is_above_sar = False
        self._has_trend_state = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(parabolic_sar_stochastic_strategy, self).OnStarted(time)
        self._sar_value = 0.0
        self._last_stoch_k = 50.0
        self._is_above_sar = False
        self._has_trend_state = False
        self._cooldown = 0

        sar = ParabolicSar()
        sar.AccelerationStep = self._acceleration_factor.Value
        sar.AccelerationMax = self._max_acceleration_factor.Value

        stoch = StochasticOscillator()
        stoch.K.Length = self._stoch_k.Value
        stoch.D.Length = self._stoch_d.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sar, self.OnSar)
        subscription.BindEx(stoch, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)
            stoch_area = self.CreateChartArea()
            if stoch_area is not None:
                self.DrawIndicator(stoch_area, stoch)

    def OnSar(self, candle, sar_value):
        if candle is None or sar_value is None:
            return
        if candle.State != CandleStates.Finished or not sar_value.IsFormed:
            return
        self._sar_value = float(sar_value)

    def ProcessCandle(self, candle, stoch_value):
        if candle is None or stoch_value is None:
            return
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._sar_value == 0 or not stoch_value.IsFormed:
            return

        if not hasattr(stoch_value, 'K') or stoch_value.K is None:
            return
        stoch_k = float(stoch_value.K)

        close = float(candle.ClosePrice)
        price_above_sar = close > self._sar_value

        if not self._has_trend_state:
            self._is_above_sar = price_above_sar
            self._has_trend_state = True
            self._last_stoch_k = stoch_k
            return

        sar_signal_change = price_above_sar != self._is_above_sar

        if self._cooldown > 0:
            self._cooldown -= 1
            self._last_stoch_k = stoch_k
            self._is_above_sar = price_above_sar
            return

        cd = self._cooldown_bars.Value
        os_level = self._stoch_oversold.Value
        ob_level = self._stoch_overbought.Value

        long_entry = (self.Position == 0 and price_above_sar
                      and self._last_stoch_k <= os_level and stoch_k > self._last_stoch_k)
        short_entry = (self.Position == 0 and not price_above_sar
                       and self._last_stoch_k >= ob_level and stoch_k < self._last_stoch_k)

        if long_entry:
            self.BuyMarket()
            self._cooldown = cd
        elif short_entry:
            self.SellMarket()
            self._cooldown = cd
        elif sar_signal_change and self.Position > 0 and not price_above_sar:
            self.SellMarket()
            self._cooldown = cd
        elif sar_signal_change and self.Position < 0 and price_above_sar:
            self.BuyMarket()
            self._cooldown = cd

        self._last_stoch_k = stoch_k
        self._is_above_sar = price_above_sar

    def OnReseted(self):
        super(parabolic_sar_stochastic_strategy, self).OnReseted()
        self._sar_value = 0.0
        self._last_stoch_k = 50.0
        self._is_above_sar = False
        self._has_trend_state = False
        self._cooldown = 0

    def CreateClone(self):
        return parabolic_sar_stochastic_strategy()
