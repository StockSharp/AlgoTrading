import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class asset_class_momentum_rotational_strategy(Strategy):
    """Momentum rotation strategy using ROC and SMA trend filter."""

    def __init__(self):
        super(asset_class_momentum_rotational_strategy, self).__init__()

        self._roc_length = self.Param("RocLength", 14) \
            .SetDisplay("ROC Length", "Rate of change lookback", "Parameters")
        self._sma_period = self.Param("SmaPeriod", 30) \
            .SetDisplay("SMA Period", "SMA period for trend filter", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle type used for momentum", "General")

        self._cooldown_remaining = 0

    @property
    def RocLength(self):
        return self._roc_length.Value
    @property
    def SmaPeriod(self):
        return self._sma_period.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

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

    def ProcessCandle(self, candle, roc_val, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)
        rv = float(roc_val)
        sv = float(sma_val)

        if rv > 0 and close > sv and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars
        elif rv < 0 and close < sv and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars

    def CreateClone(self):
        return asset_class_momentum_rotational_strategy()
