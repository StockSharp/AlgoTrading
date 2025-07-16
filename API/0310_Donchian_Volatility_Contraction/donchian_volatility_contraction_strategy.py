import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (

    Highest,
    Lowest,
    AverageTrueRange,
    SimpleMovingAverage,
    StandardDeviation,
)
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class donchian_volatility_contraction_strategy(Strategy):
    """Strategy based on Donchian Channel breakout after volatility contraction."""

    def __init__(self):
        super(donchian_volatility_contraction_strategy, self).__init__()

        # Donchian channel period parameter.
        self._donchian_period = (
            self.Param("DonchianPeriod", 20)
            .SetGreaterThanZero()
            .SetDisplay("Donchian Period", "Period for Donchian Channel", "Indicators")
            .SetCanOptimize(True)
            .SetOptimize(10, 50, 5)
        )

        # ATR period parameter.
        self._atr_period = (
            self.Param("AtrPeriod", 14)
            .SetGreaterThanZero()
            .SetDisplay("ATR Period", "Period for ATR indicator", "Indicators")
            .SetCanOptimize(True)
            .SetOptimize(7, 28, 7)
        )

        # Volatility contraction factor parameter.
        self._volatility_factor = (
            self.Param("VolatilityFactor", 2.0)
            .SetGreaterThanZero()
            .SetDisplay(
                "Volatility Factor",
                "Standard deviation multiplier for contraction detection",
                "Indicators",
            )
            .SetCanOptimize(True)
            .SetOptimize(1.0, 3.0, 0.5)
        )

        # Candle type parameter.
        self._candle_type = (
            self.Param("CandleType", tf(5))
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        )

        # Indicators for maintaining the channel width
        self._avg_dc_width = 0
        self._std_dev_dc_width = 0
        self._current_dc_width = 0

    @property
    def DonchianPeriod(self):
        """Donchian channel period parameter."""
        return self._donchian_period.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchian_period.Value = value

    @property
    def AtrPeriod(self):
        """ATR period parameter."""
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def VolatilityFactor(self):
        """Volatility contraction factor parameter."""
        return self._volatility_factor.Value

    @VolatilityFactor.setter
    def VolatilityFactor(self, value):
        self._volatility_factor.Value = value

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
        super(donchian_volatility_contraction_strategy, self).OnStarted(time)

        # Initialize values
        self._avg_dc_width = 0
        self._std_dev_dc_width = 0
        self._current_dc_width = 0

        # Create indicators
        donchian_high = Highest()
        donchian_high.Length = self.DonchianPeriod
        donchian_low = Lowest()
        donchian_low.Length = self.DonchianPeriod
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        sma = SimpleMovingAverage()
        sma.Length = self.DonchianPeriod
        standard_deviation = StandardDeviation()
        standard_deviation.Length = self.DonchianPeriod

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)

        def handler(candle, high_value):
            high_price = to_float(high_value)

            # Process Donchian Low separately
            low_value = process_candle(donchian_low, candle)
            low_price = to_float(low_value)

            # Process ATR
            atr_value = process_candle(atr, candle)

            # Calculate Donchian Channel width
            self._current_dc_width = high_price - low_price

            # Process SMA and StdDev for the channel width
            sma_value = process_float(
                sma,
                self._current_dc_width,
                candle.ServerTime,
                candle.State == CandleStates.Finished,
            )
            std_dev_value = process_float(
                standard_deviation,
                self._current_dc_width,
                candle.ServerTime,
                candle.State == CandleStates.Finished,
            )

            self._avg_dc_width = to_float(sma_value)
            self._std_dev_dc_width = to_float(std_dev_value)

            # Process the strategy logic
            self.ProcessStrategy(candle, high_price, low_price, to_float(atr_value))

        subscription.BindEx(donchian_high, handler).Start()

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )
    def ProcessStrategy(self, candle, donchian_high, donchian_low, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate volatility threshold
        volatility_threshold = self._avg_dc_width - self.VolatilityFactor * self._std_dev_dc_width

        # Check for volatility contraction
        is_volatility_contracted = self._current_dc_width < volatility_threshold

        if is_volatility_contracted:
            # Breakout after volatility contraction
            if candle.ClosePrice > donchian_high and self.Position <= 0:
                # Cancel any active orders before entering a new position
                self.CancelActiveOrders()

                # Calculate position size
                volume = self.Volume + Math.Abs(self.Position)

                # Enter long position
                self.BuyMarket(volume)
            elif candle.ClosePrice < donchian_low and self.Position >= 0:
                # Cancel any active orders before entering a new position
                self.CancelActiveOrders()

                # Calculate position size
                volume = self.Volume + Math.Abs(self.Position)

                # Enter short position
                self.SellMarket(volume)

        # Exit logic - when price reverts to the middle of the channel
        channel_middle = (donchian_high + donchian_low) / 2

        if (self.Position > 0 and candle.ClosePrice < channel_middle) or (
            self.Position < 0 and candle.ClosePrice > channel_middle
        ):
            # Close position
            self.ClosePosition()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return donchian_volatility_contraction_strategy()
