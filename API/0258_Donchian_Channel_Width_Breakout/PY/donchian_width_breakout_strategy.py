import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class donchian_width_breakout_strategy(Strategy):
    """
    Strategy that trades on Donchian Channel width breakouts.
    When Donchian Channel width increases significantly above its average,
    it enters position in the direction determined by price movement.
    """

    def __init__(self):
        super(donchian_width_breakout_strategy, self).__init__()

        self._donchianPeriod = self.Param("DonchianPeriod", 20) \
            .SetDisplay("Donchian Period", "Period for the Donchian Channel", "Indicators")

        self._widthThreshold = self.Param("WidthThreshold", 1.2) \
            .SetDisplay("Width Threshold", "Threshold multiplier for width breakout", "Trading")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._widthAverage = None

    @property
    def DonchianPeriod(self):
        return self._donchianPeriod.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchianPeriod.Value = value

    @property
    def WidthThreshold(self):
        return self._widthThreshold.Value

    @WidthThreshold.setter
    def WidthThreshold(self, value):
        self._widthThreshold.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        super(donchian_width_breakout_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(donchian_width_breakout_strategy, self).OnStarted(time)

        highest = Highest()
        highest.Length = self.DonchianPeriod
        lowest = Lowest()
        lowest.Length = self.DonchianPeriod
        donchian_period = self.DonchianPeriod
        self._widthAverage = SimpleMovingAverage()
        self._widthAverage.Length = max(5, donchian_period // 2)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        width = float(highest_value) - float(lowest_value)
        if width <= 0:
            return

        avg_result = process_float(self._widthAverage, width, candle.ServerTime, True)

        if not self._widthAverage.IsFormed:
            return

        avg_width = float(avg_result)
        if avg_width <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        middle_channel = (float(highest_value) + float(lowest_value)) / 2.0

        # Width breakout detection
        if width > avg_width * self.WidthThreshold and self.Position == 0:
            if float(candle.ClosePrice) > middle_channel:
                self.BuyMarket()
            elif float(candle.ClosePrice) < middle_channel:
                self.SellMarket()

    def CreateClone(self):
        return donchian_width_breakout_strategy()
