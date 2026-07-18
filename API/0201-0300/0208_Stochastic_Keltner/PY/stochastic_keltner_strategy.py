import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, KeltnerChannels, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class stochastic_keltner_strategy(Strategy):
    """
    Strategy based on Stochastic Oscillator and Keltner Channels indicators
    """

    def __init__(self):
        super(stochastic_keltner_strategy, self).__init__()

        self._stochPeriod = self.Param("StochPeriod", 14) \
            .SetRange(5, 30) \
            .SetDisplay("Stoch Period", "Period for Stochastic Oscillator", "Stochastic")

        self._stochK = self.Param("StochK", 3) \
            .SetRange(1, 10) \
            .SetDisplay("Stoch %K", "Stochastic %K smoothing period", "Stochastic")

        self._stochD = self.Param("StochD", 3) \
            .SetRange(1, 10) \
            .SetDisplay("Stoch %D", "Stochastic %D smoothing period", "Stochastic")

        self._emaPeriod = self.Param("EmaPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("EMA Period", "EMA period for Keltner Channel", "Keltner")

        self._keltnerMultiplier = self.Param("KeltnerMultiplier", 2.0) \
            .SetRange(1.0, 4.0) \
            .SetDisplay("K Multiplier", "Multiplier for Keltner Channel", "Keltner")

        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetRange(7, 28) \
            .SetDisplay("ATR Period", "ATR period for Keltner Channel and stop-loss", "Risk Management")

        self._atrMultiplier = self.Param("AtrMultiplier", 2.0) \
            .SetRange(1.0, 4.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")

        self._cooldownBars = self.Param("CooldownBars", 40) \
            .SetRange(1, 200) \
            .SetDisplay("Cooldown Bars", "Bars between entries", "General")

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_stoch_k = 50.0
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candleType.Value

    def OnReseted(self):
        super(stochastic_keltner_strategy, self).OnReseted()
        self._prev_stoch_k = 50.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(stochastic_keltner_strategy, self).OnStarted2(time)
        self._prev_stoch_k = 50.0
        self._cooldown = 0

        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stochPeriod.Value
        stochastic.D.Length = self._stochD.Value

        keltner = KeltnerChannels()
        keltner.Length = self._emaPeriod.Value

        atr = AverageTrueRange()
        atr.Length = self._atrPeriod.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(keltner, stochastic, atr, self.ProcessIndicators).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, keltner)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, keltner_value, stoch_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        price = float(candle.ClosePrice)

        upper = keltner_value.Upper
        lower = keltner_value.Lower
        if upper is None or lower is None:
            return

        upper_band = float(upper)
        lower_band = float(lower)

        stoch_k_val = stoch_value.K
        if stoch_k_val is None:
            return

        stoch_k = float(stoch_k_val)

        crossed_below_20 = self._prev_stoch_k >= 20 and stoch_k < 20
        crossed_above_80 = self._prev_stoch_k <= 80 and stoch_k > 80
        self._prev_stoch_k = stoch_k

        if self._cooldown > 0:
            self._cooldown -= 1

        cooldown_val = int(self._cooldownBars.Value)

        if self._cooldown == 0 and crossed_below_20 and price <= lower_band * 1.001 and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = cooldown_val
        elif self._cooldown == 0 and crossed_above_80 and price >= upper_band * 0.999 and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = cooldown_val
        elif self.Position > 0 and crossed_above_80:
            self.SellMarket(self.Position)
            self._cooldown = cooldown_val
        elif self.Position < 0 and crossed_below_20:
            self.BuyMarket(abs(self.Position))
            self._cooldown = cooldown_val

    def CreateClone(self):
        return stochastic_keltner_strategy()
