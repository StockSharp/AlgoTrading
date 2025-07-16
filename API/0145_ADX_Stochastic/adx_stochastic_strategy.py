import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class adx_stochastic_strategy(Strategy):
    """
    Strategy that combines ADX (Average Directional Index) for trend strength
    and Stochastic Oscillator for entry timing with oversold/overbought conditions.

    """

    def __init__(self):
        super(adx_stochastic_strategy, self).__init__()

        # Initialize strategy parameters
        self._adxPeriod = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period of the ADX indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._adxThreshold = self.Param("AdxThreshold", 25.0) \
            .SetNotNegative() \
            .SetDisplay("ADX Threshold", "ADX level considered strong trend", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(15.0, 35.0, 5.0)

        self._stochPeriod = self.Param("StochPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Period of the Stochastic Oscillator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 5)

        self._stochK = self.Param("StochK", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %K", "Smoothing of the %K line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        self._stochD = self.Param("StochD", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %D", "Smoothing of the %D line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        self._stochOversold = self.Param("StochOversold", 20.0) \
            .SetNotNegative() \
            .SetDisplay("Stochastic Oversold", "Level considered oversold", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10.0, 30.0, 5.0)

        self._stochOverbought = self.Param("StochOverbought", 80.0) \
            .SetNotNegative() \
            .SetDisplay("Stochastic Overbought", "Level considered overbought", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(70.0, 90.0, 5.0)

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def AdxPeriod(self):
        """ADX period."""
        return self._adxPeriod.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adxPeriod.Value = value

    @property
    def AdxThreshold(self):
        """ADX threshold for strong trend."""
        return self._adxThreshold.Value

    @AdxThreshold.setter
    def AdxThreshold(self, value):
        self._adxThreshold.Value = value

    @property
    def StochPeriod(self):
        """Stochastic period."""
        return self._stochPeriod.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stochPeriod.Value = value

    @property
    def StochK(self):
        """Stochastic %K period."""
        return self._stochK.Value

    @StochK.setter
    def StochK(self, value):
        self._stochK.Value = value

    @property
    def StochD(self):
        """Stochastic %D period."""
        return self._stochD.Value

    @StochD.setter
    def StochD(self, value):
        self._stochD.Value = value

    @property
    def StochOversold(self):
        """Stochastic oversold level."""
        return self._stochOversold.Value

    @StochOversold.setter
    def StochOversold(self, value):
        self._stochOversold.Value = value

    @property
    def StochOverbought(self):
        """Stochastic overbought level."""
        return self._stochOverbought.Value

    @StochOverbought.setter
    def StochOverbought(self, value):
        self._stochOverbought.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(adx_stochastic_strategy, self).OnStarted(time)

        # Create ADX indicator with all components
        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        # Create Stochastic indicator
        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochK
        stochastic.D.Length = self.StochD

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(adx, stochastic, self.ProcessIndicators).Start()

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, adxValue, stochValue):
        """
        Process indicator values.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract ADX value from indicator
        if adxValue.MovingAverage is None:
            return
        adx = float(adxValue.MovingAverage)

        stoch = stochValue
        stochK = stoch.K

        # Check if ADX indicates strong trend
        isStrongTrend = adx > self.AdxThreshold

        if isStrongTrend:
            # Determine trend direction using DI+ and DI- (using candle direction as a simple proxy)
            isBullishTrend = candle.OpenPrice < candle.ClosePrice
            isBearishTrend = candle.OpenPrice > candle.ClosePrice

            # Long entry: strong bullish trend with Stochastic oversold
            if isBullishTrend and stochK < self.StochOversold and self.Position <= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)
            # Short entry: strong bearish trend with Stochastic overbought
            elif isBearishTrend and stochK > self.StochOverbought and self.Position >= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

        # Exit conditions
        if adx < self.AdxThreshold:
            # Exit all positions when trend weakens (ADX below threshold)
            if self.Position != 0:
                self.ClosePosition()

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return adx_stochastic_strategy()
