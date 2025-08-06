import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HullMovingAverage, CommodityChannelIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class hull_ma_cci_strategy(Strategy):
    """Strategy based on Hull Moving Average and CCI indicators."""

    # Constructor
    def __init__(self):
        super(hull_ma_cci_strategy, self).__init__()

        # Hull MA period
        self._hull_period = self.Param("HullPeriod", 9) \
            .SetRange(5, 20) \
            .SetDisplay("Hull MA Period", "Period for Hull Moving Average", "Indicators") \
            .SetCanOptimize(True)

        # CCI period
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetRange(10, 50) \
            .SetDisplay("CCI Period", "Period for CCI indicator", "Indicators") \
            .SetCanOptimize(True)

        # ATR period for stop-loss
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetRange(7, 28) \
            .SetDisplay("ATR Period", "ATR period for stop-loss calculation", "Risk Management") \
            .SetCanOptimize(True)

        # ATR multiplier for stop-loss
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetRange(1.0, 4.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management") \
            .SetCanOptimize(True)

        # Candle type for strategy
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._previous_hull_value = 0.0

    @property
    def hull_period(self):
        """Hull MA period."""
        return self._hull_period.Value

    @hull_period.setter
    def hull_period(self, value):
        self._hull_period.Value = value

    @property
    def cci_period(self):
        """CCI period."""
        return self._cci_period.Value

    @cci_period.setter
    def cci_period(self, value):
        self._cci_period.Value = value

    @property
    def atr_period(self):
        """ATR period for stop-loss."""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def atr_multiplier(self):
        """ATR multiplier for stop-loss."""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(hull_ma_cci_strategy, self).OnReseted()
        self._previous_hull_value = 0.0

    def OnStarted(self, time):
        super(hull_ma_cci_strategy, self).OnStarted(time)
        # Initialize indicators
        hull_ma = HullMovingAverage()
        hull_ma.Length = self.hull_period
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        atr = AverageTrueRange()
        atr.Length = self.atr_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hull_ma, cci, atr, self.ProcessIndicators).Start()

        # Enable ATR-based stop protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.atr_multiplier, UnitTypes.Absolute)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull_ma)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, hull_value, cci_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store previous Hull value for slope detection
        previous_hull_value = self._previous_hull_value
        self._previous_hull_value = hull_value

        # Skip first candle until we have previous value
        if previous_hull_value == 0:
            return

        # Trading logic:
        # Long: HMA(t) > HMA(t-1) && CCI < -100 (HMA rising with oversold conditions)
        # Short: HMA(t) < HMA(t-1) && CCI > 100 (HMA falling with overbought conditions)

        hull_slope = hull_value > previous_hull_value

        if hull_slope and cci_value < -100 and self.Position <= 0:
            # Buy signal - HMA rising with oversold CCI
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif not hull_slope and cci_value > 100 and self.Position >= 0:
            # Sell signal - HMA falling with overbought CCI
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Exit conditions based on HMA slope change
        elif self.Position > 0 and not hull_slope:
            # Exit long position when HMA starts falling
            self.SellMarket(self.Position)
        elif self.Position < 0 and hull_slope:
            # Exit short position when HMA starts rising
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hull_ma_cci_strategy()

