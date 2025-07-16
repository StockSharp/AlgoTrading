import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, ICandleMessage, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class harami_bearish_strategy(Strategy):
    """
    Harami Bearish pattern strategy.
    Strategy enters short position when a bearish harami pattern is detected.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(harami_bearish_strategy, self).__init__()

        # Internal state
        self._previous_candle = None
        self._pattern_detected = False

        # Strategy parameters
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use for pattern detection", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage above pattern's high", "Protection") \
            .SetRange(0.1, 5.0)

    @property
    def CandleType(self):
        """Candle type and timeframe for the strategy."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss as percentage above the pattern's high."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(harami_bearish_strategy, self).OnStarted(time)

        self._previous_candle = None
        self._pattern_detected = False

        # Create and setup subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind the candle processor
        subscription.Bind(self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        """Process candle and execute trading logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Skip first candle as we need at least one previous candle to detect the pattern
        if self._previous_candle is None:
            self._previous_candle = candle
            return

        # Check for Harami Bearish pattern:
        # 1. Previous candle is bullish (close > open)
        # 2. Current candle is bearish (close < open)
        # 3. Current candle is completely inside the previous candle (high < prev high and low > prev low)
        is_previous_bullish = self._previous_candle.OpenPrice < self._previous_candle.ClosePrice
        is_current_bearish = candle.OpenPrice > candle.ClosePrice
        is_inside_previous = candle.HighPrice < self._previous_candle.HighPrice and candle.LowPrice > self._previous_candle.LowPrice

        # Detect Harami Bearish pattern
        if is_previous_bullish and is_current_bearish and is_inside_previous and not self._pattern_detected:
            self._pattern_detected = True

            # Calculate position size (if we already have a position, this will close it and open a new one)
            volume = self.Volume + Math.Abs(self.Position)

            # Enter short position at market price
            self.SellMarket(volume)

            # Set stop-loss level
            stop_loss_level = candle.HighPrice * (1 + self.StopLossPercent / 100)

            self.LogInfo("Harami Bearish detected. Selling at {0}. Stop-loss set at {1}".format(
                candle.ClosePrice, stop_loss_level))
        elif self._pattern_detected:
            # Check for exit condition: price breaks below the previous candle's low
            if candle.LowPrice < self._previous_candle.LowPrice:
                # If we have a short position and price breaks below previous low, close the position
                if self.Position < 0:
                    self.BuyMarket(Math.Abs(self.Position))
                    self._pattern_detected = False

                    self.LogInfo("Exit signal: Price broke below previous low ({0}). Closing position at {1}".format(
                        self._previous_candle.LowPrice, candle.ClosePrice))

        # Store current candle as previous for the next iteration
        self._previous_candle = candle

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return harami_bearish_strategy()
