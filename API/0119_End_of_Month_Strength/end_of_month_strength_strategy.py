import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class end_of_month_strength_strategy(Strategy):
    """
    Implementation of End of Month Strength trading strategy.
    The strategy enters long position on the 25th day of the month and exits on the 5th day of the next month.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(end_of_month_strength_strategy, self).__init__()

        # Initialize strategy parameters
        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection")

        self._maPeriod = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")

        self._candleType = self.Param("CandleType", tf(1*1440)) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")

    @property
    def StopLossPercent(self):
        """Stop loss percentage from entry price."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def MaPeriod(self):
        """Moving average period."""
        return self._maPeriod.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(end_of_month_strength_strategy, self).OnStarted(time)

        # Create a simple moving average indicator
        sma = SimpleMovingAverage()
        sma.Length = self.MaPeriod

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            Unit(0),  # No take profit
            Unit(self.StopLossPercent, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, maValue):
        """Process finished candle and execute trading logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        dayOfMonth = candle.OpenTime.Day

        # Enter position on the 25th day of the month or later if price is above MA
        if dayOfMonth >= 25 and candle.ClosePrice > maValue and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self.LogInfo(
                "Buy signal at end of month (day {0}): Price={1}, MA={2}, Volume={3}".format(
                    dayOfMonth, candle.ClosePrice, maValue, volume))
        # Exit position on the 5th day of the month (beginning of the next month)
        elif dayOfMonth == 5 and self.Position > 0:
            self.ClosePosition()
            self.LogInfo(
                "Closing position on day 5 of month: Position={0}".format(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return end_of_month_strength_strategy()
