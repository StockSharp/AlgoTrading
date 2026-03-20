import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class blau_tvi_strategy(Strategy):

    def __init__(self):
        super(blau_tvi_strategy, self).__init__()

        self._length = self.Param("Length", 12) \
            .SetDisplay("Length", "Smoothing length", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._count = 0

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(blau_tvi_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.Length

        self.SubscribeCandles(self.CandleType) \
            .Bind(ema, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        self._count += 1

        if self._count < 3:
            self._prev_prev_ema = self._prev_ema
            self._prev_ema = ema_val
            return

        turn_up = self._prev_ema < self._prev_prev_ema and ema_val > self._prev_ema
        turn_down = self._prev_ema > self._prev_prev_ema and ema_val < self._prev_ema

        if turn_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif turn_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_ema = self._prev_ema
        self._prev_ema = ema_val

    def OnReseted(self):
        super(blau_tvi_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._count = 0

    def CreateClone(self):
        return blau_tvi_strategy()
