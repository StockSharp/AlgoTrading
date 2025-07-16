import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, DayOfWeek, DateTimeOffset
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class quarterly_expiry_strategy(Strategy):
    """
    Implementation of Quarterly Expiry trading strategy.
    The strategy trades on quarterly expiration days based on price relative to MA.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(quarterly_expiry_strategy, self).__init__()

        # Initialize strategy parameters
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection")

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")

        self._candle_type = self.Param("CandleType", tf(1*1440)) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")

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

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(quarterly_expiry_strategy, self).OnStarted(time)

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
        """
        Processes each finished candle and executes trading logic for quarterly expiry days.

        :param candle: The processed candle message.
        :param ma_value: The current value of the moving average.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        date = candle.OpenTime
        day_of_week = date.DayOfWeek

        # Check if this is a quarterly expiry day
        # Typically the third Friday of March, June, September, and December
        if self._is_quarterly_expiry_day(date):
            # BUY signal - price above MA
            if candle.ClosePrice > ma_value and self.Position <= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)
                self.LogInfo("Buy signal on quarterly expiry day: Date={0:yyyy-MM-dd}, Price={1}, MA={2}, Volume={3}".format(
                    date, candle.ClosePrice, ma_value, volume))
            # SELL signal - price below MA
            elif candle.ClosePrice < ma_value and self.Position >= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)
                self.LogInfo("Sell signal on quarterly expiry day: Date={0:yyyy-MM-dd}, Price={1}, MA={2}, Volume={3}".format(
                    date, candle.ClosePrice, ma_value, volume))
        # Exit position on Friday (if we're not already on a Friday)
        elif day_of_week == DayOfWeek.Friday and self.Position != 0:
            self.ClosePosition()
            self.LogInfo("Closing position on Friday: Date={0:yyyy-MM-dd}, Position={1}".format(
                date, self.Position))

    def _is_quarterly_expiry_day(self, date):
        """
        Check if the given date is the quarterly expiry day.
        """
        # Check if it's a Friday
        if date.DayOfWeek != DayOfWeek.Friday:
            return False

        # Check if it's March, June, September, or December
        month = date.Month
        if month != 3 and month != 6 and month != 9 and month != 12:
            return False

        # Check if it's the third Friday of the month
        # Find the first day of the month
        first_day = DateTimeOffset(date.Year, date.Month, 1, 0, 0, 0, date.Offset)

        # Find the first Friday
        days_until_first_friday = ((DayOfWeek.Friday - first_day.DayOfWeek + 7) % 7)
        first_friday = first_day.AddDays(days_until_first_friday)

        # Calculate the third Friday
        third_friday = first_friday.AddDays(14)

        # Check if the date is the third Friday
        return date.Day == third_friday.Day

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return quarterly_expiry_strategy()
