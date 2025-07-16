import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend, Momentum
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class supertrend_momentum_filter_strategy(Strategy):
    """
    Strategy based on Supertrend and Momentum indicators.
    """

    def __init__(self):
        super(supertrend_momentum_filter_strategy, self).__init__()

        # Supertrend period parameter.
        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Period", "Period of the Supertrend indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        # Supertrend multiplier parameter.
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Multiplier", "Multiplier for the Supertrend indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        # Momentum period parameter.
        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Period", "Period of the Momentum indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 30, 5)

        # Candle type parameter.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Store previous values to detect changes
        self._prev_momentum = 0.0

    @property
    def SupertrendPeriod(self):
        """Supertrend period parameter."""
        return self._supertrend_period.Value

    @SupertrendPeriod.setter
    def SupertrendPeriod(self, value):
        self._supertrend_period.Value = value

    @property
    def SupertrendMultiplier(self):
        """Supertrend multiplier parameter."""
        return self._supertrend_multiplier.Value

    @SupertrendMultiplier.setter
    def SupertrendMultiplier(self, value):
        self._supertrend_multiplier.Value = value

    @property
    def MomentumPeriod(self):
        """Momentum period parameter."""
        return self._momentum_period.Value

    @MomentumPeriod.setter
    def MomentumPeriod(self, value):
        self._momentum_period.Value = value

    @property
    def CandleType(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(supertrend_momentum_filter_strategy, self).OnStarted(time)

        # Initialize previous values
        self._prev_momentum = 0

        # Create indicators
        supertrend = SuperTrend()
        supertrend.Length = self.SupertrendPeriod
        supertrend.Multiplier = self.SupertrendMultiplier

        momentum = Momentum()
        momentum.Length = self.MomentumPeriod

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(supertrend, momentum, self.ProcessCandle).Start()

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawIndicator(area, momentum)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent),
        )

    def ProcessCandle(self, candle, supertrend_value, momentum_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        is_above_supertrend = candle.ClosePrice > supertrend_value
        is_momentum_rising = momentum_value > self._prev_momentum

        # Strategy logic:
        # Buy when price is above Supertrend and Momentum is rising
        # Sell when price is below Supertrend and Momentum is falling
        if is_above_supertrend and is_momentum_rising and self.Position <= 0:
            # Cancel any active orders before entering a new position
            self.CancelActiveOrders()

            # Calculate position size
            volume = self.Volume + Math.Abs(self.Position)

            # Enter long position
            self.BuyMarket(volume)
        elif not is_above_supertrend and not is_momentum_rising and self.Position >= 0:
            # Cancel any active orders before entering a new position
            self.CancelActiveOrders()

            # Calculate position size
            volume = self.Volume + Math.Abs(self.Position)

            # Enter short position
            self.SellMarket(volume)

        # Store current momentum value for next comparison
        self._prev_momentum = momentum_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return supertrend_momentum_filter_strategy()
