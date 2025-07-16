import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class keltner_rsi_divergence_strategy(Strategy):
    """
    Strategy based on Keltner Channel and RSI Divergence.
    """

    def __init__(self):
        super(keltner_rsi_divergence_strategy, self).__init__()

        # EMA period parameter.
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for EMA calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # ATR period parameter.
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 28, 7)

        # ATR multiplier parameter.
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to set channel width", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # RSI period parameter.
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 28, 7)

        # Candle type parameter.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Store previous values to detect divergence
        self._prev_rsi = 50
        self._prev_price = 0.0

        self._ema = None
        self._atr = None
        self._rsi = None

    @property
    def EmaPeriod(self):
        # EMA period parameter.
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def AtrPeriod(self):
        # ATR period parameter.
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrMultiplier(self):
        # ATR multiplier parameter.
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def RsiPeriod(self):
        # RSI period parameter.
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def CandleType(self):
        # Candle type parameter.
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(keltner_rsi_divergence_strategy, self).OnStarted(time)

        # Initialize previous values
        self._prev_rsi = 50
        self._prev_price = 0

        # Create indicators
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ema, self._atr, self._rsi, self.ProcessCandle).Start()

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(1, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, ema_value, atr_value, rsi_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Skip if it's the first valid candle
        if self._prev_price == 0:
            self._prev_price = candle.ClosePrice
            self._prev_rsi = rsi_value
            return

        # Calculate Keltner Channel
        upper_band = ema_value + (self.AtrMultiplier * atr_value)
        lower_band = ema_value - (self.AtrMultiplier * atr_value)

        # Check for RSI divergence
        is_bullish_divergence = rsi_value > self._prev_rsi and candle.ClosePrice < self._prev_price
        is_bearish_divergence = rsi_value < self._prev_rsi and candle.ClosePrice > self._prev_price

        # Trading logic
        if candle.ClosePrice < lower_band and is_bullish_divergence and self.Position <= 0:
            # Bullish divergence at lower band
            self.CancelActiveOrders()

            # Calculate position size
            volume = self.Volume + Math.Abs(self.Position)

            # Enter long position
            self.BuyMarket(volume)
        elif candle.ClosePrice > upper_band and is_bearish_divergence and self.Position >= 0:
            # Bearish divergence at upper band
            self.CancelActiveOrders()

            # Calculate position size
            volume = self.Volume + Math.Abs(self.Position)

            # Enter short position
            self.SellMarket(volume)

        # Exit logic - when price reverts to EMA
        if (self.Position > 0 and candle.ClosePrice > ema_value) or \
                (self.Position < 0 and candle.ClosePrice < ema_value):
            # Exit position
            self.ClosePosition()

        # Update previous values
        self._prev_rsi = rsi_value
        self._prev_price = candle.ClosePrice

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return keltner_rsi_divergence_strategy()
