import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("System.Collections")

from System import TimeSpan, Math, DateTimeOffset
from System.Drawing import Color
from System.Collections.Generic import Queue
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class trendline_bounce_strategy(Strategy):
    """
    Trendline Bounce strategy.
    The strategy automatically identifies trendlines by connecting highs or lows
    and enters positions when price bounces off a trendline with confirmation.
    
    """
    def __init__(self):
        super(trendline_bounce_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._trendlinePeriodParam = self.Param("TrendlinePeriod", 20) \
            .SetDisplay("Trendline Period", "Number of candles to use for trendline calculation", "Indicators")
        
        self._maPeriodParam = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average calculation", "Indicators")
        
        self._bounceThresholdPercentParam = self.Param("BounceThresholdPercent", 0.5) \
            .SetDisplay("Bounce Threshold %", "Maximum distance from trendline for bounce detection", "Indicators")
        
        self._candleTypeParam = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        self._stopLossPercentParam = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
        
        # Indicators
        self._ma = None
        
        # Store recent candles for trendline calculation
        self._recentCandles = Queue[ICandleMessage]()
        
        # Trendline parameters
        self._supportSlope = 0.0
        self._supportIntercept = 0.0
        self._resistanceSlope = 0.0
        self._resistanceIntercept = 0.0
        self._lastTrendlineUpdate = DateTimeOffset.MinValue

    @property
    def TrendlinePeriod(self):
        return self._trendlinePeriodParam.Value

    @TrendlinePeriod.setter
    def TrendlinePeriod(self, value):
        self._trendlinePeriodParam.Value = value

    @property
    def MAPeriod(self):
        return self._maPeriodParam.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriodParam.Value = value

    @property
    def BounceThresholdPercent(self):
        return self._bounceThresholdPercentParam.Value

    @BounceThresholdPercent.setter
    def BounceThresholdPercent(self, value):
        self._bounceThresholdPercentParam.Value = value

    @property
    def CandleType(self):
        return self._candleTypeParam.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleTypeParam.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercentParam.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercentParam.Value = value

    def OnReseted(self):
        """Resets internal state when the strategy is reset."""
        super(trendline_bounce_strategy, self).OnReseted()
        self._ma = None
        self._recentCandles.Clear()
        self._supportSlope = 0.0
        self._supportIntercept = 0.0
        self._resistanceSlope = 0.0
        self._resistanceIntercept = 0.0
        self._lastTrendlineUpdate = DateTimeOffset.MinValue

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(trendline_bounce_strategy, self).OnStarted(time)

        # Create MA indicator
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MAPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, maPrice):
        """
        Process each finished candle and execute trading logic.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Add current candle to the queue and maintain queue size
        self._recentCandles.Enqueue(candle)
        while self._recentCandles.Count > self.TrendlinePeriod and self._recentCandles.Count > 0:
            self._recentCandles.Dequeue()

        # Update trendlines periodically
        if (self._lastTrendlineUpdate == DateTimeOffset.MinValue or 
            (candle.OpenTime - self._lastTrendlineUpdate).TotalMinutes >= self.TrendlinePeriod):
            self.UpdateTrendlines()
            self._lastTrendlineUpdate = candle.OpenTime

        # Check for trendline bounces
        self.CheckForTrendlineBounces(candle, maPrice)

    def UpdateTrendlines(self):
        """
        Update support and resistance trendlines based on recent candles.
        """
        if self._recentCandles.Count < 3:
            return

        candles = list(self._recentCandles)
        n = len(candles)
        
        # Calculate support trendline (connecting lows)
        supportPoints = []
        
        # Find significant lows for support line
        for i in range(1, n - 1):
            # A point is a low if it's lower than both neighbors
            if (candles[i].LowPrice < candles[i-1].LowPrice and 
                candles[i].LowPrice < candles[i+1].LowPrice):
                supportPoints.append((float(i), float(candles[i].LowPrice)))
        
        # Calculate resistance trendline (connecting highs)
        resistancePoints = []
        
        # Find significant highs for resistance line
        for i in range(1, n - 1):
            # A point is a high if it's higher than both neighbors
            if (candles[i].HighPrice > candles[i-1].HighPrice and 
                candles[i].HighPrice > candles[i+1].HighPrice):
                resistancePoints.append((float(i), float(candles[i].HighPrice)))

        # We need at least 2 points to define a line
        if len(supportPoints) >= 2:
            self._supportSlope, self._supportIntercept = self.CalculateLinearRegression(supportPoints)
            self.LogInfo("Updated support trendline: y = {0}x + {1}", 
                       self._supportSlope, self._supportIntercept)
        
        if len(resistancePoints) >= 2:
            self._resistanceSlope, self._resistanceIntercept = self.CalculateLinearRegression(resistancePoints)
            self.LogInfo("Updated resistance trendline: y = {0}x + {1}", 
                       self._resistanceSlope, self._resistanceIntercept)

    def CalculateLinearRegression(self, points):
        """
        Calculate linear regression for a set of points.
        Returns slope and intercept.
        """
        n = len(points)
        sumX = 0.0
        sumY = 0.0
        sumXY = 0.0
        sumX2 = 0.0

        for x, y in points:
            sumX += x
            sumY += y
            sumXY += x * y
            sumX2 += x * x

        denominator = n * sumX2 - sumX * sumX
        
        # Avoid division by zero
        if denominator == 0:
            slope = 0.0
            intercept = sumY / n if n > 0 else 0.0
            return slope, intercept

        slope = (n * sumXY - sumX * sumY) / denominator
        intercept = (sumY - slope * sumX) / n
        
        return slope, intercept

    def CheckForTrendlineBounces(self, candle, maPrice):
        """
        Check for trendline bounces and execute trading logic.
        """
        if self._recentCandles.Count < 3:
            return

        # Calculate the x-coordinate for the current candle
        x = float(self._recentCandles.Count - 1)
        
        # Calculate trendline values at current x
        supportValue = self._supportSlope * x + self._supportIntercept
        resistanceValue = self._resistanceSlope * x + self._resistanceIntercept
        
        self.LogInfo("Current candle: Close={0}, Support={1}, Resistance={2}", 
                   candle.ClosePrice, supportValue, resistanceValue)
        
        # Determine if price is near a trendline
        bounceThreshold = float(candle.ClosePrice * (self.BounceThresholdPercent / 100))
        
        nearSupport = abs(candle.LowPrice - supportValue) <= bounceThreshold
        nearResistance = abs(candle.HighPrice - resistanceValue) <= bounceThreshold
        
        # Check for bullish bounce off support
        if (nearSupport and candle.ClosePrice > candle.OpenPrice and 
            candle.ClosePrice > supportValue and self.Position <= 0):
            # Bullish candle bouncing off support - go long
            if maPrice < candle.ClosePrice:  # Only go long if price is above MA (uptrend)
                self.CancelActiveOrders()
                self.BuyMarket(self.Volume + abs(self.Position))
                self.LogInfo("Long entry at {0} on support bounce at {1}", 
                           candle.ClosePrice, supportValue)

        # Check for bearish bounce off resistance
        elif (nearResistance and candle.ClosePrice < candle.OpenPrice and 
              candle.ClosePrice < resistanceValue and self.Position >= 0):
            # Bearish candle bouncing off resistance - go short
            if maPrice > candle.ClosePrice:  # Only go short if price is below MA (downtrend)
                self.CancelActiveOrders()
                self.SellMarket(self.Volume + abs(self.Position))
                self.LogInfo("Short entry at {0} on resistance bounce at {1}", 
                           candle.ClosePrice, resistanceValue)

        # Exit logic based on MA crossover
        if self.Position > 0 and candle.ClosePrice < maPrice:
            self.SellMarket(abs(self.Position))
            self.LogInfo("Long exit at {0} (price below MA {1})", candle.ClosePrice, maPrice)

        elif self.Position < 0 and candle.ClosePrice > maPrice:
            self.BuyMarket(abs(self.Position))
            self.LogInfo("Short exit at {0} (price above MA {1})", candle.ClosePrice, maPrice)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return trendline_bounce_strategy()