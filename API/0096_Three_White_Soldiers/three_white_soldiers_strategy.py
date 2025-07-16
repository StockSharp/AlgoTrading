import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class three_white_soldiers_strategy(Strategy):
    """
    Strategy based on "Three White Soldiers" candlestick pattern.
    This strategy looks for three consecutive bullish candles with
    closing prices higher than previous candle, indicating a strong uptrend.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(three_white_soldiers_strategy, self).__init__()

        # Initialize internal state
        self._firstCandle = None
        self._secondCandle = None
        self._currentCandle = None

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetRange(0.1, 5) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage below low of pattern", "Risk Management")

        self._maLength = self.Param("MaLength", 20) \
            .SetRange(10, 50) \
            .SetDisplay("MA Length", "Period of moving average for exit signal", "Indicators")

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
    def MaLength(self):
        """Moving average length for exit signal."""
        return self._maLength.Value

    @MaLength.setter
    def MaLength(self, value):
        self._maLength.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(three_white_soldiers_strategy, self).OnReseted()
        self._firstCandle = None
        self._secondCandle = None
        self._currentCandle = None

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(three_white_soldiers_strategy, self).OnStarted(time)

        # Reset candle storage
        self._firstCandle = None
        self._secondCandle = None
        self._currentCandle = None

        # Create a simple moving average indicator for exit signal
        ma = SimpleMovingAverage()
        ma.Length = self.MaLength

        # Create subscription and bind to process candles
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, self.ProcessCandle).Start()

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
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, maValue):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Shift candles
        self._firstCandle = self._secondCandle
        self._secondCandle = self._currentCandle
        self._currentCandle = candle

        # Check if we have enough candles to analyze
        if self._firstCandle is None or self._secondCandle is None or self._currentCandle is None:
            return

        # Check for "Three White Soldiers" pattern
        isWhiteSoldiers = (
            # First candle is bullish
            self._firstCandle.OpenPrice < self._firstCandle.ClosePrice and
            # Second candle is bullish
            self._secondCandle.OpenPrice < self._secondCandle.ClosePrice and
            # Third candle is bullish
            self._currentCandle.OpenPrice < self._currentCandle.ClosePrice and
            # Each close is higher than previous
            self._currentCandle.ClosePrice > self._secondCandle.ClosePrice and
            self._secondCandle.ClosePrice > self._firstCandle.ClosePrice
        )

        # Check for long entry condition
        if isWhiteSoldiers and self.Position == 0:
            self.LogInfo("Three White Soldiers pattern detected. Going long.")
            self.BuyMarket(self.Volume)
        # Check for exit condition
        elif self.Position > 0 and candle.ClosePrice < maValue:
            self.LogInfo("Price fell below MA. Exiting long position.")
            self.SellMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return three_white_soldiers_strategy()
