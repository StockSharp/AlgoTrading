import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KeltnerChannels, WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class keltner_williams_r_strategy(Strategy):
    """Strategy based on Keltner Channels and Williams %R indicators"""

    def __init__(self):
        super(keltner_williams_r_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("EMA Period", "EMA period for Keltner Channel", "Indicators")

        self._keltner_multiplier = self.Param("KeltnerMultiplier", 2.0) \
            .SetRange(1.0, 4.0) \
            .SetDisplay("K Multiplier", "Multiplier for Keltner Channel", "Indicators")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetRange(7, 28) \
            .SetDisplay("ATR Period", "ATR period for Keltner Channel", "Indicators")

        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetRange(5, 30) \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")

        self._cooldown_bars = self.Param("CooldownBars", 40) \
            .SetRange(1, 200) \
            .SetDisplay("Cooldown Bars", "Bars between entries", "General")

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_williams_r = 0.0
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(keltner_williams_r_strategy, self).OnReseted()
        self._prev_williams_r = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(keltner_williams_r_strategy, self).OnStarted2(time)
        self._prev_williams_r = 0.0
        self._cooldown = 0

        keltner = KeltnerChannels()
        keltner.Length = self._ema_period.Value
        keltner.Multiplier = self._keltner_multiplier.Value

        williams_r = WilliamsR()
        williams_r.Length = self._williams_r_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(keltner, williams_r, self.ProcessIndicators).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, keltner)
            self.DrawIndicator(area, williams_r)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, keltner_value, williams_r_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper = keltner_value.Upper
        lower = keltner_value.Lower
        if upper is None or lower is None:
            return

        upper = float(upper)
        lower = float(lower)

        wr = float(williams_r_value)
        crossed_into_oversold = self._prev_williams_r > -80 and wr <= -80
        crossed_into_overbought = self._prev_williams_r < -20 and wr >= -20
        self._prev_williams_r = wr

        price = float(candle.ClosePrice)

        if self._cooldown > 0:
            self._cooldown -= 1

        cooldown_val = int(self._cooldown_bars.Value)

        if self._cooldown == 0 and price <= lower * 1.001 and crossed_into_oversold and self.Position <= 0:
            volume = self.Volume + abs(self.Position)
            self.BuyMarket(volume)
            self._cooldown = cooldown_val
        elif self._cooldown == 0 and price >= upper * 0.999 and crossed_into_overbought and self.Position >= 0:
            volume = self.Volume + abs(self.Position)
            self.SellMarket(volume)
            self._cooldown = cooldown_val

    def CreateClone(self):
        return keltner_williams_r_strategy()
