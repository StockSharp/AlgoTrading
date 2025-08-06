import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, DayOfWeek
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class turnaround_tuesday_strategy(Strategy):
    """
    Implementation of Turnaround Tuesday trading strategy.
    The strategy enters long position on Tuesday after a price decline on Monday.

    """

    def __init__(self):
        super(turnaround_tuesday_strategy, self).__init__()

        # Initialize strategy parameters
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection")

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")

        self._candle_type = self.Param("CandleType", tf(1*1440)) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")

        # Internal state
        self._prev_close_price = 0.0
        self._is_price_lower_on_monday = False

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
        super(turnaround_tuesday_strategy, self).OnReseted()
        self._prev_close_price = 0.0
        self._is_price_lower_on_monday = False

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(turnaround_tuesday_strategy, self).OnStarted(time)
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
            takeProfit=Unit(0),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, ma_value):
        """Process each finished candle."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        current_day = candle.OpenTime.DayOfWeek

        # Record Monday's price action
        if current_day == DayOfWeek.Monday:
            # Check if Monday's close is lower than the previous candle's close
            self._is_price_lower_on_monday = candle.ClosePrice < self._prev_close_price
            self.LogInfo("Monday candle: Close={0}, Prev Close={1}, Lower={2}".format(
                candle.ClosePrice, self._prev_close_price, self._is_price_lower_on_monday))
        # Tuesday - BUY signal if Monday's close was lower
        elif (current_day == DayOfWeek.Tuesday and self._is_price_lower_on_monday and
              candle.ClosePrice > ma_value and self.Position <= 0):
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self.LogInfo("Buy signal on Tuesday after Monday decline: Price={0}, MA={1}, Volume={2}".format(
                candle.ClosePrice, ma_value, volume))
        # Closing conditions - close long position on Friday
        elif current_day == DayOfWeek.Friday and self.Position > 0:
            self.ClosePosition()
            self.LogInfo("Closing position on Friday: Position={0}".format(self.Position))

        # Store current close price for next candle
        self._prev_close_price = float(candle.ClosePrice)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return turnaround_tuesday_strategy()
