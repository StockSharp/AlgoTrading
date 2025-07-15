import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, DayOfWeek
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class day_of_week_strategy(Strategy):
    """
    Implementation of Day of Week trading strategy.
    The strategy enters long position on Monday and short position on Friday.
    """

    def __init__(self):
        super(day_of_week_strategy, self).__init__()

        # Initialize strategy parameters
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection")

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
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
        Called when the strategy starts.
        """
        super(day_of_week_strategy, self).OnStarted(time)

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
            Unit(0, UnitTypes.Absolute),  # No take profit
            Unit(self.stop_loss_percent, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, ma_value):
        """
        Processes candles and executes trading logic.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        current_day = candle.OpenTime.DayOfWeek

        # Monday - BUY signal
        if current_day == DayOfWeek.Monday and self.Position <= 0 and candle.ClosePrice > ma_value:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self.LogInfo("Buy signal on Monday: Price={0}, MA={1}, Volume={2}".format(
                candle.ClosePrice, ma_value, volume))
        # Friday - SELL signal
        elif current_day == DayOfWeek.Friday and self.Position >= 0 and candle.ClosePrice < ma_value:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self.LogInfo("Sell signal on Friday: Price={0}, MA={1}, Volume={2}".format(
                candle.ClosePrice, ma_value, volume))
        # Closing conditions
        elif (current_day == DayOfWeek.Friday and self.Position > 0) or \
             (current_day == DayOfWeek.Monday and self.Position < 0):
            self.ClosePosition()
            self.LogInfo("Closing position on {0}: Position={1}".format(current_day, self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return day_of_week_strategy()
