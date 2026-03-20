import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_hma_reversal_strategy(Strategy):

    def __init__(self):
        super(color_hma_reversal_strategy, self).__init__()

        self._hma_period = self.Param("HmaPeriod", 13) \
            .SetDisplay("HMA Period", "Hull Moving Average period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_value1 = 0.0
        self._prev_value2 = 0.0
        self._count = 0

    @property
    def HmaPeriod(self):
        return self._hma_period.Value

    @HmaPeriod.setter
    def HmaPeriod(self, value):
        self._hma_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(color_hma_reversal_strategy, self).OnStarted(time)

        self._count = 0

        hma = HullMovingAverage()
        hma.Length = self.HmaPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(hma, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, hma_value):
        if candle.State != CandleStates.Finished:
            return

        val = float(hma_value)
        self._count += 1

        if self._count <= 2:
            self._prev_value2 = self._prev_value1
            self._prev_value1 = val
            return

        was_falling = self._prev_value1 < self._prev_value2
        was_rising = self._prev_value1 > self._prev_value2
        now_rising = val > self._prev_value1
        now_falling = val < self._prev_value1

        if was_falling and now_rising and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif was_rising and now_falling and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_value2 = self._prev_value1
        self._prev_value1 = val

    def OnReseted(self):
        super(color_hma_reversal_strategy, self).OnReseted()
        self._prev_value1 = 0.0
        self._prev_value2 = 0.0
        self._count = 0

    def CreateClone(self):
        return color_hma_reversal_strategy()
