import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class stochastic_mean_reversion_strategy(Strategy):
    """
    Stochastic Mean Reversion Strategy.
    Enter when Stochastic %K deviates from its average by a certain multiple of standard deviation.
    Exit when Stochastic %K returns to its average.

    """

    def __init__(self):
        super(stochastic_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Stochastic Period", "Period for Stochastic calculation", "Strategy Parameters")

        self._k_period = self.Param("KPeriod", 3) \
            .SetDisplay("K Period", "Period for %K calculation", "Strategy Parameters")

        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("D Period", "Period for %D calculation", "Strategy Parameters")

        self._average_period = self.Param("AveragePeriod", 20) \
            .SetDisplay("Average Period", "Period for Stochastic average calculation", "Strategy Parameters")

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetDisplay("StdDev Multiplier", "Standard deviation multiplier for entry", "Strategy Parameters")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters")

        # Internal state
        self._stochastic = None
        self._stoch_average = None
        self._stoch_stddev = None
        self._prev_stoch_k_value = 0.0

    @property
    def stoch_period(self):
        """Stochastic period."""
        return self._stoch_period.Value

    @stoch_period.setter
    def stoch_period(self, value):
        self._stoch_period.Value = value

    @property
    def k_period(self):
        """Stochastic %K period."""
        return self._k_period.Value

    @k_period.setter
    def k_period(self, value):
        self._k_period.Value = value

    @property
    def d_period(self):
        """Stochastic %D period."""
        return self._d_period.Value

    @d_period.setter
    def d_period(self, value):
        self._d_period.Value = value

    @property
    def average_period(self):
        """Period for Stochastic average calculation."""
        return self._average_period.Value

    @average_period.setter
    def average_period(self, value):
        self._average_period.Value = value

    @property
    def multiplier(self):
        """Standard deviation multiplier for entry."""
        return self._multiplier.Value

    @multiplier.setter
    def multiplier(self, value):
        self._multiplier.Value = value

    @property
    def candle_type(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(stochastic_mean_reversion_strategy, self).OnStarted(time)

        # Create indicators
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.k_period
        self._stochastic.D.Length = self.d_period

        self._stoch_average = SimpleMovingAverage()
        self._stoch_average.Length = self.average_period
        self._stoch_stddev = StandardDeviation()
        self._stoch_stddev.Length = self.average_period

        self.Indicators.Add(self._stochastic)
        self.Indicators.Add(self._stoch_average)
        self.Indicators.Add(self._stoch_stddev)

        # Create candle subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind candle processing (manual stochastic processing inside)
        subscription.Bind(self.ProcessStochastic).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._stochastic)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

    def OnReseted(self):
        super(stochastic_mean_reversion_strategy, self).OnReseted()
        self._prev_stoch_k_value = 0.0
    def ProcessStochastic(self, candle):
        if candle.State != CandleStates.Finished:
            return

        stoch_result = process_candle(self._stochastic, candle)
        if not self._stochastic.IsFormed:
            return

        k_value = stoch_result.K
        if k_value is None:
            return
        k_value = float(k_value)

        # Process Stochastic %K through average and standard deviation indicators
        stoch_avg_value = float(process_float(self._stoch_average, k_value, candle.OpenTime, True))
        stoch_stddev_value = float(process_float(self._stoch_stddev, k_value, candle.OpenTime, True))

        if not self._stoch_average.IsFormed or not self._stoch_stddev.IsFormed:
            self._prev_stoch_k_value = k_value
            return

        effective_stddev = max(1.0, stoch_stddev_value)
        upper_band = stoch_avg_value + self.multiplier * effective_stddev
        lower_band = stoch_avg_value - self.multiplier * effective_stddev

        # Entry logic - only when flat
        if self.Position == 0:
            if k_value < lower_band or k_value < 20.0:
                self.BuyMarket()
            elif k_value > upper_band or k_value > 80.0:
                self.SellMarket()

        self._prev_stoch_k_value = k_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return stochastic_mean_reversion_strategy()
