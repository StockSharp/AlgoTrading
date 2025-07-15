import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class parabolic_sar_reversal_strategy(Strategy):
    """Parabolic SAR Reversal Strategy.

    Enters long when SAR switches from above to below price.
    Enters short when SAR switches from below to above price.
    """

    def __init__(self):
        """Initializes a new instance of the strategy."""
        super(parabolic_sar_reversal_strategy, self).__init__()

        self._initial_acceleration = self.Param("InitialAcceleration", 0.02) \
            .SetDisplay("Initial Acceleration", "Initial acceleration factor for Parabolic SAR", "SAR Settings") \
            .SetRange(0.01, 0.05) \
            .SetCanOptimize(True)

        self._max_acceleration = self.Param("MaxAcceleration", 0.2) \
            .SetDisplay("Max Acceleration", "Maximum acceleration factor for Parabolic SAR", "SAR Settings") \
            .SetRange(0.1, 0.3) \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_is_sar_above_price = None

    # Initial acceleration factor for Parabolic SAR.
    @property
    def InitialAcceleration(self):
        return self._initial_acceleration.Value

    @InitialAcceleration.setter
    def InitialAcceleration(self, value):
        self._initial_acceleration.Value = value

    # Maximum acceleration factor for Parabolic SAR.
    @property
    def MaxAcceleration(self):
        return self._max_acceleration.Value

    @MaxAcceleration.setter
    def MaxAcceleration(self, value):
        self._max_acceleration.Value = value

    # Type of candles to use.
    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(parabolic_sar_reversal_strategy, self).OnStarted(time)

        # Initialize previous state
        self._prev_is_sar_above_price = None

        # Create Parabolic SAR indicator
        parabolic_sar = ParabolicSar()
        parabolic_sar.Acceleration = self.InitialAcceleration
        parabolic_sar.AccelerationMax = self.MaxAcceleration

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicator and process candles
        subscription.Bind(parabolic_sar, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, parabolic_sar)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, sar_value):
        """Process candle with Parabolic SAR value."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Determine if SAR is above or below price
        is_sar_above_price = sar_value > candle.ClosePrice

        # If this is the first calculation, just store the state
        if self._prev_is_sar_above_price is None:
            self._prev_is_sar_above_price = is_sar_above_price
            return

        # Check for SAR reversal
        sar_switched_below = self._prev_is_sar_above_price and not is_sar_above_price
        sar_switched_above = not self._prev_is_sar_above_price and is_sar_above_price

        # Long entry: SAR switched from above to below price
        if sar_switched_below and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self.LogInfo(f"Long entry: SAR ({sar_value}) switched below price ({candle.ClosePrice})")
        # Short entry: SAR switched from below to above price
        elif sar_switched_above and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self.LogInfo(f"Short entry: SAR ({sar_value}) switched above price ({candle.ClosePrice})")

        # Update the previous state
        self._prev_is_sar_above_price = is_sar_above_price

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return parabolic_sar_reversal_strategy()
