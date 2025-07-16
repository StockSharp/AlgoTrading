import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import KaufmanAdaptiveMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class adaptive_ema_breakout_strategy(Strategy):
    """
    Strategy based on Adaptive EMA breakout with trend confirmation.
    """

    def __init__(self):
        super(adaptive_ema_breakout_strategy, self).__init__()

        # Fast EMA Period parameter for KAMA calculation.
        self._fast = self.Param("Fast", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast period", "Fast (EMA) period for calculating KAMA", "KAMA Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(2, 10, 1)

        # Slow EMA Period parameter for KAMA calculation.
        self._slow = self.Param("Slow", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow period", "Slow (EMA) period for calculating KAMA", "KAMA Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 40, 5)

        # Lookback period for KAMA calculation.
        self._lookback = self.Param("Lookback", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback", "Main period for calculating KAMA", "KAMA Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 5)

        # Stop-loss multiplier relative to ATR.
        self._stopMultiplier = self.Param("StopMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop ATR multiplier", "ATR multiplier for stop-loss", "Strategy Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Candle type parameter.
        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Type of candles for strategy", "General")

        # Internal state variables
        self._prevAdaptiveEmaValue = 0.0
        self._isFirstCandle = True

    @property
    def Fast(self):
        # Fast EMA Period parameter for KAMA calculation.
        return self._fast.Value

    @Fast.setter
    def Fast(self, value):
        self._fast.Value = value

    @property
    def Slow(self):
        # Slow EMA Period parameter for KAMA calculation.
        return self._slow.Value

    @Slow.setter
    def Slow(self, value):
        self._slow.Value = value

    @property
    def Lookback(self):
        # Lookback period for KAMA calculation.
        return self._lookback.Value

    @Lookback.setter
    def Lookback(self, value):
        self._lookback.Value = value

    @property
    def StopMultiplier(self):
        # Stop-loss multiplier relative to ATR.
        return self._stopMultiplier.Value

    @StopMultiplier.setter
    def StopMultiplier(self, value):
        self._stopMultiplier.Value = value

    @property
    def CandleType(self):
        # Candle type parameter.
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        super(adaptive_ema_breakout_strategy, self).OnStarted(time)

        # Create indicators
        adaptive_ema = KaufmanAdaptiveMovingAverage()
        adaptive_ema.Length = self.Lookback
        adaptive_ema.FastSCPeriod = self.Fast
        adaptive_ema.SlowSCPeriod = self.Slow
        atr = AverageTrueRange()
        atr.Length = 14

        # Reset state variables
        self._isFirstCandle = True
        self._prevAdaptiveEmaValue = 0.0

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to subscription
        subscription.Bind(adaptive_ema, atr, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            Unit(0),  # We'll handle exits in the strategy logic
            Unit(0),  # We'll handle stops in the strategy logic
            True       # Use market orders
        )

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adaptive_ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, adaptive_ema_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Initialize values on first candle
        if self._isFirstCandle:
            self._prevAdaptiveEmaValue = adaptive_ema_value
            self._isFirstCandle = False
            return

        # Calculate trend direction
        adaptive_ema_trend_up = adaptive_ema_value > self._prevAdaptiveEmaValue

        # Define entry conditions
        long_entry_condition = candle.ClosePrice > adaptive_ema_value and adaptive_ema_trend_up and self.Position <= 0
        short_entry_condition = candle.ClosePrice < adaptive_ema_value and not adaptive_ema_trend_up and self.Position >= 0

        # Define exit conditions
        long_exit_condition = candle.ClosePrice < adaptive_ema_value and self.Position > 0
        short_exit_condition = candle.ClosePrice > adaptive_ema_value and self.Position < 0

        # Execute trading logic
        if long_entry_condition:
            # Calculate position size
            position_size = self.Volume + Math.Abs(self.Position)

            # Calculate stop loss level
            stop_price = candle.ClosePrice - atr_value * self.StopMultiplier

            # Enter long position
            self.BuyMarket(position_size)

            self.LogInfo("Long entry: Price={0}, KAMA={1}, ATR={2}, Stop={3}".format(
                candle.ClosePrice, adaptive_ema_value, atr_value, stop_price))
        elif short_entry_condition:
            # Calculate position size
            position_size = self.Volume + Math.Abs(self.Position)

            # Calculate stop loss level
            stop_price = candle.ClosePrice + atr_value * self.StopMultiplier

            # Enter short position
            self.SellMarket(position_size)

            self.LogInfo("Short entry: Price={0}, KAMA={1}, ATR={2}, Stop={3}".format(
                candle.ClosePrice, adaptive_ema_value, atr_value, stop_price))
        elif long_exit_condition:
            # Exit long position
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Long exit: Price={0}, KAMA={1}".format(
                candle.ClosePrice, adaptive_ema_value))
        elif short_exit_condition:
            # Exit short position
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Short exit: Price={0}, KAMA={1}".format(
                candle.ClosePrice, adaptive_ema_value))

        # Store current value for next candle
        self._prevAdaptiveEmaValue = adaptive_ema_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adaptive_ema_breakout_strategy()
