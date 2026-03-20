import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class currency_strength_v11_strategy(Strategy):
    def __init__(self):
        super(currency_strength_v11_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._difference_threshold = self.Param("DifferenceThreshold", 0.2)

        self._prev_change = None
        self._prev_momentum = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def DifferenceThreshold(self):
        return self._difference_threshold.Value

    @DifferenceThreshold.setter
    def DifferenceThreshold(self, value):
        self._difference_threshold.Value = value

    def OnStarted(self, time):
        super(currency_strength_v11_strategy, self).OnStarted(time)

        self._prev_change = None
        self._prev_momentum = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        open_price = float(candle.OpenPrice)
        close = float(candle.ClosePrice)

        change = (close - open_price) / open_price * 100.0 if open_price != 0.0 else 0.0

        if self._prev_change is None:
            self._prev_change = change
            return

        momentum = change - self._prev_change
        threshold = float(self.DifferenceThreshold)

        long_signal = (self._prev_momentum is not None and
                       self._prev_momentum <= threshold and
                       momentum > threshold)

        short_signal = (self._prev_momentum is not None and
                        self._prev_momentum >= -threshold and
                        momentum < -threshold)

        if long_signal and self.Position <= 0:
            self.BuyMarket()
        elif short_signal and self.Position >= 0:
            self.SellMarket()

        self._prev_change = change
        self._prev_momentum = momentum

    def OnReseted(self):
        super(currency_strength_v11_strategy, self).OnReseted()
        self._prev_change = None
        self._prev_momentum = None

    def CreateClone(self):
        return currency_strength_v11_strategy()
