import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class first_day_of_month_strategy(Strategy):
    """
    Implementation of First Day of Month trading strategy.
    The strategy enters long position on the 1st day of the month and exits on the 5th day.

    """

    def __init__(self):
        super(first_day_of_month_strategy, self).__init__()

        # Initialize strategy parameters
        self._stop_loss_percent = self.Param("StopLossPercent", 2) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection")

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")

        self._candle_type = self.Param("CandleType", tf(1*1440)) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")

        self._sma = None

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

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(first_day_of_month_strategy, self).OnReseted()
        self._sma = None

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(first_day_of_month_strategy, self).OnStarted(time)

        # Create a simple moving average indicator
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.ma_period

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, ma_value):
        """Process candle and execute trading logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        day_of_month = candle.OpenTime.Day

        # Enter position on the 1st day of the month if price is above MA
        if day_of_month == 1 and candle.ClosePrice > ma_value and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo(
                "Buy signal on first day of month: Price={0}, MA={1}, Volume={2}".format(
                    candle.ClosePrice, ma_value, volume
                )
            )
        # Exit position on the 5th day of the month
        elif day_of_month == 5 and self.Position > 0:
            self.ClosePosition()
            self.LogInfo(
                "Closing position on day 5 of month: Position={0}".format(self.Position)
            )

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return first_day_of_month_strategy()
