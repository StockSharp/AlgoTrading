import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class gann_swing_breakout_strategy(Strategy):
    """
    Strategy based on Gann Swing Breakout technique.
    It detects swing highs and lows, then enters positions when price breaks out
    after a pullback to a moving average.
    
    """
    
    def __init__(self):
        super(gann_swing_breakout_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._swing_lookback = self.Param("SwingLookback", 5) \
            .SetDisplay("Swing Lookback", "Number of bars to identify swing points", "Trading parameters")
        
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average calculation", "Indicators")
        
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # State tracking
        self._last_swing_high = None
        self._last_swing_low = None
        self._high_bar_index = 0
        self._low_bar_index = 0
        self._current_bar_index = 0
        self._recent_highs = []
        self._recent_lows = []
        self._recent_candles = []
        self._prev_ma_value = 0.0

    @property
    def swing_lookback(self):
        """Number of bars to identify swing points."""
        return self._swing_lookback.Value

    @swing_lookback.setter
    def swing_lookback(self, value):
        self._swing_lookback.Value = value

    @property
    def ma_period(self):
        """Period for moving average calculation."""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

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
        super(gann_swing_breakout_strategy, self).OnReseted()
        self._last_swing_high = None
        self._last_swing_low = None
        self._high_bar_index = 0
        self._low_bar_index = 0
        self._current_bar_index = 0
        self._recent_highs = []
        self._recent_lows = []
        self._recent_candles = []
        self._prev_ma_value = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(gann_swing_breakout_strategy, self).OnStarted(time)

        # Initialize state
        self._last_swing_high = None
        self._last_swing_low = None
        self._high_bar_index = 0
        self._low_bar_index = 0
        self._current_bar_index = 0
        self._recent_highs = []
        self._recent_lows = []
        self._recent_candles = []
        self._prev_ma_value = 0.0

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.ma_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value):
        """
        Processes each finished candle and executes Gann swing breakout logic.
        
        :param candle: The processed candle message.
        :param ma_value: The current value of the moving average.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Update bar index
        self._current_bar_index += 1
        
        # Store recent candles and prices for swing detection
        self._recent_candles.append(candle)
        self._recent_highs.append(candle.HighPrice)
        self._recent_lows.append(candle.LowPrice)
        
        # Keep only necessary history for swing detection
        max_history = max(self.swing_lookback * 2 + 1, self.ma_period)
        if len(self._recent_candles) > max_history:
            self._recent_candles.pop(0)
            self._recent_highs.pop(0)
            self._recent_lows.pop(0)
        
        # Skip processing until we have enough data
        if len(self._recent_candles) < self.swing_lookback * 2 + 1:
            self._prev_ma_value = ma_value
            return

        # Detect swing high and low points
        self._detect_swing_points()
        
        # Check for price crossing MA
        ma_decimal = float(ma_value)
        is_price_above_ma = candle.ClosePrice > ma_decimal
        is_price_below_ma = candle.ClosePrice < ma_decimal
        
        if len(self._recent_candles) >= 2:
            was_price_above_ma = self._recent_candles[-2].ClosePrice > self._prev_ma_value
        else:
            was_price_above_ma = False
        
        # Detect MA pullback and breakout conditions
        is_pullback_from_low = not is_price_below_ma and was_price_above_ma
        is_pullback_from_high = not is_price_above_ma and not was_price_above_ma
        
        # Trading logic
        if self._last_swing_high is not None and self._last_swing_low is not None:
            # Long setup: Price breaks above last swing high after pullback to MA
            if (candle.ClosePrice > self._last_swing_high and 
                is_pullback_from_low and self.Position <= 0):
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)
                self.LogInfo("Buy signal: Breakout above swing high {0} after MA pullback".format(
                    self._last_swing_high))
            # Short setup: Price breaks below last swing low after pullback to MA
            elif (candle.ClosePrice < self._last_swing_low and 
                  is_pullback_from_high and self.Position >= 0):
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)
                self.LogInfo("Sell signal: Breakout below swing low {0} after MA pullback".format(
                    self._last_swing_low))
            # Exit logic for long positions
            elif self.Position > 0 and candle.ClosePrice < ma_decimal:
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: Price {0} dropped below MA {1}".format(
                    candle.ClosePrice, ma_decimal))
            # Exit logic for short positions
            elif self.Position < 0 and candle.ClosePrice > ma_decimal:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Price {0} rose above MA {1}".format(
                    candle.ClosePrice, ma_decimal))
        
        # Update previous MA value
        self._prev_ma_value = ma_decimal

    def _detect_swing_points(self):
        """
        Detects swing high and low points in the recent price data.
        """
        # Check for swing high
        mid_point = len(self._recent_highs) - self.swing_lookback - 1
        is_swing_high = True
        center_high = self._recent_highs[mid_point]
        
        # Check bars before the center point
        for i in range(mid_point - self.swing_lookback, mid_point):
            if i < 0 or self._recent_highs[i] > center_high:
                is_swing_high = False
                break
        
        # Check bars after the center point
        for i in range(mid_point + 1, mid_point + self.swing_lookback + 1):
            if i >= len(self._recent_highs) or self._recent_highs[i] > center_high:
                is_swing_high = False
                break
        
        # Check for swing low
        is_swing_low = True
        center_low = self._recent_lows[mid_point]
        
        # Check bars before the center point
        for i in range(mid_point - self.swing_lookback, mid_point):
            if i < 0 or self._recent_lows[i] < center_low:
                is_swing_low = False
                break
        
        # Check bars after the center point
        for i in range(mid_point + 1, mid_point + self.swing_lookback + 1):
            if i >= len(self._recent_lows) or self._recent_lows[i] < center_low:
                is_swing_low = False
                break
        
        # Update swing points if detected
        if is_swing_high:
            self._last_swing_high = center_high
            self._high_bar_index = self._current_bar_index - self.swing_lookback - 1
            self.LogInfo("New swing high detected: {0}".format(self._last_swing_high))
        
        if is_swing_low:
            self._last_swing_low = center_low
            self._low_bar_index = self._current_bar_index - self.swing_lookback - 1
            self.LogInfo("New swing low detected: {0}".format(self._last_swing_low))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return gann_swing_breakout_strategy()
