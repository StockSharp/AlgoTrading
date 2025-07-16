import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import UnitTypes, Unit, DataType, CandleStates, ICandleMessage
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class santa_claus_rally_strategy(Strategy):
    """
    Implementation of Santa Claus Rally trading strategy.
    The strategy enters long position between December 20 and December 31,
    and exits on January 5 of the following year.

    """

    def __init__(self):
        super(santa_claus_rally_strategy, self).__init__()

        # Initialize strategy parameters
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection")

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")

        self._candle_type = self.Param("CandleType", tf(1*1440)) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(santa_claus_rally_strategy, self).OnReseted()

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(santa_claus_rally_strategy, self).OnStarted(time)

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
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, ma_value):
        """
        Process candle and execute trading logic

        :param candle: The candle message.
        :param ma_value: The moving average value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        date = candle.OpenTime
        month = date.Month
        day = date.Day

        # Santa Claus Rally period - Dec 20 to Dec 31
        if month == 12 and day >= 20 and day <= 31 and self.Position <= 0 and candle.ClosePrice > ma_value:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal for Santa Claus Rally: Date={0:yyyy-MM-dd}, Price={1}, MA={2}, Volume={3}".format(
                date, candle.ClosePrice, ma_value, volume))
        # Exit position on January 5
        elif month == 1 and day == 5 and self.Position > 0:
            self.ClosePosition()
            self.LogInfo("Closing position on January 5: Position={0}".format(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return santa_claus_rally_strategy()
