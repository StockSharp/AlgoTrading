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

        self._prev_stoch_k_value = 0.0

        # Create indicators
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.k_period
        self._stochastic.D.Length = self.d_period

        self._stoch_average = SimpleMovingAverage()
        self._stoch_average.Length = self.average_period
        self._stoch_stddev = StandardDeviation()
        self._stoch_stddev.Length = self.average_period

        # Create candle subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind stochastic to candles
        subscription.BindEx(self._stochastic, self.ProcessStochastic).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._stochastic)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(5, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
    def ProcessStochastic(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        # Extract %K value from stochastic
        if stoch_value.K is None:
            return
        k_value = float(stoch_value.K)

        # Process Stochastic %K through average and standard deviation indicators
        stoch_avg_value = float(process_float(self._stoch_average, k_value, candle.ServerTime, candle.State == CandleStates.Finished))
        stoch_stddev_value = float(process_float(self._stoch_stddev, k_value, candle.ServerTime, candle.State == CandleStates.Finished))

        # Store previous Stochastic %K value for changes detection
        current_stoch_k_value = k_value

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading() or not self._stoch_average.IsFormed or not self._stoch_stddev.IsFormed:
            self._prev_stoch_k_value = current_stoch_k_value
            return

        # Calculate bands
        upper_band = stoch_avg_value + self.multiplier * stoch_stddev_value
        lower_band = stoch_avg_value - self.multiplier * stoch_stddev_value

        self.LogInfo(
            "Stoch %K: {0}, Avg: {1}, Upper: {2}, Lower: {3}".format(
                current_stoch_k_value, stoch_avg_value, upper_band, lower_band
            )
        )

        # Entry logic
        if self.Position == 0:
            # Long Entry: Stochastic %K is below lower band
            if current_stoch_k_value < lower_band:
                self.LogInfo(
                    "Buy Signal - Stoch %K ({0}) < Lower Band ({1})".format(
                        current_stoch_k_value, lower_band
                    )
                )
                self.BuyMarket(self.Volume)
            # Short Entry: Stochastic %K is above upper band
            elif current_stoch_k_value > upper_band:
                self.LogInfo(
                    "Sell Signal - Stoch %K ({0}) > Upper Band ({1})".format(
                        current_stoch_k_value, upper_band
                    )
                )
                self.SellMarket(self.Volume)
        # Exit logic
        elif self.Position > 0 and current_stoch_k_value > stoch_avg_value:
            # Exit Long: Stochastic %K returned to average
            self.LogInfo(
                "Exit Long - Stoch %K ({0}) > Avg ({1})".format(
                    current_stoch_k_value, stoch_avg_value
                )
            )
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and current_stoch_k_value < stoch_avg_value:
            # Exit Short: Stochastic %K returned to average
            self.LogInfo(
                "Exit Short - Stoch %K ({0}) < Avg ({1})".format(
                    current_stoch_k_value, stoch_avg_value
                )
            )
            self.BuyMarket(Math.Abs(self.Position))

        self._prev_stoch_k_value = current_stoch_k_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return stochastic_mean_reversion_strategy()
