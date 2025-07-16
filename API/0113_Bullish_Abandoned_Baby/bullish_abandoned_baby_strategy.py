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
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class bullish_abandoned_baby_strategy(Strategy):
    """
    Strategy based on Bullish Abandoned Baby candlestick pattern.
    """
    def __init__(self):
        super(bullish_abandoned_baby_strategy, self).__init__()

        # Candle type and timeframe.
        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use for analysis", "Candles")

        # Stop-loss percent from entry price.
        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetRange(0.1, 5.0) \
            .SetDisplay("Stop Loss %", "Stop Loss percentage below the low of the doji candle", "Risk") \
            .SetCanOptimize(True)

        self._prevCandle1 = None
        self._prevCandle2 = None

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

    def OnStarted(self, time):
        super(bullish_abandoned_baby_strategy, self).OnStarted(time)

        # Reset pattern candles
        self._prevCandle1 = None
        self._prevCandle2 = None

        # Create and subscribe to candles
        subscription = self.SubscribeCandles(self.CandleType)

        subscription.Bind(self.ProcessCandle).Start()

        # Configure protection for open positions
        self.StartProtection(
            Unit(0),  # No take profit, using exit logic in the strategy
            Unit(self.StopLossPercent, UnitTypes.Percent),
            False
        )

        # Set up chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Add log entry for the candle
        self.LogInfo("Candle: Open={0}, High={1}, Low={2}, Close={3}".format(
            candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice))

        # If we have enough candles, check for the Bullish Abandoned Baby pattern
        if self._prevCandle2 is not None and self._prevCandle1 is not None:
            # Check for bullish abandoned baby pattern:
            # 1. First candle is bearish (close < open)
            # 2. Middle candle is a doji and gaps down (high < low of first candle)
            # 3. Current candle is bullish (close > open) and gaps up (low > high of middle candle)
            firstCandleBearish = self._prevCandle2.ClosePrice < self._prevCandle2.OpenPrice
            middleCandleGapsDown = self._prevCandle1.HighPrice < self._prevCandle2.LowPrice
            currentCandleBullish = candle.ClosePrice > candle.OpenPrice
            currentCandleGapsUp = candle.LowPrice > self._prevCandle1.HighPrice

            if firstCandleBearish and middleCandleGapsDown and currentCandleBullish and currentCandleGapsUp:
                self.LogInfo("Bullish Abandoned Baby pattern detected!")

                # Enter long position if we don't have one already
                if self.Position <= 0:
                    self.BuyMarket(self.Volume)
                    self.LogInfo("Long position opened: {0} at market".format(self.Volume))

        # Store current candle for next pattern check
        self._prevCandle2 = self._prevCandle1
        self._prevCandle1 = candle

        # Exit logic - if we're in a long position and price breaks above high of the current candle
        if self.Position > 0 and candle.HighPrice > (self._prevCandle2.HighPrice if self._prevCandle2 is not None else 0):
            self.LogInfo("Exit signal: Price broke above previous candle high")
            self.ClosePosition()

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return bullish_abandoned_baby_strategy()
