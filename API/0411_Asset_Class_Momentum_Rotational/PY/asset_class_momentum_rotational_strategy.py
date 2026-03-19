import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import RateOfChange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class asset_class_momentum_rotational_strategy(Strategy):
    """
    Momentum rotation strategy using ROC and SMA trend filter.
    """

    def __init__(self):
        super(asset_class_momentum_rotational_strategy, self).__init__()

        self._roc_length = self.Param("RocLength", 14) \
            .SetDisplay("ROC Length", "Rate of change lookback", "Parameters")
        self._sma_period = self.Param("SmaPeriod", 30) \
            .SetDisplay("SMA Period", "SMA period for trend filter", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Candle type used for momentum", "General")

        self._cooldown_remaining = 0

    @property
    def RocLength(self): return self._roc_length.Value
    @RocLength.setter
    def RocLength(self, v): self._roc_length.Value = v
    @property
    def SmaPeriod(self): return self._sma_period.Value
    @SmaPeriod.setter
    def SmaPeriod(self, v): self._sma_period.Value = v
    @property
    def CooldownBars(self): return self._cooldown_bars.Value
    @CooldownBars.setter
    def CooldownBars(self, v): self._cooldown_bars.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(asset_class_momentum_rotational_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(asset_class_momentum_rotational_strategy, self).OnStarted(time)

        roc = RateOfChange()
        roc.Length = self.RocLength
        sma = SimpleMovingAverage()
        sma.Length = self.SmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(roc, sma, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, roc_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)

        if roc_value > 0 and close > sma_value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = self.CooldownBars
        elif roc_value < 0 and close < sma_value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = self.CooldownBars

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return asset_class_momentum_rotational_strategy()
