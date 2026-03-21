import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, Aroon
from StockSharp.Algo.Strategies import Strategy


class bollinger_aroon_strategy(Strategy):
    """Bollinger Bands + Aroon Strategy.
    Buys when price touches lower BB with Aroon Up confirming uptrend.
    Exits when price reaches upper BB or Aroon signals weakness."""

    def __init__(self):
        super(bollinger_aroon_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._bb_length = self.Param("BBLength", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 2.0) \
            .SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._aroon_length = self.Param("AroonLength", 14) \
            .SetDisplay("Aroon Period", "Aroon indicator period", "Aroon")
        self._aroon_confirmation = self.Param("AroonConfirmation", 60.0) \
            .SetDisplay("Aroon Confirmation", "Aroon confirmation level", "Aroon")
        self._aroon_stop = self.Param("AroonStop", 40.0) \
            .SetDisplay("Aroon Stop", "Aroon stop level", "Aroon")

        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def BBLength(self):
        return self._bb_length.Value
    @property
    def BBMultiplier(self):
        return self._bb_multiplier.Value
    @property
    def AroonLength(self):
        return self._aroon_length.Value
    @property
    def AroonConfirmation(self):
        return self._aroon_confirmation.Value
    @property
    def AroonStop(self):
        return self._aroon_stop.Value

    def OnReseted(self):
        super(bollinger_aroon_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bollinger_aroon_strategy, self).OnStarted(time)

        bb = BollingerBands()
        bb.Length = self.BBLength
        bb.Width = self.BBMultiplier

        aroon = Aroon()
        aroon.Length = self.AroonLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bb, aroon, self.OnProcess).Start()

    def OnProcess(self, candle, bb_value, aroon_value):
        if candle.State != CandleStates.Finished:
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        if aroon_value.Up is None:
            return

        lower_band = float(bb_value.LowBand)
        upper_band = float(bb_value.UpBand)
        aroon_up = float(aroon_value.Up)
        close = float(candle.ClosePrice)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        if close <= lower_band and aroon_up > self.AroonConfirmation and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = 12
        elif close >= upper_band and aroon_up < self.AroonStop and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = 12
        elif self.Position > 0 and (close >= upper_band or aroon_up < self.AroonStop):
            self.SellMarket()
            self._cooldown_remaining = 12
        elif self.Position < 0 and (close <= lower_band or aroon_up > self.AroonConfirmation):
            self.BuyMarket()
            self._cooldown_remaining = 12

    def CreateClone(self):
        return bollinger_aroon_strategy()
