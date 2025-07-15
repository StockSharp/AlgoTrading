import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, ICandleMessage, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class pre_holiday_strength_strategy(Strategy):
    """
    Implementation of Pre-Holiday Strength trading strategy.
    The strategy enters long position before a holiday and exits after the holiday.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(pre_holiday_strength_strategy, self).__init__()

        # Dictionary of common holidays (month, day)
        self._holidays = {
            # US Holidays (approximate dates, some holidays like Easter vary)
            (1, 1): "New Year's Day",
            (7, 4): "Independence Day",
            (12, 25): "Christmas",
            (11, 25): "Thanksgiving",  # Approximate (4th Thursday in November)
            (5, 31): "Memorial Day",  # Approximate (last Monday in May)
            (9, 4): "Labor Day",      # Approximate (first Monday in September)
            # Add more holidays as needed
        }

        self._inPreHolidayPosition = False

        # Initialize strategy parameters
        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection")

        self._maPeriod = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))) \
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

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(pre_holiday_strength_strategy, self).OnReseted()
        self._inPreHolidayPosition = False

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(pre_holiday_strength_strategy, self).OnStarted(time)

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

    def ProcessCandle(self, candle, ma_value):
        """Process candle and execute trading logic"""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        date = candle.OpenTime
        tomorrow = date.AddDays(1)

        isTomorrowHoliday = self.IsHoliday(tomorrow)
        isToday = self.IsHoliday(date)

        # Enter position one day before holiday
        if (isTomorrowHoliday and not self._inPreHolidayPosition and
                candle.ClosePrice > ma_value and self.Position <= 0):
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self._inPreHolidayPosition = True

            holidayName = self.GetHolidayName(tomorrow)
            self.LogInfo(
                "Buy signal before holiday {0}: Date={1:yyyy-MM-dd}, Price={2}, MA={3}, Volume={4}".format(
                    holidayName, date, candle.ClosePrice, ma_value, volume))

        # Exit position after holiday
        elif self._inPreHolidayPosition and isToday and self.Position > 0:
            self.ClosePosition()

            self._inPreHolidayPosition = False

            holidayName = self.GetHolidayName(date)
            self.LogInfo(
                "Closing position after holiday {0}: Date={1:yyyy-MM-dd}, Position={2}".format(
                    holidayName, date, self.Position))

    def IsHoliday(self, date):
        return (date.Month, date.Day) in self._holidays

    def GetHolidayName(self, date):
        if (date.Month, date.Day) in self._holidays:
            return self._holidays[(date.Month, date.Day)]
        return "Unknown Holiday"

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return pre_holiday_strength_strategy()
