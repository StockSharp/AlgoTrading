import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Unit
from StockSharp.Messages import UnitTypes
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class vwap_with_behavioral_bias_filter_strategy(Strategy):
    """
    VWAP with Behavioral Bias Filter strategy.
    Entry condition:
    Long: Price < VWAP && Bias_Score < -Threshold (oversold with panic)
    Short: Price > VWAP && Bias_Score > Threshold (overbought with euphoria)
    Exit condition:
    Long: Price > VWAP
    Short: Price < VWAP

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(vwap_with_behavioral_bias_filter_strategy, self).__init__()

        # Strategy parameters
        self._biasThreshold = self.Param("BiasThreshold", 0.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Bias Threshold", "Threshold for behavioral bias", "Behavioral Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(0.3, 0.7, 0.1)

        self._biasWindowSize = self.Param("BiasWindowSize", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bias Window Size", "Window size for behavioral bias calculation", "Behavioral Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._stopLoss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (%)", "Stop Loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Indicators and state variables
        self._vwap = None
        self._currentBiasScore = 0.0
        self._recentPriceMovements = []
        self._isLong = False
        self._isShort = False

    @property
    def BiasThreshold(self):
        """Behavioral bias threshold for entry signal."""
        return self._biasThreshold.Value

    @BiasThreshold.setter
    def BiasThreshold(self, value):
        self._biasThreshold.Value = value

    @property
    def BiasWindowSize(self):
        """Window size for behavioral bias calculation."""
        return self._biasWindowSize.Value

    @BiasWindowSize.setter
    def BiasWindowSize(self, value):
        self._biasWindowSize.Value = value

    @property
    def StopLoss(self):
        """Stop loss percentage."""
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        return [
            (self.Security, self.CandleType)
        ]

    def OnStarted(self, time):
        super(vwap_with_behavioral_bias_filter_strategy, self).OnStarted(time)

        # Initialize flags
        self._isLong = False
        self._isShort = False
        self._currentBiasScore = 0
        self._recentPriceMovements = []

        # Initialize VWAP indicator
        self._vwap = VolumeWeightedMovingAverage()

        # Subscribe to candles and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._vwap, self.ProcessCandle).Start()

        # Create chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._vwap)
            self.DrawOwnTrades(area)

        # Enable position protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLoss, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, vwapValue):
        """Process each candle and VWAP value."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Update behavioral bias score
        self.UpdateBehavioralBias(candle)

        price = candle.ClosePrice
        priceBelowVwap = price < vwapValue
        priceAboveVwap = price > vwapValue

        # Trading logic

        # Entry conditions

        # Long entry: Price below VWAP and negative bias score (panic)
        if priceBelowVwap and self._currentBiasScore < -self.BiasThreshold and not self._isLong and self.Position <= 0:
            self.LogInfo("Long signal: Price {0} < VWAP {1}, Bias {2} < -Threshold {3}".format(price, vwapValue, self._currentBiasScore, -self.BiasThreshold))
            self.BuyMarket(self.Volume)
            self._isLong = True
            self._isShort = False
        # Short entry: Price above VWAP and positive bias score (euphoria)
        elif priceAboveVwap and self._currentBiasScore > self.BiasThreshold and not self._isShort and self.Position >= 0:
            self.LogInfo("Short signal: Price {0} > VWAP {1}, Bias {2} > Threshold {3}".format(price, vwapValue, self._currentBiasScore, self.BiasThreshold))
            self.SellMarket(self.Volume)
            self._isShort = True
            self._isLong = False

        # Exit conditions

        # Exit long: Price rises above VWAP
        if self._isLong and priceAboveVwap and self.Position > 0:
            self.LogInfo("Exit long: Price {0} > VWAP {1}".format(price, vwapValue))
            self.SellMarket(abs(self.Position))
            self._isLong = False
        # Exit short: Price falls below VWAP
        elif self._isShort and priceBelowVwap and self.Position < 0:
            self.LogInfo("Exit short: Price {0} < VWAP {1}".format(price, vwapValue))
            self.BuyMarket(abs(self.Position))
            self._isShort = False

    def UpdateBehavioralBias(self, candle):
        """Update behavioral bias score based on recent price movements.
        This is a simplified model of behavioral biases in markets."""
        # Calculate price movement %
        priceChange = 0
        if candle.OpenPrice != 0:
            priceChange = (candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice * 100

        # Add to queue
        self._recentPriceMovements.append(priceChange)

        # Maintain window size
        while len(self._recentPriceMovements) > self.BiasWindowSize:
            self._recentPriceMovements.pop(0)

        # Not enough data yet
        if len(self._recentPriceMovements) < 5:
            self._currentBiasScore = 0
            return

        # Calculate various components of bias score

        # 1. Recent momentum (last 5 candles)
        recentMovement = sum(self._recentPriceMovements[-5:])

        # 2. Overreaction to recent news (volatility of recent moves)
        avg = sum(self._recentPriceMovements) / len(self._recentPriceMovements)
        variance = sum(x * x for x in self._recentPriceMovements) / len(self._recentPriceMovements) - avg * avg
        volatility = Math.Sqrt(max(0, variance))

        # 3. Herding behavior (consecutive moves in same direction)
        previousMove = 0
        consecutiveSameDirection = 0
        maxConsecutive = 0
        for movement in self._recentPriceMovements:
            if previousMove != 0 and Math.Sign(movement) == Math.Sign(previousMove):
                consecutiveSameDirection += 1
                maxConsecutive = max(maxConsecutive, consecutiveSameDirection)
            else:
                consecutiveSameDirection = 0
            previousMove = movement

        # 4. Current candle characteristics
        bodySize = abs(candle.ClosePrice - candle.OpenPrice)
        totalSize = candle.HighPrice - candle.LowPrice
        bodyRatio = bodySize / totalSize if totalSize > 0 else 0

        # Combined bias score calculation
        self._currentBiasScore = 0

        # Recent momentum component (range -0.5 to 0.5)
        self._currentBiasScore += min(0.5, max(-0.5, recentMovement / 2))

        # Volatility component (range -0.3 to 0.3)
        # Higher volatility often indicates panic or euphoria
        self._currentBiasScore += Math.Sign(recentMovement) * min(0.3, volatility / 10)

        # Herding component (range -0.2 to 0.2)
        self._currentBiasScore += Math.Sign(recentMovement) * min(0.2, maxConsecutive / 10.0)

        # Current candle strength component (range -0.2 to 0.2)
        if candle.ClosePrice > candle.OpenPrice:
            self._currentBiasScore += bodyRatio * 0.2  # Bullish bias
        else:
            self._currentBiasScore -= bodyRatio * 0.2  # Bearish bias

        # Ensure score is between -1 and 1
        self._currentBiasScore = max(-1.0, min(1.0, self._currentBiasScore))

        self.LogInfo(
            "Behavioral Bias: {0}, Components: Momentum={1}, Volatility={2}, Herding={3}, Candle={4}".format(
                self._currentBiasScore,
                recentMovement / 2,
                volatility / 10,
                maxConsecutive / 10.0,
                bodyRatio * 0.2)
        )

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return vwap_with_behavioral_bias_filter_strategy()
