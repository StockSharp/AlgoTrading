import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes, Unit, DataType, ICandleMessage, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class hammer_candle_strategy(Strategy):
    """
    Hammer Candle strategy that enters long positions when a hammer candlestick pattern appears.
    
    """
    def __init__(self):
        super(hammer_candle_strategy, self).__init__()
        
        # Initialize internal state
        self._isPositionOpen = False

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use for pattern detection", "General")

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(hammer_candle_strategy, self).OnReseted()
        self._isPositionOpen = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(hammer_candle_strategy, self).OnStarted(time)

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        # Setup stop loss/take profit protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        # Position events will be handled in OnPositionReceived

    def ProcessCandle(self, candle):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Already have a position, wait for exit
        if self._isPositionOpen:
            return

        # Check for hammer pattern:
        # 1. Lower shadow is at least twice the size of the body
        # 2. Small or no upper shadow
        # 3. Current low is lower than previous low (downtrend)

        bodySize = float(Math.Abs(candle.OpenPrice - candle.ClosePrice))
        lowerShadow = float(Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice)
        upperShadow = float(candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice))

        isHammer = (lowerShadow > bodySize * 2 and
                   upperShadow < bodySize * 0.5 and
                   candle.ClosePrice > candle.OpenPrice)  # Bullish candle

        if isHammer:
            # Enter long position on hammer pattern
            self.BuyMarket(self.Volume)
            self._isPositionOpen = True

            self.LogInfo("Hammer pattern detected. Low: {0}, Body size: {1}, Lower shadow: {2}".format(
                candle.LowPrice, bodySize, lowerShadow))

    def OnPositionReceived(self, position):
        """Handle position information when strategy is started."""
        super(hammer_candle_strategy, self).OnPositionReceived(position)
        if self.Position == 0:
            self._isPositionOpen = False

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return hammer_candle_strategy()