import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class parabolic_sar_volatility_expansion_strategy(Strategy):
    """
    Strategy based on Parabolic SAR with Volatility Expansion detection.
    """

    def __init__(self):
        # Constructor.
        super(parabolic_sar_volatility_expansion_strategy, self).__init__()

        # SAR acceleration factor parameter.
        self._sar_af = self.Param("SarAf", 0.02) \
            .SetGreaterThanZero() \
            .SetDisplay("SAR AF", "Parabolic SAR acceleration factor", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(0.01, 0.05, 0.01)

        # SAR maximum acceleration factor parameter.
        self._sar_max_af = self.Param("SarMaxAf", 0.2) \
            .SetGreaterThanZero() \
            .SetDisplay("SAR Max AF", "Parabolic SAR maximum acceleration factor", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(0.1, 0.3, 0.05)

        # ATR period parameter.
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 28, 7)

        # Volatility expansion factor parameter.
        self._volatility_expansion_factor = self.Param("VolatilityExpansionFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Volatility Expansion Factor", "Factor for volatility expansion detection", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        # Candle type parameter.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Indicators
        self._atr_sma = None
        self._atr_std_dev = None

    @property
    def SarAf(self):
        return self._sar_af.Value

    @SarAf.setter
    def SarAf(self, value):
        self._sar_af.Value = value

    @property
    def SarMaxAf(self):
        return self._sar_max_af.Value

    @SarMaxAf.setter
    def SarMaxAf(self, value):
        self._sar_max_af.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def VolatilityExpansionFactor(self):
        return self._volatility_expansion_factor.Value

    @VolatilityExpansionFactor.setter
    def VolatilityExpansionFactor(self, value):
        self._volatility_expansion_factor.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(parabolic_sar_volatility_expansion_strategy, self).OnStarted(time)

        # Create indicators
        parabolic_sar = ParabolicSar()
        parabolic_sar.Acceleration = self.SarAf
        parabolic_sar.AccelerationMax = self.SarMaxAf

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        self._atr_sma = SimpleMovingAverage()
        self._atr_sma.Length = self.AtrPeriod
        self._atr_std_dev = StandardDeviation()
        self._atr_std_dev.Length = self.AtrPeriod

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(parabolic_sar, atr, self._on_candle)
        subscription.Start()

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, parabolic_sar)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _on_candle(self, candle, sar_value, atr_value):
        # Calculate ATR average and standard deviation
        atr_avg = to_float(process_float(self._atr_sma, atr_value, candle.ServerTime, candle.State == CandleStates.Finished))
        atr_std = to_float(process_float(self._atr_std_dev, atr_value, candle.ServerTime, candle.State == CandleStates.Finished))

        # Process the strategy logic
        self.ProcessStrategy(candle, sar_value, atr_value, atr_avg, atr_std)

    def ProcessStrategy(self, candle, sar_value, atr_value, atr_avg, atr_std_dev):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check if volatility is expanding
        volatility_threshold = atr_avg + (self.VolatilityExpansionFactor * atr_std_dev)
        is_volatility_expanding = atr_value > volatility_threshold

        # Trading logic - only trade during volatility expansion
        if is_volatility_expanding:
            # Check price relative to SAR
            is_above_sar = candle.ClosePrice > sar_value
            is_below_sar = candle.ClosePrice < sar_value

            if is_above_sar and self.Position <= 0:
                # Price above SAR with volatility expansion - Go long
                self.CancelActiveOrders()

                # Calculate position size
                volume = self.Volume + Math.Abs(self.Position)

                # Enter long position
                self.BuyMarket(volume)
            elif is_below_sar and self.Position >= 0:
                # Price below SAR with volatility expansion - Go short
                self.CancelActiveOrders()

                # Calculate position size
                volume = self.Volume + Math.Abs(self.Position)

                # Enter short position
                self.SellMarket(volume)

        # Exit logic - when price crosses SAR
        if (self.Position > 0 and candle.ClosePrice < sar_value) or \
                (self.Position < 0 and candle.ClosePrice > sar_value):
            # Close position
            self.ClosePosition()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return parabolic_sar_volatility_expansion_strategy()
