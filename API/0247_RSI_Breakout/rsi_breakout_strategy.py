import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class rsi_breakout_strategy(Strategy):
    """
    RSI Breakout Strategy (247).
    Enter when RSI breaks out above/below its average by a certain multiple of standard deviation.
    Exit when RSI returns to its average.
    """

    def __init__(self):
        super(rsi_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._rsiPeriod = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        self._averagePeriod = self.Param("AveragePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for RSI average calculation", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("StdDev Multiplier", "Standard deviation multiplier for entry", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters")

        # Internal indicators
        self._rsi = None
        self._rsiAverage = None
        self._rsiStdDev = None

        # State variables
        self._prevRsiValue = 0.0
        self._currentRsiValue = 0.0
        self._currentRsiAvg = 0.0
        self._currentRsiStdDev = 0.0

    @property
    def RsiPeriod(self):
        """RSI period."""
        return self._rsiPeriod.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsiPeriod.Value = value

    @property
    def AveragePeriod(self):
        """Period for RSI average calculation."""
        return self._averagePeriod.Value

    @AveragePeriod.setter
    def AveragePeriod(self, value):
        self._averagePeriod.Value = value

    @property
    def Multiplier(self):
        """Standard deviation multiplier for entry."""
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED!! Return securities and candle types used."""
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(rsi_breakout_strategy, self).OnReseted()
        self._prevRsiValue = 0.0
        self._currentRsiValue = 0.0
        self._currentRsiAvg = 0.0
        self._currentRsiStdDev = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(rsi_breakout_strategy, self).OnStarted(time)

        self._prevRsiValue = 0.0
        self._currentRsiValue = 0.0
        self._currentRsiAvg = 0.0
        self._currentRsiStdDev = 0.0

        # Create indicators
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod
        self._rsiAverage = SimpleMovingAverage()
        self._rsiAverage.Length = self.AveragePeriod
        self._rsiStdDev = StandardDeviation()
        self._rsiStdDev.Length = self.AveragePeriod

        # Create candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind RSI to candles
        subscription.Bind(self._rsi, self.ProcessRsi).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawIndicator(area, self._rsiAverage)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            Unit(5, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent)
        )

    def ProcessRsi(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        # Store previous and current RSI value
        self._prevRsiValue = self._currentRsiValue
        self._currentRsiValue = rsi_value

        # Process RSI through average and standard deviation indicators
        avg_value = process_float(self._rsiAverage, rsi_value, candle.ServerTime, candle.State == CandleStates.Finished)
        std_dev_value = process_float(self._rsiStdDev, rsi_value, candle.ServerTime, candle.State == CandleStates.Finished)

        self._currentRsiAvg = to_float(avg_value)
        self._currentRsiStdDev = to_float(std_dev_value)

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading() or not self._rsiAverage.IsFormed or not self._rsiStdDev.IsFormed:
            return

        # Calculate bands
        upper_band = self._currentRsiAvg + self.Multiplier * self._currentRsiStdDev
        lower_band = self._currentRsiAvg - self.Multiplier * self._currentRsiStdDev

        self.LogInfo(
            "RSI: {0}, RSI Avg: {1}, Upper: {2}, Lower: {3}".format(
                self._currentRsiValue, self._currentRsiAvg, upper_band, lower_band))

        # Entry logic - BREAKOUT
        if self.Position == 0:
            # Long Entry: RSI breaks above upper band
            if self._currentRsiValue > upper_band:
                self.LogInfo("Buy Signal - RSI ({0}) > Upper Band ({1})".format(self._currentRsiValue, upper_band))
                self.BuyMarket(self.Volume)
            # Short Entry: RSI breaks below lower band
            elif self._currentRsiValue < lower_band:
                self.LogInfo("Sell Signal - RSI ({0}) < Lower Band ({1})".format(self._currentRsiValue, lower_band))
                self.SellMarket(self.Volume)
        # Exit logic
        elif self.Position > 0 and self._currentRsiValue < self._currentRsiAvg:
            # Exit Long: RSI returns below average
            self.LogInfo("Exit Long - RSI ({0}) < RSI Avg ({1})".format(self._currentRsiValue, self._currentRsiAvg))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and self._currentRsiValue > self._currentRsiAvg:
            # Exit Short: RSI returns above average
            self.LogInfo("Exit Short - RSI ({0}) > RSI Avg ({1})".format(self._currentRsiValue, self._currentRsiAvg))
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return rsi_breakout_strategy()
