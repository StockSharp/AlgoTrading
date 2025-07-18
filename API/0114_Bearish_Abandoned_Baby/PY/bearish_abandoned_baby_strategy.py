import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class bearish_abandoned_baby_strategy(Strategy):
    """Strategy based on Bearish Abandoned Baby candlestick pattern."""

    def __init__(self):
        super(bearish_abandoned_baby_strategy, self).__init__()

        # Initialize strategy parameters
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use for analysis", "Candles")

        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetRange(0.1, 5.0) \
            .SetDisplay("Stop Loss %", "Stop Loss percentage above the high of the doji candle", "Risk") \
            .SetCanOptimize(True)

        # Internal state
        self._prev_candle1 = None
        self._prev_candle2 = None

    @property
    def candle_type(self):
        """Candle type and timeframe."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percent from entry price."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(bearish_abandoned_baby_strategy, self).OnReseted()
        self._prev_candle1 = None
        self._prev_candle2 = None

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up subscriptions and charting.

        :param time: The time when the strategy started.
        """
        super(bearish_abandoned_baby_strategy, self).OnStarted(time)

        # Reset pattern candles
        self._prev_candle1 = None
        self._prev_candle2 = None

        # Create and subscribe to candles
        subscription = self.SubscribeCandles(self.candle_type)

        subscription.Bind(self.ProcessCandle).Start()

        # Configure protection for open positions
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent),
            isStopTrailing=False
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

        # If we have enough candles, check for the Bearish Abandoned Baby pattern
        if self._prev_candle2 is not None and self._prev_candle1 is not None:
            # Check for bearish abandoned baby pattern:
            # 1. First candle is bullish (close > open)
            # 2. Middle candle is a doji and gaps up (low > high of first candle)
            # 3. Current candle is bearish (close < open) and gaps down (high < low of middle candle)
            first_candle_bullish = self._prev_candle2.ClosePrice > self._prev_candle2.OpenPrice
            middle_candle_gaps_up = self._prev_candle1.LowPrice > self._prev_candle2.HighPrice
            current_candle_bearish = candle.ClosePrice < candle.OpenPrice
            current_candle_gaps_down = candle.HighPrice < self._prev_candle1.LowPrice

            if first_candle_bullish and middle_candle_gaps_up and current_candle_bearish and current_candle_gaps_down:
                self.LogInfo("Bearish Abandoned Baby pattern detected!")

                # Enter short position if we don't have one already
                if self.Position >= 0:
                    self.SellMarket(self.Volume)
                    self.LogInfo("Short position opened: {0} at market".format(self.Volume))

        # Store current candle for next pattern check
        self._prev_candle2 = self._prev_candle1
        self._prev_candle1 = candle

        # Exit logic - if we're in a short position and price breaks below low of the current candle
        if self.Position < 0 and self._prev_candle2 is not None and candle.LowPrice < self._prev_candle2.LowPrice:
            self.LogInfo("Exit signal: Price broke below previous candle low")
            self.ClosePosition()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bearish_abandoned_baby_strategy()
