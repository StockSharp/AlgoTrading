import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class n_candles_strategy(Strategy):
    def __init__(self):
        super(n_candles_strategy, self).__init__()

        self._consecutive_candles = self.Param("ConsecutiveCandles", 4)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._current_direction = 0
        self._streak_length = 0

    @property
    def ConsecutiveCandles(self):
        return self._consecutive_candles.Value

    @ConsecutiveCandles.setter
    def ConsecutiveCandles(self, value):
        self._consecutive_candles.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(n_candles_strategy, self).OnStarted(time)

        self._current_direction = 0
        self._streak_length = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        direction = 0
        if close > open_price:
            direction = 1
        elif close < open_price:
            direction = -1
        else:
            self._current_direction = 0
            self._streak_length = 0
            return

        consecutive = int(self.ConsecutiveCandles)

        if direction == self._current_direction:
            self._streak_length = min(self._streak_length + 1, consecutive)
        else:
            self._current_direction = direction
            self._streak_length = 1

        if self._streak_length < consecutive:
            return

        if direction > 0 and self.Position <= 0:
            self.BuyMarket()
        elif direction < 0 and self.Position >= 0:
            self.SellMarket()

    def OnReseted(self):
        super(n_candles_strategy, self).OnReseted()
        self._current_direction = 0
        self._streak_length = 0

    def CreateClone(self):
        return n_candles_strategy()
