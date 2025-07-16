import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class momentum_divergence_strategy(Strategy):
    """
    Momentum Divergence strategy.
    Trades based on divergence between price and momentum.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(momentum_divergence_strategy, self).__init__()

        # Initialize strategy parameters
        self._momentumPeriod = self.Param("MomentumPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Period", "Period for Momentum indicator", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._maPeriod = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period for Moving Average", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "Common")

        # Initialize internal state
        self._momentum = None
        self._sma = None
        self._prevPrice = 0
        self._prevMomentum = 0
        self._currentPrice = 0
        self._currentMomentum = 0

    @property
    def MomentumPeriod(self):
        """Momentum indicator period."""
        return self._momentumPeriod.Value

    @MomentumPeriod.setter
    def MomentumPeriod(self, value):
        self._momentumPeriod.Value = value

    @property
    def MaPeriod(self):
        """Moving average period."""
        return self._maPeriod.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Initializes indicators and subscriptions.
        """
        super(momentum_divergence_strategy, self).OnStarted(time)

        # Create indicators
        self._momentum = Momentum()
        self._momentum.Length = self.MomentumPeriod
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.MaPeriod

        # Reset state
        self._prevPrice = 0
        self._prevMomentum = 0
        self._currentPrice = 0
        self._currentMomentum = 0

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._momentum, self._sma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._momentum)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, momentumValue, smaValue):
        """
        Process candle and execute divergence-based trading logic.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store previous values before updating current ones
        self._prevPrice = self._currentPrice
        self._prevMomentum = self._currentMomentum

        # Update current values
        self._currentPrice = candle.ClosePrice
        self._currentMomentum = momentumValue

        # Skip first candle after indicators become formed
        if self._prevPrice == 0 or self._prevMomentum == 0:
            return

        # Detect bullish divergence (price makes lower low but momentum makes higher low)
        bullishDivergence = self._currentPrice < self._prevPrice and self._currentMomentum > self._prevMomentum

        # Detect bearish divergence (price makes higher high but momentum makes lower high)
        bearishDivergence = self._currentPrice > self._prevPrice and self._currentMomentum < self._prevMomentum

        # Trading signals
        if bullishDivergence and self.Position <= 0:
            # Bullish divergence - buy signal
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif bearishDivergence and self.Position >= 0:
            # Bearish divergence - sell signal
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        # Exit when price crosses MA in the opposite direction
        elif self.Position > 0 and candle.ClosePrice < smaValue:
            # Exit long position
            self.SellMarket(self.Position)
        elif self.Position < 0 and candle.ClosePrice > smaValue:
            # Exit short position
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return momentum_divergence_strategy()
