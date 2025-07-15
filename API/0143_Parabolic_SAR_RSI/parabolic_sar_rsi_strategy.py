import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Unit
from StockSharp.Messages import UnitTypes
from StockSharp.Algo.Indicators import ParabolicSar, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class parabolic_sar_rsi_strategy(Strategy):
    """
    Strategy that combines Parabolic SAR for trend direction
    and RSI for entry confirmation with oversold/overbought conditions.
    """

    def __init__(self):
        super(parabolic_sar_rsi_strategy, self).__init__()

        # Initialize strategy parameters
        self._sar_af = self.Param("SarAf", 0.02) \
            .SetRange(0.01, 0.1) \
            .SetDisplay("SAR Acceleration Factor", "Initial acceleration factor for Parabolic SAR", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(0.01, 0.05, 0.01)

        self._sar_max_af = self.Param("SarMaxAf", 0.2) \
            .SetRange(0.1, 0.5) \
            .SetDisplay("SAR Max Acceleration", "Maximum acceleration factor for Parabolic SAR", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(0.1, 0.3, 0.1)

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetNotNegative() \
            .SetDisplay("RSI Oversold", "RSI level considered oversold", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(20.0, 40.0, 5.0)

        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetNotNegative() \
            .SetDisplay("RSI Overbought", "RSI level considered overbought", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(60.0, 80.0, 5.0)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def sar_af(self):
        """Parabolic SAR acceleration factor."""
        return self._sar_af.Value

    @sar_af.setter
    def sar_af(self, value):
        self._sar_af.Value = value

    @property
    def sar_max_af(self):
        """Parabolic SAR maximum acceleration factor."""
        return self._sar_max_af.Value

    @sar_max_af.setter
    def sar_max_af(self, value):
        self._sar_max_af.Value = value

    @property
    def rsi_period(self):
        """RSI period."""
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def rsi_oversold(self):
        """RSI oversold level."""
        return self._rsi_oversold.Value

    @rsi_oversold.setter
    def rsi_oversold(self, value):
        self._rsi_oversold.Value = value

    @property
    def rsi_overbought(self):
        """RSI overbought level."""
        return self._rsi_overbought.Value

    @rsi_overbought.setter
    def rsi_overbought(self, value):
        self._rsi_overbought.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(parabolic_sar_rsi_strategy, self).OnStarted(time)

        # Create indicators
        parabolic_sar = ParabolicSar()
        parabolic_sar.Acceleration = self.sar_af
        parabolic_sar.AccelerationMax = self.sar_max_af
        parabolic_sar.AccelerationStep = self.sar_af  # Using initial AF as the step

        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(parabolic_sar, rsi, self.ProcessCandles).Start()

        # Enable dynamic stop-loss using Parabolic SAR
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),  # No take profit
            Unit(0, UnitTypes.Absolute)   # No fixed stop loss - using dynamic SAR
        )

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, parabolic_sar)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def ProcessCandles(self, candle, sar_value, rsi_value):
        """Process candles and indicator values."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Long entry: price above SAR and RSI oversold
        if candle.ClosePrice > sar_value and rsi_value < self.rsi_oversold and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        # Short entry: price below SAR and RSI overbought
        elif candle.ClosePrice < sar_value and rsi_value > self.rsi_overbought and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Long exit: price falls below SAR (trend change)
        elif self.Position > 0 and candle.ClosePrice < sar_value:
            self.SellMarket(Math.Abs(self.Position))
        # Short exit: price rises above SAR (trend change)
        elif self.Position < 0 and candle.ClosePrice > sar_value:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return parabolic_sar_rsi_strategy()
