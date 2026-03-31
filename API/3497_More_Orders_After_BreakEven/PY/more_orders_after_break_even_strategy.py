import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class more_orders_after_break_even_strategy(Strategy):
    def __init__(self):
        super(more_orders_after_break_even_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._period = self.Param("Period", 50)

        self._prev_close = 0.0
        self._prev_tema = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    def OnReseted(self):
        super(more_orders_after_break_even_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_tema = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(more_orders_after_break_even_strategy, self).OnStarted2(time)
        self._prev_close = 0.0
        self._prev_tema = 0.0
        self._has_prev = False

        tema = ExponentialMovingAverage()
        tema.Length = self.Period

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(tema, self._process_candle).Start()

    def _process_candle(self, candle, tema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        tema_val = float(tema_value)

        if self._has_prev:
            if self._prev_close <= self._prev_tema and close > tema_val and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_close >= self._prev_tema and close < tema_val and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_tema = tema_val
        self._has_prev = True

    def CreateClone(self):
        return more_orders_after_break_even_strategy()
