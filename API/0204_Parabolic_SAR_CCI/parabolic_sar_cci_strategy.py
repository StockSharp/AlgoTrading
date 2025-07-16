import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class parabolic_sar_cci_strategy(Strategy):
    """Strategy based on Parabolic SAR and CCI indicators (#204)"""

    def __init__(self):
        super(parabolic_sar_cci_strategy, self).__init__()

        # Constructor
        self._sar_acceleration_factor = self.Param("SarAccelerationFactor", 0.02) \
            .SetRange(0.01, 0.05) \
            .SetDisplay("SAR AF", "Parabolic SAR acceleration factor", "Indicators") \
            .SetCanOptimize(True)

        self._sar_max_acceleration_factor = self.Param("SarMaxAccelerationFactor", 0.2) \
            .SetRange(0.1, 0.5) \
            .SetDisplay("SAR Max AF", "Parabolic SAR maximum acceleration factor", "Indicators") \
            .SetCanOptimize(True)

        self._cci_period = self.Param("CciPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("CCI Period", "Period for CCI indicator", "Indicators") \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def sar_acceleration_factor(self):
        """Parabolic SAR acceleration factor"""
        return self._sar_acceleration_factor.Value

    @sar_acceleration_factor.setter
    def sar_acceleration_factor(self, value):
        self._sar_acceleration_factor.Value = value

    @property
    def sar_max_acceleration_factor(self):
        """Parabolic SAR maximum acceleration factor"""
        return self._sar_max_acceleration_factor.Value

    @sar_max_acceleration_factor.setter
    def sar_max_acceleration_factor(self, value):
        self._sar_max_acceleration_factor.Value = value

    @property
    def cci_period(self):
        """CCI period"""
        return self._cci_period.Value

    @cci_period.setter
    def cci_period(self, value):
        self._cci_period.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy"""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super(parabolic_sar_cci_strategy, self).OnStarted(time)

        # Initialize indicators
        parabolic_sar = ParabolicSar()
        parabolic_sar.Acceleration = self.sar_acceleration_factor
        parabolic_sar.AccelerationMax = self.sar_max_acceleration_factor

        cci = CommodityChannelIndex()
        cci.Length = self.cci_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(parabolic_sar, cci, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, parabolic_sar)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, sar_value, cci_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        price = candle.ClosePrice

        # Trading logic:
        # Long: Price > SAR && CCI < -100 (trend up with oversold conditions)
        # Short: Price < SAR && CCI > 100 (trend down with overbought conditions)

        if price > sar_value and cci_value < -100 and self.Position <= 0:
            # Buy signal - trend up with oversold CCI
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif price < sar_value and cci_value > 100 and self.Position >= 0:
            # Sell signal - trend down with overbought CCI
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Exit conditions based on SAR breakout (dynamic stop-loss)
        elif self.Position > 0 and price < sar_value:
            # Exit long position when price drops below SAR
            self.SellMarket(self.Position)
        elif self.Position < 0 and price > sar_value:
            # Exit short position when price rises above SAR
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return parabolic_sar_cci_strategy()
