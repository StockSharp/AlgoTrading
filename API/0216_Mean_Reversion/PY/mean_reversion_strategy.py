import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class mean_reversion_strategy(Strategy):
    """
    Statistical Mean Reversion strategy.
    Enters long when price falls below the mean by a specified number of standard deviations.
    Enters short when price rises above the mean by a specified number of standard deviations.
    Exits positions when price returns to the mean.
    """

    def __init__(self):
        """Constructor."""
        super(mean_reversion_strategy, self).__init__()

        self._movingAveragePeriod = self.Param("MovingAveragePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period for moving average calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviationMultiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for entry signals", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._ma = None
        self._stdDev = None

    @property
    def MovingAveragePeriod(self):
        """Moving average period parameter."""
        return self._movingAveragePeriod.Value

    @MovingAveragePeriod.setter
    def MovingAveragePeriod(self, value):
        self._movingAveragePeriod.Value = value

    @property
    def DeviationMultiplier(self):
        """Standard deviation multiplier parameter."""
        return self._deviationMultiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviationMultiplier.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage parameter."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def CandleType(self):
        """Candle type parameter."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        """See base class for details."""
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(mean_reversion_strategy, self).OnReseted()
        self._ma = None
        self._stdDev = None

    def OnStarted(self, time):
        super(mean_reversion_strategy, self).OnStarted(time)

        # Initialize indicators
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MovingAveragePeriod
        self._stdDev = StandardDeviation()
        self._stdDev.Length = self.MovingAveragePeriod

        # Create candles subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to subscription
        subscription.Bind(self._ma, self._stdDev, self.ProcessCandle).Start()

        # Enable position protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, maValue, stdDevValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate upper and lower bands based on mean and standard deviation
        upperBand = maValue + (stdDevValue * self.DeviationMultiplier)
        lowerBand = maValue - (stdDevValue * self.DeviationMultiplier)

        # Trading logic
        if candle.ClosePrice < lowerBand:
            # Long signal: Price below lower band (mean - k*stdDev)
            if self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Long Entry: Price({0}) < Lower Band({1:F2})".format(candle.ClosePrice, lowerBand))
        elif candle.ClosePrice > upperBand:
            # Short signal: Price above upper band (mean + k*stdDev)
            if self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Short Entry: Price({0}) > Upper Band({1:F2})".format(candle.ClosePrice, upperBand))
        elif ((self.Position > 0 and candle.ClosePrice > maValue) or
              (self.Position < 0 and candle.ClosePrice < maValue)):
            # Exit signals: Price returned to the mean
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Long: Price({0}) > MA({1:F2})".format(candle.ClosePrice, maValue))
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Short: Price({0}) < MA({1:F2})".format(candle.ClosePrice, maValue))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return mean_reversion_strategy()
