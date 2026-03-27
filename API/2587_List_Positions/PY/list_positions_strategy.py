import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class list_positions_strategy(Strategy):
    def __init__(self):
        super(list_positions_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._log_interval = self.Param("LogInterval", 10)

        self._candle_count = 0
        self._prev_close = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LogInterval(self):
        return self._log_interval.Value

    @LogInterval.setter
    def LogInterval(self, value):
        self._log_interval.Value = value

    def OnStarted(self, time):
        super(list_positions_strategy, self).OnStarted(time)

        self._candle_count = 0
        self._prev_close = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormed:
            return

        close = float(candle.ClosePrice)

        self._candle_count += 1

        if self._prev_close is not None:
            if close > self._prev_close and self.Position <= 0:
                self.BuyMarket()
            elif close < self._prev_close and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close

    def OnReseted(self):
        super(list_positions_strategy, self).OnReseted()
        self._candle_count = 0
        self._prev_close = None

    def CreateClone(self):
        return list_positions_strategy()
