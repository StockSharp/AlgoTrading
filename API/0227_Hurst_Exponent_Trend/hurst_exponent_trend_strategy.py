import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import HurstExponent, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class hurst_exponent_trend_strategy(Strategy):
    """
    Hurst Exponent Trend strategy.
    Uses Hurst exponent to identify trending markets.
    """

    def __init__(self):
        super(hurst_exponent_trend_strategy, self).__init__()

        # Hurst exponent calculation period.
        self._hurstPeriod = self.Param("HurstPeriod", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Hurst Period", "Period for Hurst exponent calculation", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(50, 150, 25)

        # Moving average period.
        self._maPeriod = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period for Moving Average", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10)

        # Hurst exponent threshold for trend identification.
        self._hurstThreshold = self.Param("HurstThreshold", 0.55) \
            .SetRange(0.1, 0.9) \
            .SetDisplay("Hurst Threshold", "Threshold value for trend identification", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(0.5, 0.6, 0.05)

        # Candle type for strategy.
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "Common")

        # Internal indicators
        self._hurst = None
        self._sma = None

    @property
    def HurstPeriod(self):
        """Hurst exponent calculation period."""
        return self._hurstPeriod.Value

    @HurstPeriod.setter
    def HurstPeriod(self, value):
        self._hurstPeriod.Value = value

    @property
    def MaPeriod(self):
        """Moving average period."""
        return self._maPeriod.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def HurstThreshold(self):
        """Hurst exponent threshold for trend identification."""
        return self._hurstThreshold.Value

    @HurstThreshold.setter
    def HurstThreshold(self, value):
        self._hurstThreshold.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        """Return securities and candle types used by the strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(hurst_exponent_trend_strategy, self).OnStarted(time)

        # Create indicators
        self._hurst = HurstExponent()
        self._hurst.Length = self.HurstPeriod
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.MaPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._hurst, self._sma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),  # No take profit
            stopLoss=Unit(2, UnitTypes.Percent)       # 2% stop loss
        )

    def ProcessCandle(self, candle, hurstValue, smaValue):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check if market is trending (Hurst > 0.5 indicates trending market)
        isTrending = hurstValue > self.HurstThreshold

        if isTrending:
            # In trending markets, use price relative to MA to determine direction

            # Long setup - trending market with price above MA
            if candle.ClosePrice > smaValue and self.Position <= 0:
                # Buy signal - trending market with price above MA
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            # Short setup - trending market with price below MA
            elif candle.ClosePrice < smaValue and self.Position >= 0:
                # Sell signal - trending market with price below MA
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        else:
            # In non-trending markets, exit positions
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hurst_exponent_trend_strategy()
