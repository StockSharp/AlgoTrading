import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class elder_impulse_strategy(Strategy):
    """
    Strategy based on Elder's Impulse System.
    It combines EMA direction with MACD histogram to identify bullish and bearish impulses.
    Green bar: EMA rising and MACD histogram positive
    Red bar: EMA falling and MACD histogram negative
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    
    def __init__(self):
        super(elder_impulse_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._ema_period = self.Param("EmaPeriod", 13) \
            .SetDisplay("EMA Period", "Period for EMA calculation", "Indicators")
        
        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("MACD Fast Period", "Fast period for MACD", "Indicators")
        
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("MACD Slow Period", "Slow period for MACD", "Indicators")
        
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("MACD Signal Period", "Signal period for MACD", "Indicators")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # Cache for EMA direction
        self._previous_ema = 0.0
        self._is_first_candle = True

    @property
    def ema_period(self):
        """EMA period."""
        return self._ema_period.Value

    @ema_period.setter
    def ema_period(self, value):
        self._ema_period.Value = value

    @property
    def macd_fast_period(self):
        """MACD fast period."""
        return self._macd_fast_period.Value

    @macd_fast_period.setter
    def macd_fast_period(self, value):
        self._macd_fast_period.Value = value

    @property
    def macd_slow_period(self):
        """MACD slow period."""
        return self._macd_slow_period.Value

    @macd_slow_period.setter
    def macd_slow_period(self, value):
        self._macd_slow_period.Value = value

    @property
    def macd_signal_period(self):
        """MACD signal period."""
        return self._macd_signal_period.Value

    @macd_signal_period.setter
    def macd_signal_period(self, value):
        self._macd_signal_period.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage."""
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
        super(elder_impulse_strategy, self).OnReseted()
        self._previous_ema = 0.0
        self._is_first_candle = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(elder_impulse_strategy, self).OnStarted(time)

        # Reset state variables
        self._previous_ema = 0.0
        self._is_first_candle = True

        # Create indicators
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.macd_fast_period
        macd.Macd.LongMa.Length = self.macd_slow_period
        macd.SignalMa.Length = self.macd_signal_period

        # Subscribe to candles
        subscription = self.SubscribeCandles(self.candle_type)
        
        # Process candles with both indicators
        subscription.Bind(ema, macd, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(None, Unit(self.stop_loss_percent, UnitTypes.Percent))

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value, macd_value):
        """
        Processes each finished candle and executes Elder's Impulse System logic.
        
        :param candle: The processed candle message.
        :param ema_value: The current value of the EMA indicator.
        :param macd_value: The current value of the MACD indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema_decimal = float(ema_value)

        if self._is_first_candle:
            self._previous_ema = ema_decimal
            self._is_first_candle = False
            return

        # Determine EMA direction
        is_ema_rising = ema_decimal > self._previous_ema

        # Extract MACD values
        try:
            if hasattr(macd_value, 'Macd') and macd_value.Macd is not None:
                macd_line = float(macd_value.Macd)
            else:
                return
                
            if hasattr(macd_value, 'Signal') and macd_value.Signal is not None:
                signal = float(macd_value.Signal)
            else:
                return
        except:
            # If we can't extract MACD values, skip this candle
            return

        # Get MACD histogram value (MACD - Signal)
        macd_histogram = macd_line - signal

        # Elder Impulse System:
        # 1. Green bar: EMA rising and MACD histogram rising
        # 2. Red bar: EMA falling and MACD histogram falling
        # 3. Blue bar: EMA and MACD histogram in opposite directions

        is_bullish = is_ema_rising and macd_histogram > 0
        is_bearish = not is_ema_rising and macd_histogram < 0

        # Entry logic
        if is_bullish and self.Position <= 0:
            # Buy signal: EMA rising and MACD histogram positive
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: EMA rising, MACD histogram positive. EMA = {0:F2}, MACD Histogram = {1:F4}".format(
                ema_decimal, macd_histogram))
        elif is_bearish and self.Position >= 0:
            # Sell signal: EMA falling and MACD histogram negative
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: EMA falling, MACD histogram negative. EMA = {0:F2}, MACD Histogram = {1:F4}".format(
                ema_decimal, macd_histogram))

        # Exit logic
        if self.Position > 0 and macd_histogram < 0:
            # Exit long position when MACD histogram turns negative
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting long position: MACD histogram turned negative. MACD Histogram = {0:F4}".format(
                macd_histogram))
        elif self.Position < 0 and macd_histogram > 0:
            # Exit short position when MACD histogram turns positive
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exiting short position: MACD histogram turned positive. MACD Histogram = {0:F4}".format(
                macd_histogram))

        # Store current EMA value for next comparison
        self._previous_ema = ema_decimal

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return elder_impulse_strategy()
