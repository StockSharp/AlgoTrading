import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class heikin_ashi_consecutive_strategy(Strategy):
    """
    Strategy based on consecutive Heikin Ashi candles.
    It enters long position after a sequence of bullish Heikin Ashi candles 
    and short position after a sequence of bearish Heikin Ashi candles.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    
    def __init__(self):
        super(heikin_ashi_consecutive_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._consecutive_candles = self.Param("ConsecutiveCandles", 3) \
            .SetDisplay("Consecutive Candles", "Number of consecutive candles required for signal", "Trading parameters")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # State tracking
        self._bullish_count = 0
        self._bearish_count = 0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_ha_high = 0.0
        self._prev_ha_low = 0.0

    @property
    def consecutive_candles(self):
        """Number of consecutive candles required for signal."""
        return self._consecutive_candles.Value

    @consecutive_candles.setter
    def consecutive_candles(self, value):
        self._consecutive_candles.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(heikin_ashi_consecutive_strategy, self).OnReseted()
        self._bullish_count = 0
        self._bearish_count = 0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_ha_high = 0.0
        self._prev_ha_low = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(heikin_ashi_consecutive_strategy, self).OnStarted(time)

        # Initialize state
        self._bullish_count = 0
        self._bearish_count = 0
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_ha_high = 0.0
        self._prev_ha_low = 0.0

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)
        
        # Calculate Heikin-Ashi candles in the ProcessCandle handler
        subscription.Bind(self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        # Start protection with stop loss
        self.StartProtection(None, Unit(self.stop_loss_percent, UnitTypes.Percent))

    def ProcessCandle(self, candle):
        """
        Processes each finished candle and executes Heikin-Ashi consecutive logic.
        
        :param candle: The processed candle message.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate Heikin-Ashi values
        if self._prev_ha_open == 0:
            # First candle - initialize Heikin-Ashi values
            ha_open = (candle.OpenPrice + candle.ClosePrice) / 2
            ha_close = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4
            ha_high = candle.HighPrice
            ha_low = candle.LowPrice
        else:
            # Calculate Heikin-Ashi values based on previous HA candle
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2
            ha_close = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4
            ha_high = max(max(candle.HighPrice, ha_open), ha_close)
            ha_low = min(min(candle.LowPrice, ha_open), ha_close)

        # Determine if Heikin-Ashi candle is bullish or bearish
        is_bullish = ha_close > ha_open
        is_bearish = ha_close < ha_open

        # Update consecutive counts
        if is_bullish:
            self._bullish_count += 1
            self._bearish_count = 0
        elif is_bearish:
            self._bearish_count += 1
            self._bullish_count = 0
        else:
            # Neutral candle (rare case) - reset both counts
            self._bullish_count = 0
            self._bearish_count = 0

        # Trading logic
        if self._bullish_count >= self.consecutive_candles and self.Position <= 0:
            # Enough consecutive bullish candles - Buy signal
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("{0} consecutive bullish Heikin-Ashi candles - Buy signal".format(
                self._bullish_count))
        elif self._bearish_count >= self.consecutive_candles and self.Position >= 0:
            # Enough consecutive bearish candles - Sell signal
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("{0} consecutive bearish Heikin-Ashi candles - Sell signal".format(
                self._bearish_count))
        # Exit logic
        elif self.Position > 0 and is_bearish:
            # Exit long position on first bearish candle
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: Bearish Heikin-Ashi candle appeared")
        elif self.Position < 0 and is_bullish:
            # Exit short position on first bullish candle
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: Bullish Heikin-Ashi candle appeared")

        # Store current Heikin-Ashi values for next candle
        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close
        self._prev_ha_high = ha_high
        self._prev_ha_low = ha_low

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return heikin_ashi_consecutive_strategy()
