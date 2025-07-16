import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class donchian_width_breakout_strategy(Strategy):
    """
    Strategy that trades on Donchian Channel width breakouts.
    When Donchian Channel width increases significantly above its average,
    it enters position in the direction determined by price movement.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        """Initialize donchian_width_breakout_strategy."""
        super(donchian_width_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Donchian Period", "Period for the Donchian Channel", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._avg_period = self.Param("AvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for width average calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        # Create indicator placeholders
        self._highest = None
        self._lowest = None
        self._width_average = None

        # Track channel width values
        self._last_width = 0.0
        self._last_avg_width = 0.0

    @property
    def DonchianPeriod(self):
        """Donchian Channel period."""
        return self._donchian_period.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchian_period.Value = value

    @property
    def AvgPeriod(self):
        """Period for width average calculation."""
        return self._avg_period.Value

    @AvgPeriod.setter
    def AvgPeriod(self, value):
        self._avg_period.Value = value

    @property
    def Multiplier(self):
        """Standard deviation multiplier for breakout detection."""
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLoss(self):
        """Stop-loss percentage."""
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    def GetWorkingSecurities(self):
        """Return securities used by the strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(donchian_width_breakout_strategy, self).OnStarted(time)

        self._last_width = 0
        self._last_avg_width = 0

        # Create indicators for Donchian Channel components
        self._highest = Highest()
        self._highest.Length = self.DonchianPeriod
        self._lowest = Lowest()
        self._lowest.Length = self.DonchianPeriod
        self._width_average = SimpleMovingAverage()
        self._width_average.Length = self.AvgPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind to candle processing
        subscription.Bind(self.ProcessCandle).Start()

        # Enable stop loss protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLoss, UnitTypes.Percent)
        )
        # Create chart area for visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Process candle through Highest and Lowest indicators
        highest_value = to_float(self._highest.Process(candle))
        lowest_value = to_float(self._lowest.Process(candle))

        # Calculate Donchian Channel width
        width = highest_value - lowest_value

        # Process width through average
        width_avg_value = process_float(self._width_average, width, candle.ServerTime, candle.State == CandleStates.Finished)
        avg_width = to_float(width_avg_value)

        # For first values, just save and skip
        if self._last_width == 0:
            self._last_width = width
            self._last_avg_width = avg_width
            return

        # Calculate width standard deviation (simplified approach)
        std_dev = Math.Abs(width - avg_width) * 1.5  # Simplified approximation

        # Skip if indicators are not formed yet
        if not self._highest.IsFormed or not self._lowest.IsFormed or not self._width_average.IsFormed:
            self._last_width = width
            self._last_avg_width = avg_width
            return

        # Check if trading is allowed
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._last_width = width
            self._last_avg_width = avg_width
            return

        # Donchian Channel width breakout detection
        if width > avg_width + self.Multiplier * std_dev:
            # Determine direction based on price and channel
            middle_channel = (highest_value + lowest_value) / 2
            bullish = candle.ClosePrice > middle_channel

            # Cancel active orders before placing new ones
            self.CancelActiveOrders()

            # Trade in the direction determined by price position in the channel
            if bullish and self.Position <= 0:
                # Bullish breakout - Buy
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif not bullish and self.Position >= 0:
                # Bearish breakout - Sell
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        # Check for exit condition - width returns to average
        elif (self.Position > 0 or self.Position < 0) and width < avg_width:
            # Exit position when channel width returns to normal
            self.ClosePosition()

        # Update last values
        self._last_width = width
        self._last_avg_width = avg_width

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return donchian_width_breakout_strategy()

