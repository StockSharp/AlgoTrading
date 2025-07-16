import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class bollinger_volatility_breakout_strategy(Strategy):
    """Strategy based on Bollinger Bands breakout with volatility confirmation."""

    def __init__(self):
        super(bollinger_volatility_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicator Settings") \
            .SetCanOptimize(True)

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicator Settings") \
            .SetCanOptimize(True)

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicator Settings") \
            .SetCanOptimize(True)

        self._atr_deviation_multiplier = self.Param("AtrDeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Deviation Multiplier", "Standard deviation multiplier for ATR", "Strategy Settings") \
            .SetCanOptimize(True)

        self._stop_loss_multiplier = self.Param("StopLossMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss Multiplier", "ATR multiplier for stop-loss", "Strategy Settings") \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "General")

        # Internal indicators
        self._atr_sma = None
        self._atr_std_dev = None

    @property
    def BollingerPeriod(self):
        """Period for Bollinger Bands calculation."""
        return self._bollinger_period.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollinger_period.Value = value

    @property
    def BollingerDeviation(self):
        """Standard deviation multiplier for Bollinger Bands."""
        return self._bollinger_deviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def AtrPeriod(self):
        """Period for ATR calculation."""
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrDeviationMultiplier(self):
        """ATR standard deviation multiplier for volatility confirmation."""
        return self._atr_deviation_multiplier.Value

    @AtrDeviationMultiplier.setter
    def AtrDeviationMultiplier(self, value):
        self._atr_deviation_multiplier.Value = value

    @property
    def StopLossMultiplier(self):
        """Stop loss multiplier relative to ATR."""
        return self._stop_loss_multiplier.Value

    @StopLossMultiplier.setter
    def StopLossMultiplier(self, value):
        self._stop_loss_multiplier.Value = value

    @property
    def CandleType(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """Return security and timeframe used by the strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(bollinger_volatility_breakout_strategy, self).OnStarted(time)

        # Create indicators
        bollinger_bands = BollingerBands()
        bollinger_bands.Length = self.BollingerPeriod
        bollinger_bands.Width = self.BollingerDeviation

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        self._atr_sma = SimpleMovingAverage()
        self._atr_sma.Length = self.AtrPeriod
        self._atr_std_dev = StandardDeviation()
        self._atr_std_dev.Length = self.AtrPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind main indicators to subscription
        subscription.BindEx(bollinger_bands, atr, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            Unit(0),  # We'll handle exits in the strategy logic
            Unit(0),   # We'll handle stops in the strategy logic
            True
        )

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger_bands)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bb_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        bb = bb_value  # BollingerBandsValue
        bb_upper = bb.UpBand
        bb_lower = bb.LowBand
        bb_middle = bb.MovingAverage

        atr_dec = to_float(atr_value)

        # Get values from indicators
        atr_sma_value = to_float(self._atr_sma.Process(atr_dec, candle.ServerTime, True))  # Default to current ATR if SMA not available
        atr_std_dev_value = to_float(self._atr_std_dev.Process(atr_dec, candle.ServerTime, True)) * 0.2  # Default to 20% of ATR if StdDev not available

        # Calculate volatility threshold for breakout confirmation
        volatility_threshold = atr_sma_value + self.AtrDeviationMultiplier * atr_std_dev_value

        # Check for increased volatility
        is_high_volatility = atr_dec > volatility_threshold

        # Define entry conditions
        long_entry_condition = candle.ClosePrice > bb_upper and is_high_volatility and self.Position <= 0
        short_entry_condition = candle.ClosePrice < bb_lower and is_high_volatility and self.Position >= 0

        # Define exit conditions
        long_exit_condition = candle.ClosePrice < bb_middle and self.Position > 0
        short_exit_condition = candle.ClosePrice > bb_middle and self.Position < 0

        # Log current values
        self.LogInfo(
            "Close: {0}, BB Upper: {1}, BB Lower: {2}, ATR: {3}, ATR Threshold: {4}, High Volatility: {5}".format(
                candle.ClosePrice, bb_upper, bb_lower, atr_dec, volatility_threshold, is_high_volatility))

        # Execute trading logic
        if long_entry_condition:
            # Calculate position size
            position_size = self.Volume + Math.Abs(self.Position)

            # Calculate stop loss level
            stop_price = candle.ClosePrice - atr_dec * self.StopLossMultiplier

            # Enter long position
            self.BuyMarket(position_size)

            self.LogInfo(
                "Long entry: Price={0}, BB Upper={1}, ATR={2}, Stop={3}".format(
                    candle.ClosePrice, bb_upper, atr_dec, stop_price))

        elif short_entry_condition:
            # Calculate position size
            position_size = self.Volume + Math.Abs(self.Position)

            # Calculate stop loss level
            stop_price = candle.ClosePrice + atr_dec * self.StopLossMultiplier

            # Enter short position
            self.SellMarket(position_size)

            self.LogInfo(
                "Short entry: Price={0}, BB Lower={1}, ATR={2}, Stop={3}".format(
                    candle.ClosePrice, bb_lower, atr_dec, stop_price))

        elif long_exit_condition:
            # Exit long position
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Long exit: Price={0}, BB Middle={1}".format(candle.ClosePrice, bb_middle))

        elif short_exit_condition:
            # Exit short position
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Short exit: Price={0}, BB Middle={1}".format(candle.ClosePrice, bb_middle))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bollinger_volatility_breakout_strategy()

