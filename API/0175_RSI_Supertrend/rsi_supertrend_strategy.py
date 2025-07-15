import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class rsi_supertrend_strategy(Strategy):
    """
    Strategy based on RSI and Supertrend indicators.
    Enters long when RSI is oversold (< 30) and price is above Supertrend
    Enters short when RSI is overbought (> 70) and price is below Supertrend

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(rsi_supertrend_strategy, self).__init__()

        # Initialize strategy parameters
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for RSI indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Period", "ATR period for Supertrend", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 14, 1)

        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Multiplier", "ATR multiplier for Supertrend", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(2.0, 4.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")

        # Custom Supertrend indicator
        self._atr = None
        self._up_value = 0.0
        self._down_value = 0.0
        self._current_trend = 0.0
        self._prev_up_value = 0.0
        self._prev_down_value = 0.0
        self._prev_close = 0.0
        self._is_first_value = True

    @property
    def rsi_period(self):
        """RSI period"""
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def supertrend_period(self):
        """Supertrend ATR period"""
        return self._supertrend_period.Value

    @supertrend_period.setter
    def supertrend_period(self, value):
        self._supertrend_period.Value = value

    @property
    def supertrend_multiplier(self):
        """Supertrend ATR multiplier"""
        return self._supertrend_multiplier.Value

    @supertrend_multiplier.setter
    def supertrend_multiplier(self, value):
        self._supertrend_multiplier.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy calculation"""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(rsi_supertrend_strategy, self).OnReseted()
        self._is_first_value = True
        self._current_trend = 1
        self._down_value = 0
        self._prev_up_value = 0
        self._prev_down_value = 0
        self._prev_close = 0
        self._up_value = 0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(rsi_supertrend_strategy, self).OnStarted(time)

        # Reset state variables
        self._is_first_value = True
        self._current_trend = 1  # Default to uptrend
        self._down_value = 0
        self._prev_up_value = 0
        self._prev_down_value = 0
        self._prev_close = 0
        self._up_value = 0

        # Create RSI indicator
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        # Create ATR indicator for Supertrend calculation
        self._atr = AverageTrueRange()
        self._atr.Length = self.supertrend_period

        # Enable using Supertrend as a dynamic stop-loss
        # We'll implement our own stop management based on Supertrend

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)

            # Separate area for RSI
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)

            # Note: We'll manually draw Supertrend lines in ProcessCandle method

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value, atr_value):
        """
        Process candle and execute trading logic based on RSI and Supertrend.

        :param candle: The processed candle message.
        :param rsi_value: The current RSI value.
        :param atr_value: The current ATR value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate Supertrend
        close_price = candle.ClosePrice
        high_price = candle.HighPrice
        low_price = candle.LowPrice

        # Basic bands calculation
        basic_upper_band = (high_price + low_price) / 2 + self.supertrend_multiplier * atr_value
        basic_lower_band = (high_price + low_price) / 2 - self.supertrend_multiplier * atr_value

        if self._is_first_value:
            # Initialize values for the first candle
            self._up_value = basic_upper_band
            self._down_value = basic_lower_band
            self._prev_up_value = self._up_value
            self._prev_down_value = self._down_value
            self._prev_close = close_price
            self._is_first_value = False
            return

        # Calculate final upper and lower bands
        self._up_value = basic_upper_band
        if self._up_value < self._prev_up_value or self._prev_close > self._prev_up_value:
            self._up_value = self._prev_up_value

        self._down_value = basic_lower_band
        if self._down_value > self._prev_down_value or self._prev_close < self._prev_down_value:
            self._down_value = self._prev_down_value

        # Determine trend direction
        prev_trend = self._current_trend

        if self._prev_close <= self._prev_up_value:
            self._current_trend = -1  # Downtrend

        if self._prev_close >= self._prev_down_value:
            self._current_trend = 1  # Uptrend

        # Store values for next iteration
        self._prev_up_value = self._up_value
        self._prev_down_value = self._down_value
        self._prev_close = close_price

        # Get Supertrend value based on current trend
        supertrend_value = self._down_value if self._current_trend == 1 else self._up_value

        # Trading logic
        is_trend_change = prev_trend != self._current_trend

        # Long condition: RSI oversold and price above Supertrend
        if rsi_value < 30 and self._current_trend == 1 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))

            # Note: We're using Supertrend as our stop-loss level,
            # so we don't need to set a separate stop-loss order
        # Short condition: RSI overbought and price below Supertrend
        elif rsi_value > 70 and self._current_trend == -1 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        # Exit conditions - based on Supertrend direction change
        elif is_trend_change:
            if self._current_trend == -1 and self.Position > 0:
                # Trend changed to down - exit long
                self.SellMarket(self.Position)
            elif self._current_trend == 1 and self.Position < 0:
                # Trend changed to up - exit short
                self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return rsi_supertrend_strategy()
