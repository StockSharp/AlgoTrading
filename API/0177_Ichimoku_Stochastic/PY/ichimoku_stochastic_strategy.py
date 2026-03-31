import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import Ichimoku, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class ichimoku_stochastic_strategy(Strategy):
    """
    Strategy based on Ichimoku Cloud and Stochastic Oscillator indicators.
    """

    def __init__(self):
        super(ichimoku_stochastic_strategy, self).__init__()

        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen line", "Ichimoku")

        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetDisplay("Kijun-sen Period", "Period for Kijun-sen line", "Ichimoku")

        self._senkou_period = self.Param("SenkouPeriod", 52) \
            .SetDisplay("Senkou Span Period", "Period for Senkou Span B line", "Ichimoku")

        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Stochastic Period", "Period for Stochastic Oscillator", "Stochastic")

        self._stoch_k = self.Param("StochK", 3) \
            .SetDisplay("Stochastic %K", "Smoothing for Stochastic %K line", "Stochastic")

        self._stoch_d = self.Param("StochD", 3) \
            .SetDisplay("Stochastic %D", "Period for Stochastic %D line", "Stochastic")

        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetRange(1, 20) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._candle_type = self.Param("CandleType", tf(30)) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")

        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ichimoku_stochastic_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted2(self, time):
        super(ichimoku_stochastic_strategy, self).OnStarted2(time)
        self._cooldown = 0

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_period.Value

        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_k.Value
        stochastic.D.Length = self._stoch_d.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ichimoku, stochastic, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)

            stoch_area = self.CreateChartArea()
            if stoch_area is not None:
                self.DrawIndicator(stoch_area, stochastic)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ichimoku_value, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if ichimoku_value.Tenkan is None:
            return
        tenkan = float(ichimoku_value.Tenkan)

        if ichimoku_value.Kijun is None:
            return
        kijun = float(ichimoku_value.Kijun)

        if ichimoku_value.SenkouA is None:
            return
        senkou_a = float(ichimoku_value.SenkouA)

        if ichimoku_value.SenkouB is None:
            return
        senkou_b = float(ichimoku_value.SenkouB)

        price = float(candle.ClosePrice)

        is_above_kumo = price > max(senkou_a, senkou_b)
        is_below_kumo = price < min(senkou_a, senkou_b)

        is_bullish_cross = tenkan > kijun
        is_bearish_cross = tenkan < kijun

        if stoch_value.K is None:
            return
        stochastic_k = float(stoch_value.K)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cooldown = int(self._cooldown_bars.Value)

        if is_above_kumo and is_bullish_cross and stochastic_k < 15 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._cooldown = cooldown
        elif is_below_kumo and is_bearish_cross and stochastic_k > 85 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._cooldown = cooldown
        elif is_bearish_cross and self.Position > 0:
            self.SellMarket(self.Position)
            self._cooldown = cooldown
        elif is_bullish_cross and self.Position < 0:
            self.BuyMarket(abs(self.Position))
            self._cooldown = cooldown

    def CreateClone(self):
        return ichimoku_stochastic_strategy()
