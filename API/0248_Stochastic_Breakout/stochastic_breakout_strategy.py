import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator, SimpleMovingAverage, StandardDeviation, StochasticOscillatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class stochastic_breakout_strategy(Strategy):
    """
    Stochastic Breakout Strategy.
    This strategy identifies breakouts based on the Stochastic oscillator values compared to their historical average.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(stochastic_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._stochasticPeriod = self.Param("StochasticPeriod", 14) \
            .SetDisplay("Stochastic Period", "Stochastic oscillator period", "Stochastic")

        self._kPeriod = self.Param("KPeriod", 3) \
            .SetDisplay("K Period", "Stochastic %K smoothing period", "Stochastic")

        self._dPeriod = self.Param("DPeriod", 3) \
            .SetDisplay("D Period", "Stochastic %D smoothing period", "Stochastic")

        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Lookback period for calculating the average and standard deviation", "Breakout")

        self._deviationMultiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetDisplay("Deviation Multiplier", "Deviation multiplier for breakout detection", "Breakout")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        # Internal indicators and state
        self._stochastic = None
        self._stochAverage = None
        self._stochStdDev = None

        self._prevStochValue = 0
        self._prevStochAverage = 0
        self._prevStochStdDev = 0

    @property
    def StochasticPeriod(self):
        """Stochastic oscillator period."""
        return self._stochasticPeriod.Value

    @StochasticPeriod.setter
    def StochasticPeriod(self, value):
        self._stochasticPeriod.Value = value

    @property
    def KPeriod(self):
        """Stochastic %K smoothing period."""
        return self._kPeriod.Value

    @KPeriod.setter
    def KPeriod(self, value):
        self._kPeriod.Value = value

    @property
    def DPeriod(self):
        """Stochastic %D smoothing period."""
        return self._dPeriod.Value

    @DPeriod.setter
    def DPeriod(self, value):
        self._dPeriod.Value = value

    @property
    def LookbackPeriod(self):
        """Lookback period for calculating the average and standard deviation."""
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def DeviationMultiplier(self):
        """Deviation multiplier for breakout detection."""
        return self._deviationMultiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviationMultiplier.Value = value

    @property
    def CandleType(self):
        """Candle type."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED!! Return securities and candle types used."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(stochastic_breakout_strategy, self).OnStarted(time)

        self._prevStochAverage = 0
        self._prevStochStdDev = 0
        self._prevStochValue = 0

        # Initialize indicators
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochasticPeriod
        self._stochastic.D.Length = self.DPeriod

        self._stochAverage = SimpleMovingAverage()
        self._stochAverage.Length = self.LookbackPeriod
        self._stochStdDev = StandardDeviation()
        self._stochStdDev.Length = self.LookbackPeriod

        # Reset stored values
        self._prevStochValue = 0
        self._prevStochAverage = 0
        self._prevStochStdDev = 0

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._stochastic, self.ProcessStochastic).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._stochastic)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )

    def ProcessStochastic(self, candle, stochValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get stochastic value (K line)
        stochTyped = stochValue if isinstance(stochValue, StochasticOscillatorValue) else None
        if stochTyped is None or stochTyped.K is None:
            return

        stochK = stochTyped.K

        # Calculate average and standard deviation of stochastic
        stochAvgValue = to_float(self._stochAverage.Process(stochK, candle.ServerTime, candle.State == CandleStates.Finished))
        tempStdDevValue = to_float(self._stochStdDev.Process(stochK, candle.ServerTime, candle.State == CandleStates.Finished))

        # First values initialization - skip trading decision
        if self._prevStochValue == 0:
            self._prevStochValue = stochK
            self._prevStochAverage = stochAvgValue
            self._prevStochStdDev = tempStdDevValue
            return

        # Calculate breakout thresholds
        upperThreshold = self._prevStochAverage + self._prevStochStdDev * self.DeviationMultiplier
        lowerThreshold = self._prevStochAverage - self._prevStochStdDev * self.DeviationMultiplier

        # Trading logic:
        # Buy when stochastic breaks above upper threshold
        if stochK > upperThreshold and self._prevStochValue <= upperThreshold and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Stochastic breakout UP: {0} > {1}. Buying at {2}".format(stochK, upperThreshold, candle.ClosePrice))
        # Sell when stochastic breaks below lower threshold
        elif stochK < lowerThreshold and self._prevStochValue >= lowerThreshold and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Stochastic breakout DOWN: {0} < {1}. Selling at {2}".format(stochK, lowerThreshold, candle.ClosePrice))
        # Exit positions when stochastic returns to average
        elif self.Position > 0 and stochK < self._prevStochAverage and self._prevStochValue >= self._prevStochAverage:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Stochastic returned to average: {0} < {1}. Closing long position at {2}".format(stochK, self._prevStochAverage, candle.ClosePrice))
        elif self.Position < 0 and stochK > self._prevStochAverage and self._prevStochValue <= self._prevStochAverage:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Stochastic returned to average: {0} > {1}. Closing short position at {2}".format(stochK, self._prevStochAverage, candle.ClosePrice))

        # Store current values for next comparison
        self._prevStochValue = stochK
        self._prevStochAverage = stochAvgValue
        self._prevStochStdDev = tempStdDevValue

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return stochastic_breakout_strategy()
