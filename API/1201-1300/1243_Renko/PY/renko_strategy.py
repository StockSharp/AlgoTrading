import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Math
from StockSharp.Messages import DataType, RenkoCandleMessage, Unit, CandleStates
from StockSharp.Algo.Strategies import Strategy


class renko_strategy(Strategy):
    def __init__(self):
        super(renko_strategy, self).__init__()
        self._box_size = self.Param("BoxSize", 10.0).SetGreaterThanZero()
        self._previous_direction = 0

    def _candle_type(self):
        return DataType.Create(clr.GetClrType(RenkoCandleMessage), Unit(float(self._box_size.Value)))

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type())]

    def OnReseted(self):
        super(renko_strategy, self).OnReseted()
        self._previous_direction = 0

    def OnStarted2(self, time):
        super(renko_strategy, self).OnStarted2(time)
        self.SubscribeCandles(self._candle_type()).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        open_price = float(candle.OpenPrice)
        close_price = float(candle.ClosePrice)
        current_direction = 1 if close_price > open_price else (-1 if close_price < open_price else 0)

        if current_direction == 0:
            return

        signal = self.get_signal(self._previous_direction, current_direction)

        if signal > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif signal < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

        self._previous_direction = current_direction

    @staticmethod
    def get_signal(previous_direction, current_direction):
        if previous_direction < 0 and current_direction > 0:
            return 1
        if previous_direction > 0 and current_direction < 0:
            return -1
        return 0

    def CreateClone(self):
        return renko_strategy()
