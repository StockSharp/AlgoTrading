import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class atr_mean_reversion_strategy(Strategy):
    """
    ATR Mean Reversion strategy.
    Trades when price deviates from its average by a multiple of ATR.
    """

    def __init__(self):
        super(atr_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._maPeriod = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Period for Moving Average", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10)

        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR indicator", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetRange(0.1, float('inf')) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for entry threshold", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "Common")

        # Indicators
        self._sma = None
        self._atr = None

    @property
    def MaPeriod(self):
        """Moving average period."""
        return self._maPeriod.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def AtrPeriod(self):
        """ATR indicator period."""
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def Multiplier(self):
        """ATR multiplier for entry threshold."""
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        super(atr_mean_reversion_strategy, self).OnStarted(time)

        # Create indicators
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.MaPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._sma, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(2, UnitTypes.Absolute)
        )
    def ProcessCandle(self, candle, sma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate entry thresholds
        upper_threshold = sma_value + self.Multiplier * atr_value
        lower_threshold = sma_value - self.Multiplier * atr_value

        # Long setup - price below lower threshold
        if candle.ClosePrice < lower_threshold and self.Position <= 0:
            # Buy signal - price has deviated too much below average
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Short setup - price above upper threshold
        elif candle.ClosePrice > upper_threshold and self.Position >= 0:
            # Sell signal - price has deviated too much above average
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        # Exit long position when price returns to average
        elif self.Position > 0 and candle.ClosePrice >= sma_value:
            # Close long position
            self.SellMarket(self.Position)
        # Exit short position when price returns to average
        elif self.Position < 0 and candle.ClosePrice <= sma_value:
            # Close short position
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return atr_mean_reversion_strategy()
