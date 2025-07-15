import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class supertrend_reversal_strategy(Strategy):
    """
    Supertrend Reversal Strategy.
    Enters long when Supertrend switches from above to below price and
    enters short when it flips from below to above price.
    """

    def __init__(self):
        """Initialize a new instance of :class:`supertrend_reversal_strategy`."""
        super(supertrend_reversal_strategy, self).__init__()

        # Initialize strategy parameters
        self._period = self.Param("Period", 10) \
            .SetDisplay("Period", "Period for Supertrend calculation", "Supertrend Settings") \
            .SetRange(7, 20) \
            .SetCanOptimize(True)

        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetDisplay("Multiplier", "Multiplier for Supertrend calculation", "Supertrend Settings") \
            .SetRange(2.0, 4.0) \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Previous state variables
        self._prev_is_supertrend_above_price = None
        self._atr = None
        self._prev_highest = 0
        self._prev_lowest = 0
        self._prev_supertrend = 0
        self._prev_close = 0
        self._is_first_update = True

    @property
    def period(self):
        """Period for Supertrend calculation."""
        return self._period.Value

    @period.setter
    def period(self, value):
        self._period.Value = value

    @property
    def multiplier(self):
        """Multiplier for Supertrend calculation."""
        return self._multiplier.Value

    @multiplier.setter
    def multiplier(self, value):
        self._multiplier.Value = value

    @property
    def candle_type(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """Return security and timeframe used by the strategy."""
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(supertrend_reversal_strategy, self).OnStarted(time)

        # Initialize previous state
        self._prev_is_supertrend_above_price = None
        self._is_first_update = True
        self._prev_highest = 0
        self._prev_lowest = 0
        self._prev_supertrend = 0
        self._prev_close = 0

        # Create ATR indicator for Supertrend calculation
        self._atr = AverageTrueRange()
        self._atr.Length = self.period

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind ATR indicator and process candles
        subscription.Bind(self._atr, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, atr_value):
        """Process candle along with its ATR value."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate Supertrend value
        median_price = (candle.HighPrice + candle.LowPrice) / 2
        upper_band = median_price + (self.multiplier * atr_value)
        lower_band = median_price - (self.multiplier * atr_value)

        if self._is_first_update:
            self._prev_highest = upper_band
            self._prev_lowest = lower_band
            self._prev_supertrend = upper_band if candle.ClosePrice <= upper_band else lower_band
            self._prev_close = candle.ClosePrice
            self._is_first_update = False
            return

        # Calculate current upper and lower limits
        current_upper_band = upper_band
        current_lower_band = lower_band

        # Adjust upper band
        if current_upper_band < self._prev_highest or self._prev_close > self._prev_highest:
            current_upper_band = upper_band
        else:
            current_upper_band = self._prev_highest

        # Adjust lower band
        if current_lower_band > self._prev_lowest or self._prev_close < self._prev_lowest:
            current_lower_band = lower_band
        else:
            current_lower_band = self._prev_lowest

        # Calculate Supertrend
        if self._prev_supertrend == self._prev_highest:
            if candle.ClosePrice <= current_upper_band:
                supertrend = current_upper_band
            else:
                supertrend = current_lower_band
        else:
            if candle.ClosePrice >= current_lower_band:
                supertrend = current_lower_band
            else:
                supertrend = current_upper_band

        # Determine if Supertrend is above or below price
        is_supertrend_above_price = supertrend > candle.ClosePrice

        # If this is the first valid calculation, just store the state
        if self._prev_is_supertrend_above_price is None:
            self._prev_is_supertrend_above_price = is_supertrend_above_price
            # Update previous values for next calculation
            self._prev_highest = current_upper_band
            self._prev_lowest = current_lower_band
            self._prev_supertrend = supertrend
            self._prev_close = candle.ClosePrice
            return

        # Check for Supertrend reversal
        supertrend_switched_below = self._prev_is_supertrend_above_price and not is_supertrend_above_price
        supertrend_switched_above = (not self._prev_is_supertrend_above_price) and is_supertrend_above_price

        # Long entry: Supertrend switched from above to below price
        if supertrend_switched_below and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Long entry: Supertrend switched below price")
        # Short entry: Supertrend switched from below to above price
        elif supertrend_switched_above and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Short entry: Supertrend switched above price")

        # Update the previous state and values
        self._prev_is_supertrend_above_price = is_supertrend_above_price
        self._prev_highest = current_upper_band
        self._prev_lowest = current_lower_band
        self._prev_supertrend = supertrend
        self._prev_close = candle.ClosePrice

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return supertrend_reversal_strategy()
