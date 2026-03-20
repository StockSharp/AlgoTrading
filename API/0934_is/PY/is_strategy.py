import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class is_strategy(Strategy):
    def __init__(self):
        super(is_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles used by the strategy", "General")
        self._reverse = self.Param("Reverse", False) \
            .SetDisplay("Reverse", "Reverse trading direction", "General")
        self._enable_short = self.Param("EnableShort", True) \
            .SetDisplay("Sell On", "Enable short selling", "General")
        self._previous_value = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(is_strategy, self).OnReseted()
        self._previous_value = 0.0

    def OnStarted(self, time):
        super(is_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        ii = 2.0 if self._reverse.Value else 1.0
        i2 = 1.0 if self._reverse.Value else 2.0
        prev = self._previous_value
        if close == ii and prev != ii:
            self.BuyMarket()
        elif close == i2 and prev != i2:
            if self.Position > 0:
                self.SellMarket()
        if self._enable_short.Value:
            if close == i2 and prev != i2:
                self.SellMarket()
            elif close == ii and prev != ii:
                if self.Position < 0:
                    self.BuyMarket()
        self._previous_value = close

    def CreateClone(self):
        return is_strategy()
