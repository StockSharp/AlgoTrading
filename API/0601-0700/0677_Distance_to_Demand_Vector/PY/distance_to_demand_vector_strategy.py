import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class distance_to_demand_vector_strategy(Strategy):
    """
    Distance to demand vector strategy.
    The long vector is the lowest low and the short vector the highest high of the last Length candles.
    When the distance from the close to the long vector rises above the distance to the short vector the strategy goes long,
    and when it falls below it goes short, reversing an opposite position.
    """

    def __init__(self):
        super(distance_to_demand_vector_strategy, self).__init__()
        self._length = self.Param("Length", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Length", "Candles that define the demand vectors", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_diff = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(distance_to_demand_vector_strategy, self).OnReseted()
        self._prev_diff = None

    def OnStarted2(self, time):
        super(distance_to_demand_vector_strategy, self).OnStarted2(time)

        self._prev_diff = None

        highest = Highest()
        highest.Length = self._length.Value
        lowest = Lowest()
        lowest.Length = self._length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(highest, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        if not highest_value.IsFormed or not lowest_value.IsFormed:
            return

        close = candle.ClosePrice
        distance_to_long = close - lowest_value.GetValue[Decimal](None)
        distance_to_short = highest_value.GetValue[Decimal](None) - close
        diff = distance_to_long - distance_to_short

        prev = self._prev_diff
        self._prev_diff = diff

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        zero = Decimal(0)
        if prev <= zero and diff > zero and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif prev >= zero and diff < zero and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return distance_to_demand_vector_strategy()
