import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator, StochasticOscillatorValue, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class stochastic_with_dynamic_zones_strategy(Strategy):
    """
    Strategy based on Stochastic Oscillator with Dynamic Overbought/Oversold Zones.

    """

    def __init__(self):
        super(stochastic_with_dynamic_zones_strategy, self).__init__()

        # Initialize strategy parameters
        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Period for Stochastic Oscillator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 30, 5)

        self._stoch_k_period = self.Param("StochKPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %K Period", "Smoothing period for %K line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 10, 1)

        self._stoch_d_period = self.Param("StochDPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %D Period", "Smoothing period for %D line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 10, 1)

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for dynamic zones calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10)

        self._std_dev_factor = self.Param("StandardDeviationFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Standard Deviation Factor", "Factor for dynamic zones calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal fields
        self._prev_stoch_k = 50
        self._stochastic = None
        self._stoch_sma = None
        self._stoch_std_dev = None

    @property
    def StochPeriod(self):
        """Stochastic period parameter."""
        return self._stoch_period.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stoch_period.Value = value

    @property
    def StochKPeriod(self):
        """Stochastic %K period parameter."""
        return self._stoch_k_period.Value

    @StochKPeriod.setter
    def StochKPeriod(self, value):
        self._stoch_k_period.Value = value

    @property
    def StochDPeriod(self):
        """Stochastic %D period parameter."""
        return self._stoch_d_period.Value

    @StochDPeriod.setter
    def StochDPeriod(self, value):
        self._stoch_d_period.Value = value

    @property
    def LookbackPeriod(self):
        """Lookback period for dynamic zones calculation."""
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def StandardDeviationFactor(self):
        """Standard deviation factor for dynamic zones."""
        return self._std_dev_factor.Value

    @StandardDeviationFactor.setter
    def StandardDeviationFactor(self, value):
        self._std_dev_factor.Value = value

    @property
    def CandleType(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(stochastic_with_dynamic_zones_strategy, self).OnReseted()
        self._prev_stoch_k = 50
        self._stochastic = None
        self._stoch_sma = None
        self._stoch_std_dev = None

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(stochastic_with_dynamic_zones_strategy, self).OnStarted(time)

        # Create indicators
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochKPeriod
        self._stochastic.D.Length = self.StochDPeriod

        self._stoch_sma = SimpleMovingAverage()
        self._stoch_sma.Length = self.LookbackPeriod
        self._stoch_std_dev = StandardDeviation()
        self._stoch_std_dev.Length = self.LookbackPeriod

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._stochastic, self.ProcessStochastic).Start()

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._stochastic)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )
    def ProcessStochastic(self, candle, stoch_value):
        if stoch_value.K is None:
            return

        # Calculate dynamic zones
        stoch_k = float(stoch_value.K)
        stoch_k_avg = float(process_float(self._stoch_sma, stoch_k, candle.ServerTime, candle.State == CandleStates.Finished))
        stoch_k_std_dev = float(process_float(self._stoch_std_dev, stoch_k, candle.ServerTime, candle.State == CandleStates.Finished))

        dynamic_oversold = stoch_k_avg - (self.StandardDeviationFactor * stoch_k_std_dev)
        dynamic_overbought = stoch_k_avg + (self.StandardDeviationFactor * stoch_k_std_dev)

        # Process the strategy logic
        self.ProcessStrategy(candle, stoch_k, dynamic_oversold, dynamic_overbought)

    def ProcessStrategy(self, candle, stoch_k, dynamic_oversold, dynamic_overbought):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check if Stochastic is reversing
        is_reversing_up = stoch_k > self._prev_stoch_k
        is_reversing_down = stoch_k < self._prev_stoch_k

        # Check if Stochastic is in oversold/overbought zones
        is_oversold = stoch_k < dynamic_oversold
        is_overbought = stoch_k > dynamic_overbought

        # Trading logic
        if is_oversold and is_reversing_up and self.Position <= 0:
            # Oversold condition with upward reversal - Buy signal
            self.CancelActiveOrders()

            # Calculate position size
            volume = self.Volume + Math.Abs(self.Position)

            # Enter long position
            self.BuyMarket(volume)
        elif is_overbought and is_reversing_down and self.Position >= 0:
            # Overbought condition with downward reversal - Sell signal
            self.CancelActiveOrders()

            # Calculate position size
            volume = self.Volume + Math.Abs(self.Position)

            # Enter short position
            self.SellMarket(volume)

        # Exit logic - when Stochastic crosses the middle line (50)
        if (self.Position > 0 and stoch_k > 50) or (self.Position < 0 and stoch_k < 50):
            # Close position
            self.ClosePosition()

        # Update previous Stochastic value
        self._prev_stoch_k = stoch_k

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return stochastic_with_dynamic_zones_strategy()

