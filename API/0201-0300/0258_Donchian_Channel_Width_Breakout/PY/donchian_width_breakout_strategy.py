import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class donchian_width_breakout_strategy(Strategy):
    """
    Donchian Channel width breakout.
    Enters when the channel width exceeds its average by a standard deviation multiplier,
    in the direction of the close relative to the channel middle.
    Exits when the width falls back below its average.
    """

    def __init__(self):
        super(donchian_width_breakout_strategy, self).__init__()

        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Donchian Period", "Period of the Donchian Channel", "Indicators")
        self._avg_period = self.Param("AvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for width average and deviation", "Strategy")
        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Standard deviation multiplier for breakout", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")

        self._width_average = None
        self._width_std_dev = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(donchian_width_breakout_strategy, self).OnReseted()
        self._width_average = None
        self._width_std_dev = None

    def OnStarted2(self, time):
        super(donchian_width_breakout_strategy, self).OnStarted2(time)

        highest = Highest()
        highest.Length = self._donchian_period.Value
        lowest = Lowest()
        lowest.Length = self._donchian_period.Value
        self._width_average = SimpleMovingAverage()
        self._width_average.Length = self._avg_period.Value
        self._width_std_dev = StandardDeviation()
        self._width_std_dev.Length = self._avg_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self._process_candle).Start()

        stop = float(self._stop_loss.Value)
        self.StartProtection(None, Unit(stop, UnitTypes.Percent) if stop > 0 else None)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        highest_value = float(highest_value)
        lowest_value = float(lowest_value)

        width = highest_value - lowest_value
        avg_width = float(process_float(self._width_average, width, candle.ServerTime, True))
        std_width = float(process_float(self._width_std_dev, width, candle.ServerTime, True))

        if not self._width_average.IsFormed or not self._width_std_dev.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        middle = (highest_value + lowest_value) / 2.0

        if width > avg_width + float(self._multiplier.Value) * std_width:
            if close > middle and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
                return
            if close < middle and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
                return

        if width < avg_width:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)

    def CreateClone(self):
        return donchian_width_breakout_strategy()
