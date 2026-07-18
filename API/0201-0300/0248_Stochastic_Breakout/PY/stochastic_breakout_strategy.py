import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

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

    def OnReseted(self):
        super(stochastic_breakout_strategy, self).OnReseted()
        self._prevStochValue = 0
        self._prevStochAverage = 0
        self._prevStochStdDev = 0

    def OnStarted2(self, time):
        """Called when the strategy starts."""
        super(stochastic_breakout_strategy, self).OnStarted2(time)

        # Initialize indicators
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochasticPeriod
        self._stochastic.D.Length = self.DPeriod

        self._stochAverage = SimpleMovingAverage()
        self._stochAverage.Length = self.LookbackPeriod
        self._stochStdDev = StandardDeviation()
        self._stochStdDev.Length = self.LookbackPeriod

        self.Indicators.Add(self._stochastic)

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessStochastic).Start()

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
    def ProcessStochastic(self, candle):
        if candle.State != CandleStates.Finished:
            return

        stoch_result = process_candle(self._stochastic, candle)
        if not self._stochastic.IsFormed:
            return

        k_val = stoch_result.K
        if k_val is None:
            return
        stochK = float(k_val)

        # Calculate average and standard deviation of stochastic
        stochAvgValue = float(process_float(self._stochAverage, stochK, candle.ServerTime, True))
        tempStdDevValue = float(process_float(self._stochStdDev, stochK, candle.ServerTime, True))

        if not self._stochAverage.IsFormed or not self._stochStdDev.IsFormed:
            self._prevStochValue = stochK
            self._prevStochAverage = stochAvgValue
            self._prevStochStdDev = tempStdDevValue
            return

        # First values initialization - skip trading decision
        if self._prevStochValue == 0:
            self._prevStochValue = stochK
            self._prevStochAverage = stochAvgValue
            self._prevStochStdDev = tempStdDevValue
            return

        # Calculate breakout thresholds
        upperThreshold = self._prevStochAverage + self._prevStochStdDev * float(self.DeviationMultiplier)
        lowerThreshold = self._prevStochAverage - self._prevStochStdDev * float(self.DeviationMultiplier)

        # Entry only when flat (no exit logic in CS)
        if stochK > upperThreshold and self._prevStochValue <= upperThreshold and self.Position == 0:
            self.BuyMarket()
        elif stochK < lowerThreshold and self._prevStochValue >= lowerThreshold and self.Position == 0:
            self.SellMarket()

        # Store current values for next comparison
        self._prevStochValue = stochK
        self._prevStochAverage = stochAvgValue
        self._prevStochStdDev = tempStdDevValue

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return stochastic_breakout_strategy()
