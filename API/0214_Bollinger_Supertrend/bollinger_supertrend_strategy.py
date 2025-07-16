import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class bollinger_supertrend_strategy(Strategy):
    """
    Strategy based on Bollinger Bands and Supertrend indicators.
    Enters long when price breaks above upper Bollinger Band and is above Supertrend.
    Enters short when price breaks below lower Bollinger Band and is below Supertrend.
    Uses Supertrend for dynamic exit.
    """

    def __init__(self):
        super(bollinger_supertrend_strategy, self).__init__()

        # Bollinger Bands period.
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        # Bollinger Bands standard deviation multiplier.
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        # Supertrend ATR period.
        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Period", "ATR period for Supertrend calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 14, 1)

        # Supertrend ATR multiplier.
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Multiplier", "ATR multiplier for Supertrend calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(2.0, 4.0, 0.5)

        # Candle type parameter.
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._is_long_trend = False
        self._supertrend_value = 0.0
        self._last_close = 0.0

    @property
    def BollingerPeriod(self):
        """Bollinger Bands period."""
        return self._bollinger_period.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollinger_period.Value = value

    @property
    def BollingerDeviation(self):
        """Bollinger Bands standard deviation multiplier."""
        return self._bollinger_deviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def SupertrendPeriod(self):
        """Supertrend ATR period."""
        return self._supertrend_period.Value

    @SupertrendPeriod.setter
    def SupertrendPeriod(self, value):
        self._supertrend_period.Value = value

    @property
    def SupertrendMultiplier(self):
        """Supertrend ATR multiplier."""
        return self._supertrend_multiplier.Value

    @SupertrendMultiplier.setter
    def SupertrendMultiplier(self, value):
        self._supertrend_multiplier.Value = value

    @property
    def CandleType(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(bollinger_supertrend_strategy, self).OnReseted()
        self._is_long_trend = False
        self._supertrend_value = 0.0
        self._last_close = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(bollinger_supertrend_strategy, self).OnStarted(time)

        # Reset state
        self._is_long_trend = False
        self._supertrend_value = 0.0
        self._last_close = 0.0

        # Initialize indicators
        bollinger = BollingerBands()
        bollinger.Length = self.BollingerPeriod
        bollinger.Width = self.BollingerDeviation

        atr = AverageTrueRange()
        atr.Length = self.SupertrendPeriod

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bollinger_value, atr_value):
        """Processes each finished candle and executes trading logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract Bollinger Band values
        bb = bollinger_value
        middle_band = bb.MovingAverage
        upper_band = bb.UpBand
        lower_band = bb.LowBand

        # Calculate Supertrend (simplified)
        atr_val = to_float(atr_value) * self.SupertrendMultiplier
        upper_band2 = (candle.HighPrice + candle.LowPrice) / 2 + atr_val
        lower_band2 = (candle.HighPrice + candle.LowPrice) / 2 - atr_val

        # Determine Supertrend value and direction
        if self._last_close == 0:
            # First candle initialization
            median_price = (candle.HighPrice + candle.LowPrice) / 2
            self._supertrend_value = lower_band2 if candle.ClosePrice > median_price else upper_band2
            self._is_long_trend = candle.ClosePrice > self._supertrend_value
        else:
            if self._is_long_trend:
                # Previous trend was up
                if candle.ClosePrice < self._supertrend_value:
                    # Trend changes to down
                    self._is_long_trend = False
                    self._supertrend_value = upper_band2
                else:
                    # Trend remains up, adjust supertrend value
                    self._supertrend_value = max(lower_band2, self._supertrend_value)
            else:
                # Previous trend was down
                if candle.ClosePrice > self._supertrend_value:
                    # Trend changes to up
                    self._is_long_trend = True
                    self._supertrend_value = lower_band2
                else:
                    # Trend remains down, adjust supertrend value
                    self._supertrend_value = min(upper_band2, self._supertrend_value)

        self._last_close = candle.ClosePrice

        # Trading logic
        is_price_above_supertrend = candle.ClosePrice > self._supertrend_value
        is_price_above_upper_band = candle.ClosePrice > upper_band
        is_price_below_lower_band = candle.ClosePrice < lower_band

        # Long signal: Price breaks above upper Bollinger Band and is above Supertrend
        if is_price_above_upper_band and is_price_above_supertrend:
            if self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Long Entry: Price({0}) > Upper BB({1}) && Price > Supertrend({2})".format(
                    candle.ClosePrice, upper_band, self._supertrend_value))
        # Short signal: Price breaks below lower Bollinger Band and is below Supertrend
        elif is_price_below_lower_band and not is_price_above_supertrend:
            if self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Short Entry: Price({0}) < Lower BB({1}) && Price < Supertrend({2})".format(
                    candle.ClosePrice, lower_band, self._supertrend_value))
        # Exit signals based on Supertrend
        elif (self.Position > 0 and not is_price_above_supertrend) or (self.Position < 0 and is_price_above_supertrend):
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Long: Price({0}) < Supertrend({1})".format(
                    candle.ClosePrice, self._supertrend_value))
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Short: Price({0}) > Supertrend({1})".format(
                    candle.ClosePrice, self._supertrend_value))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bollinger_supertrend_strategy()
