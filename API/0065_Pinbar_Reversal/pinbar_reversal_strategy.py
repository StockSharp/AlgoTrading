import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class pinbar_reversal_strategy(Strategy):
    """
    Strategy based on Pinbar (Pin Bar) candlestick pattern.
    A pinbar is characterized by a small body with a long wick/tail, 
    signaling a potential reversal in the market.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(pinbar_reversal_strategy, self).__init__()
        
        # Initialize internal state
        self._ma = None

        # Initialize strategy parameters
        self._tailToBodyRatio = self.Param("TailToBodyRatio", 2.0) \
            .SetDisplay("Tail/Body Ratio", "Minimum ratio of tail to body length", "Pattern Parameters")

        self._oppositeTailRatio = self.Param("OppositeTailRatio", 0.5) \
            .SetDisplay("Opposite Tail Ratio", "Maximum ratio of opposite tail to body", "Pattern Parameters")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Percentage-based stop loss from entry", "Risk Management")

        self._useTrend = self.Param("UseTrend", True) \
            .SetDisplay("Use Trend Filter", "Whether to use MA trend filter", "Signal Parameters")

        self._maPeriod = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for the moving average trend filter", "Signal Parameters")

    @property
    def TailToBodyRatio(self):
        return self._tailToBodyRatio.Value

    @TailToBodyRatio.setter
    def TailToBodyRatio(self, value):
        self._tailToBodyRatio.Value = value

    @property
    def OppositeTailRatio(self):
        return self._oppositeTailRatio.Value

    @OppositeTailRatio.setter
    def OppositeTailRatio(self, value):
        self._oppositeTailRatio.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def UseTrend(self):
        return self._useTrend.Value

    @UseTrend.setter
    def UseTrend(self, value):
        self._useTrend.Value = value

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(pinbar_reversal_strategy, self).OnStarted(time)

        # Create moving average for trend detection
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MAPeriod

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind candle processing with the MA
        subscription.Bind(self._ma, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            Unit(0, UnitTypes.Absolute),  # No take profit (managed by exit conditions)
            Unit(self.StopLossPercent, UnitTypes.Percent),  # Stop loss at defined percentage
            False  # No trailing
        )

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, maValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param maValue: The Moving Average value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate candle body and shadows
        bodyLength = Math.Abs(candle.ClosePrice - candle.OpenPrice)
        upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice)
        lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice
        
        # Check for bullish pinbar (long lower shadow)
        isBullishPinbar = (lowerShadow > bodyLength * self.TailToBodyRatio and 
                          upperShadow < bodyLength * self.OppositeTailRatio)
        
        # Check for bearish pinbar (long upper shadow)
        isBearishPinbar = (upperShadow > bodyLength * self.TailToBodyRatio and 
                          lowerShadow < bodyLength * self.OppositeTailRatio)
        
        # Determine trend if needed
        isBullishTrend = not self.UseTrend or candle.ClosePrice > maValue
        isBearishTrend = not self.UseTrend or candle.ClosePrice < maValue
        
        self.LogInfo("Candle: {0}, Close: {1}, MA: {2}, Body: {3}, Upper: {4}, Lower: {5}".format(
            candle.OpenTime, candle.ClosePrice, maValue, bodyLength, upperShadow, lowerShadow))

        # Process long signals
        if isBullishPinbar and isBullishTrend and self.Position <= 0:
            # Bullish pinbar in bullish trend or no trend filter
            if self.Position < 0:
                # Close any existing short position
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Closed short position on bullish pinbar")

            # Open new long position
            self.BuyMarket(self.Volume)
            self.LogInfo("Buy signal: Bullish Pinbar detected at {0}, Body: {1:F4}, Lower Shadow: {2:F4}, Ratio: {3:F2}".format(
                candle.OpenTime, bodyLength, lowerShadow, lowerShadow/bodyLength if bodyLength > 0 else 0))
        # Process short signals
        elif isBearishPinbar and isBearishTrend and self.Position >= 0:
            # Bearish pinbar in bearish trend or no trend filter
            if self.Position > 0:
                # Close any existing long position
                self.SellMarket(self.Position)
                self.LogInfo("Closed long position on bearish pinbar")

            # Open new short position
            self.SellMarket(self.Volume)
            self.LogInfo("Sell signal: Bearish Pinbar detected at {0}, Body: {1:F4}, Upper Shadow: {2:F4}, Ratio: {3:F2}".format(
                candle.OpenTime, bodyLength, upperShadow, upperShadow/bodyLength if bodyLength > 0 else 0))
        # Exit signals based on opposite pinbars
        elif self.Position > 0 and isBearishPinbar:
            # Exit long position on bearish pinbar
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: Bearish pinbar appeared")
        elif self.Position < 0 and isBullishPinbar:
            # Exit short position on bullish pinbar
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: Bullish pinbar appeared")

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return pinbar_reversal_strategy()