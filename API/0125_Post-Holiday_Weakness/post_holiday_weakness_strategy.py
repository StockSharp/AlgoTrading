import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class post_holiday_weakness_strategy(Strategy):
    """
    Implementation of Post-Holiday Weakness trading strategy.
    The strategy enters short position after a holiday and exits on Friday.
    """

    def __init__(self):
        super(post_holiday_weakness_strategy, self).__init__()

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection")

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")

        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")

        # Dictionary of common holidays (month, day)
        self._holidays = {
            # US Holidays (approximate dates, some holidays like Easter vary)
            (1, 1): "New Year's Day",
            (7, 4): "Independence Day",
            (12, 25): "Christmas",
            (11, 25): "Thanksgiving",  # Approximate (4th Thursday in November)
            (5, 31): "Memorial Day",  # Approximate (last Monday in May)
            (9, 4): "Labor Day",          # Approximate (first Monday in September)
            # Add more holidays as needed
        }

        self._in_post_holiday_position = False

    @property
    def stop_loss_percent(self):
        """Stop loss percentage from entry price."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def ma_period(self):
        """Moving average period."""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super(post_holiday_weakness_strategy, self).OnStarted(time)

        # Create a simple moving average indicator
        sma = SimpleMovingAverage()
        sma.Length = self.ma_period

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0),  # No take profit
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, ma_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        date = candle.OpenTime
        yesterday = date.AddDays(-1)
        day_of_week = date.DayOfWeek

        was_yesterday_holiday = self.IsHoliday(yesterday)

        # Enter position after holiday
        if (was_yesterday_holiday and not self._in_post_holiday_position
                and candle.ClosePrice < ma_value and self.Position >= 0):
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self._in_post_holiday_position = True

            holiday_name = self.GetHolidayName(yesterday)
            self.LogInfo(
                "Sell signal after holiday {0}: Date={1:yyyy-MM-dd}, Price={2}, MA={3}, Volume={4}".format(
                    holiday_name, date, candle.ClosePrice, ma_value, volume))
        # Exit position on Friday
        elif (self._in_post_holiday_position and day_of_week == DayOfWeek.Friday and self.Position < 0):
            self.ClosePosition()

            self._in_post_holiday_position = False

            self.LogInfo(
                "Closing position on Friday: Date={0:yyyy-MM-dd}, Position={1}".format(
                    date, self.Position))

    def IsHoliday(self, date):
        return (date.Month, date.Day) in self._holidays

    def GetHolidayName(self, date):
        return self._holidays.get((date.Month, date.Day), "Unknown Holiday")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return post_holiday_weakness_strategy()
