import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Messages import Unit
from StockSharp.Messages import UnitTypes
from StockSharp.Algo.Strategies import Strategy

class rejection_candle_strategy(Strategy):
    """
    Strategy based on rejection candles that indicate potential reversals.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(rejection_candle_strategy, self).__init__()

        # Initialize strategy parameters
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use for pattern detection", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection") \
            .SetRange(0.1, 5.0)

        # Internal state
        self._previous_candle = None
        self._in_position = False
        self._current_position_side = None

    @property
    def candle_type(self):
        """Candle type and timeframe for the strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage from entry price."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(rejection_candle_strategy, self).OnReseted()
        self._previous_candle = None
        self._in_position = False
        self._current_position_side = None

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up subscriptions and charting.

        :param time: The time when the strategy started.
        """
        super(rejection_candle_strategy, self).OnStarted(time)

        self._previous_candle = None
        self._in_position = False
        self._current_position_side = None

        # Create and setup subscription for candles
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind the candle processor
        subscription.Bind(self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(Unit(0), Unit(self.stop_loss_percent, UnitTypes.Percent))

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        """
        Processes finished candles and executes trading logic.

        :param candle: The candle message.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Skip first candle as we need at least one previous candle
        if self._previous_candle is None:
            self._previous_candle = candle
            return

        # Determine candle characteristics
        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice
        has_upper_wick = candle.HighPrice > max(candle.OpenPrice, candle.ClosePrice)
        has_lower_wick = candle.LowPrice < min(candle.OpenPrice, candle.ClosePrice)
        made_lower_low = candle.LowPrice < self._previous_candle.LowPrice
        made_higher_high = candle.HighPrice > self._previous_candle.HighPrice

        # 1. Bearish Rejection (Pin Bar): Made a higher high but closed lower with a long upper wick
        if made_higher_high and is_bearish and has_upper_wick:
            # Calculate upper wick size as a percentage of candle body
            body_size = Math.Abs(candle.ClosePrice - candle.OpenPrice)
            upper_wick_size = candle.HighPrice - max(candle.OpenPrice, candle.ClosePrice)

            # Upper wick should be significant compared to body
            if upper_wick_size > body_size * 1.5:
                # If we already have a long position, close it
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                    self._in_position = False
                    self._current_position_side = None
                    self.LogInfo("Closed long position at {0} on bearish rejection".format(candle.ClosePrice))
                # Enter short if we're not already short
                elif self.Position <= 0:
                    volume = self.Volume + Math.Abs(self.Position)
                    self.SellMarket(volume)
                    self._in_position = True
                    self._current_position_side = Sides.Sell
                    self.LogInfo("Bearish rejection detected. Short entry at {0}".format(candle.ClosePrice))

        # 2. Bullish Rejection (Pin Bar): Made a lower low but closed higher with a long lower wick
        elif made_lower_low and is_bullish and has_lower_wick:
            # Calculate lower wick size as a percentage of candle body
            body_size = Math.Abs(candle.ClosePrice - candle.OpenPrice)
            lower_wick_size = min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice

            # Lower wick should be significant compared to body
            if lower_wick_size > body_size * 1.5:
                # If we already have a short position, close it
                if self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                    self._in_position = False
                    self._current_position_side = None
                    self.LogInfo("Closed short position at {0} on bullish rejection".format(candle.ClosePrice))
                # Enter long if we're not already long
                elif self.Position >= 0:
                    volume = self.Volume + Math.Abs(self.Position)
                    self.BuyMarket(volume)
                    self._in_position = True
                    self._current_position_side = Sides.Buy
                    self.LogInfo("Bullish rejection detected. Long entry at {0}".format(candle.ClosePrice))

        # Check for exit conditions if in position
        if self._in_position:
            if self._current_position_side == Sides.Buy and candle.HighPrice > self._previous_candle.HighPrice:
                # For long positions: exit when price breaks above the high of previous candle
                self.SellMarket(Math.Abs(self.Position))
                self._in_position = False
                self._current_position_side = None
                self.LogInfo("Exit signal: Price broke above previous high ({0}). Closed long at {1}".format(
                    self._previous_candle.HighPrice, candle.ClosePrice))
            elif self._current_position_side == Sides.Sell and candle.LowPrice < self._previous_candle.LowPrice:
                # For short positions: exit when price breaks below the low of previous candle
                self.BuyMarket(Math.Abs(self.Position))
                self._in_position = False
                self._current_position_side = None
                self.LogInfo("Exit signal: Price broke below previous low ({0}). Closed short at {1}".format(
                    self._previous_candle.LowPrice, candle.ClosePrice))

        # Store current candle as previous for the next iteration
        self._previous_candle = candle

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return rejection_candle_strategy()
