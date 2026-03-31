import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class adaptive_market_level_strategy(Strategy):

    def __init__(self):
        super(adaptive_market_level_strategy, self).__init__()

        self._period = self.Param("Period", 14) \
            .SetDisplay("Period", "SMA period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_sma = 0.0
        self._prev_prev_sma = 0.0
        self._count = 0

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(adaptive_market_level_strategy, self).OnStarted2(time)

        sma = SimpleMovingAverage()
        sma.Length = self.Period

        self.SubscribeCandles(self.CandleType) \
            .Bind(sma, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        sma_val = float(sma_value)
        self._count += 1

        if self._count < 3:
            self._prev_prev_sma = self._prev_sma
            self._prev_sma = sma_val
            return

        turn_up = self._prev_sma < self._prev_prev_sma and sma_val > self._prev_sma
        turn_down = self._prev_sma > self._prev_prev_sma and sma_val < self._prev_sma

        if turn_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif turn_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_sma = self._prev_sma
        self._prev_sma = sma_val

    def OnReseted(self):
        super(adaptive_market_level_strategy, self).OnReseted()
        self._prev_sma = 0.0
        self._prev_prev_sma = 0.0
        self._count = 0

    def CreateClone(self):
        return adaptive_market_level_strategy()
