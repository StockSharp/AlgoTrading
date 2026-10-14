import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class average_force_strategy(Strategy):
    """
    Average Force strategy.
    The raw force is the position of the close inside the highest high and lowest low of the last Period candles, centred on zero
    ((close - lowest) / (highest - lowest) - 0.5), and the Average Force is its SMA over Smooth candles. A positive Average
    Force holds a long and a negative one holds a short, so the position reverses whenever it crosses zero.
    """

    def __init__(self):
        super(average_force_strategy, self).__init__()
        self._period = self.Param("Period", 18) \
            .SetGreaterThanZero() \
            .SetDisplay("Period", "Lookback of the highest high and lowest low", "Indicator")
        self._smooth = self.Param("Smooth", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Smooth", "SMA period of the force", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._force_average = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(average_force_strategy, self).OnReseted()
        self._force_average = None

    def OnStarted2(self, time):
        super(average_force_strategy, self).OnStarted2(time)

        highest = Highest()
        highest.Length = self._period.Value
        lowest = Lowest()
        lowest.Length = self._period.Value
        self._force_average = SimpleMovingAverage()
        self._force_average.Length = self._smooth.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        highest = float(highest_value)
        lowest = float(lowest_value)
        value_range = highest - lowest
        force = (float(candle.ClosePrice) - lowest) / value_range - 0.5 if value_range > 0 else 0.0
        average_force = float(to_decimal(process_float(self._force_average, force, candle.ServerTime, True)))

        if not self._force_average.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if average_force > 0 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif average_force < 0 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return average_force_strategy()
