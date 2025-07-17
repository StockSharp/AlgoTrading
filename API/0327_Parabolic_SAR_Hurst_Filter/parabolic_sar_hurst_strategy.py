import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ParabolicSar, HurstExponent
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class parabolic_sar_hurst_strategy(Strategy):
    """
    Parabolic SAR with Hurst Filter Strategy.
    Enters a position when price crosses SAR and Hurst exponent indicates a persistent trend.

    """

    def __init__(self):
        super(parabolic_sar_hurst_strategy, self).__init__()

        # Initialize strategy.
        self._sar_acceleration_factor = self.Param("SarAccelerationFactor", 0.02) \
            .SetRange(0.01, 0.2) \
            .SetDisplay("SAR Acceleration Factor", "Initial acceleration factor for Parabolic SAR", "SAR Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(0.01, 0.1, 0.01)

        self._sar_max_acceleration_factor = self.Param("SarMaxAccelerationFactor", 0.2) \
            .SetRange(0.05, 0.5) \
            .SetDisplay("SAR Max Acceleration Factor", "Maximum acceleration factor for Parabolic SAR", "SAR Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(0.1, 0.3, 0.05)

        self._hurst_period = self.Param("HurstPeriod", 100) \
            .SetRange(20, 200) \
            .SetDisplay("Hurst Period", "Period for Hurst exponent calculation", "Hurst Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(50, 150, 25)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_sar_value = 0
        self._hurst_value = 0.5  # Default value (random walk)

    @property
    def SarAccelerationFactor(self):
        """Parabolic SAR acceleration factor."""
        return self._sar_acceleration_factor.Value

    @SarAccelerationFactor.setter
    def SarAccelerationFactor(self, value):
        self._sar_acceleration_factor.Value = value

    @property
    def SarMaxAccelerationFactor(self):
        """Parabolic SAR maximum acceleration factor."""
        return self._sar_max_acceleration_factor.Value

    @SarMaxAccelerationFactor.setter
    def SarMaxAccelerationFactor(self, value):
        self._sar_max_acceleration_factor.Value = value

    @property
    def HurstPeriod(self):
        """Hurst exponent calculation period."""
        return self._hurst_period.Value

    @HurstPeriod.setter
    def HurstPeriod(self, value):
        self._hurst_period.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(parabolic_sar_hurst_strategy, self).OnStarted(time)

        # Initialize values
        self._prev_sar_value = 0
        self._hurst_value = 0.5  # Default value (random walk)

        # Create indicators
        parabolic_sar = ParabolicSar()
        parabolic_sar.Acceleration = self.SarAccelerationFactor
        parabolic_sar.AccelerationMax = self.SarMaxAccelerationFactor

        hurst_indicator = HurstExponent()
        hurst_indicator.Length = self.HurstPeriod

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to the subscription
        subscription.BindEx(parabolic_sar, hurst_indicator, self.ProcessCandle).Start()

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, parabolic_sar)
            self.DrawIndicator(area, hurst_indicator)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, sar_value, hurst_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get SAR and Hurst values
        sar_price = to_float(sar_value)
        self._hurst_value = to_float(hurst_value)

        # Store previous SAR for comparison
        current_sar_value = sar_price

        # Log the values
        self.LogInfo(f"SAR: {sar_price}, Hurst: {self._hurst_value}, Price: {candle.ClosePrice}")

        # Skip first candle (need previous SAR value for comparison)
        if self._prev_sar_value == 0:
            self._prev_sar_value = current_sar_value
            return

        # Trading logic based on Parabolic SAR and Hurst exponent
        # Hurst > 0.5 indicates trending market (persistence)
        if self._hurst_value > 0.5:
            # Long signal: Price crossed above SAR
            if candle.ClosePrice > sar_price and self.Position <= 0:
                # Close any existing short position
                if self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))

                # Open long position
                self.BuyMarket(self.Volume)
                self.LogInfo(f"Long signal: SAR= float({sar_price}, Price={candle.ClosePrice}, Hurst={self._hurst_value}"))
            # Short signal: Price crossed below SAR
            elif candle.ClosePrice < sar_price and self.Position >= 0:
                # Close any existing long position
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))

                # Open short position
                self.SellMarket(self.Volume)
                self.LogInfo(f"Short signal: SAR= float({sar_price}, Price={candle.ClosePrice}, Hurst={self._hurst_value}"))
        else:
            # If Hurst < 0.5, consider closing positions as market is not trending
            if self.Position != 0:
                self.LogInfo(f"Closing position as Hurst < 0.5: Hurst={self._hurst_value}")
                self.ClosePosition()

        # Update previous SAR value
        self._prev_sar_value = current_sar_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return parabolic_sar_hurst_strategy()