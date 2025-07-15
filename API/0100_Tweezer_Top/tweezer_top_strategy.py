import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Strategies import Strategy


class tweezer_top_strategy(Strategy):
    """
    Strategy based on "Tweezer Top" candlestick pattern.
    This pattern forms when two candlesticks have nearly identical highs, with the first
    being bullish and the second being bearish, indicating a potential reversal.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(tweezer_top_strategy, self).__init__()

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetRange(0.1, 5.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage above high", "Risk Management")

        self._highTolerancePercent = self.Param("HighTolerancePercent", 0.1) \
            .SetRange(0.05, 1.0) \
            .SetDisplay("High Tolerance %", "Maximum percentage difference between highs", "Pattern Parameters")

        # Internal state
        self._previousCandle = None
        self._currentCandle = None
        self._entryPrice = 0.0

    @property
    def CandleType(self):
        """Candle type and timeframe for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percent from entry price."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def HighTolerancePercent(self):
        """Tolerance percentage for comparing high prices."""
        return self._highTolerancePercent.Value

    @HighTolerancePercent.setter
    def HighTolerancePercent(self, value):
        self._highTolerancePercent.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED!! Returns securities this strategy works with."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(tweezer_top_strategy, self).OnStarted(time)

        # Reset candle storage
        self._previousCandle = None
        self._currentCandle = None
        self._entryPrice = 0.0

        # Create subscription and bind to process candles
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        # Setup protection with stop loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            isStopTrailing=False
        )

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Shift candles
        self._previousCandle = self._currentCandle
        self._currentCandle = candle

        if self._previousCandle is None:
            return

        # Check for Tweezer Top pattern
        isTweezerTop = self.IsTweezerTop(self._previousCandle, self._currentCandle)

        # Check for entry condition
        if isTweezerTop and self.Position == 0:
            self.LogInfo("Tweezer Top pattern detected. Going short.")
            self.SellMarket(self.Volume)
            self._entryPrice = candle.ClosePrice
        # Check for exit condition
        elif self.Position < 0 and candle.LowPrice < self._entryPrice:
            self.LogInfo("Price below entry low. Taking profit.")
            self.BuyMarket(abs(self.Position))

    def IsTweezerTop(self, candle1, candle2):
        # First candle must be bullish (close > open)
        if candle1.ClosePrice <= candle1.OpenPrice:
            return False

        # Second candle must be bearish (close < open)
        if candle2.ClosePrice >= candle2.OpenPrice:
            return False

        # Calculate the tolerance range for high comparisons
        highTolerance = candle1.HighPrice * (self.HighTolerancePercent / 100.0)

        # High prices must be approximately equal
        highsAreEqual = Math.Abs(candle1.HighPrice - candle2.HighPrice) <= highTolerance
        if not highsAreEqual:
            return False

        return True

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return tweezer_top_strategy()
