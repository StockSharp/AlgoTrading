import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class macd_trend_strategy(Strategy):
    """
    Strategy based on MACD indicator.
    It enters long position when MACD crosses above signal line 
    and short position when MACD crosses below signal line.
    
    """
    
    def __init__(self):
        super(macd_trend_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._fast_ema_period = self.Param("FastEmaPeriod", 12) \
            .SetDisplay("Fast EMA Period", "Period for fast EMA in MACD", "Indicators")
        
        self._slow_ema_period = self.Param("SlowEmaPeriod", 26) \
            .SetDisplay("Slow EMA Period", "Period for slow EMA in MACD", "Indicators")
        
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Period for signal line in MACD", "Indicators")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # Current state
        self._prev_is_macd_above_signal = False

    @property
    def fast_ema_period(self):
        """Period for fast EMA in MACD."""
        return self._fast_ema_period.Value

    @fast_ema_period.setter
    def fast_ema_period(self, value):
        self._fast_ema_period.Value = value

    @property
    def slow_ema_period(self):
        """Period for slow EMA in MACD."""
        return self._slow_ema_period.Value

    @slow_ema_period.setter
    def slow_ema_period(self, value):
        self._slow_ema_period.Value = value

    @property
    def signal_period(self):
        """Period for signal line in MACD."""
        return self._signal_period.Value

    @signal_period.setter
    def signal_period(self, value):
        self._signal_period.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss as percentage of entry price."""
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
        super(macd_trend_strategy, self).OnReseted()
        self._prev_is_macd_above_signal = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(macd_trend_strategy, self).OnStarted(time)

        # Initialize state
        self._prev_is_macd_above_signal = False

        # Create MACD indicator with signal line
        macd = MovingAverageConvergenceDivergenceSignal()
        # Configure MACD parameters
        macd.Macd.ShortMa.Length = self.fast_ema_period
        macd.Macd.LongMa.Length = self.slow_ema_period
        macd.SignalMa.Length = self.signal_period

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

        # Start protection with stop loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, macd_value):
        """
        Processes each finished candle and executes MACD-based trading logic.
        
        :param candle: The processed candle message.
        :param macd_value: The current value of the MACD indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract MACD and Signal values
        if macd_value.Macd is None:
            return
        macd_line = float(macd_value.Macd)

        if macd_value.Signal is None:
            return
        signal_line = float(macd_value.Signal)

        # Check MACD position relative to signal line
        is_macd_above_signal = macd_line > signal_line
        
        # Check for crossovers
        is_macd_crossed_above_signal = is_macd_above_signal and not self._prev_is_macd_above_signal
        is_macd_crossed_below_signal = not is_macd_above_signal and self._prev_is_macd_above_signal

        # Entry/exit logic based on MACD crossovers
        if is_macd_crossed_above_signal and self.Position <= 0:
            # MACD crossed above signal line - Buy signal
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: MACD ({0:F5}) crossed above Signal ({1:F5})".format(
                macd_line, signal_line))
        elif is_macd_crossed_below_signal and self.Position >= 0:
            # MACD crossed below signal line - Sell signal
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: MACD ({0:F5}) crossed below Signal ({1:F5})".format(
                macd_line, signal_line))
        # Exit logic based on opposite crossover
        elif is_macd_crossed_below_signal and self.Position > 0:
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: MACD ({0:F5}) crossed below Signal ({1:F5})".format(
                macd_line, signal_line))
        elif is_macd_crossed_above_signal and self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: MACD ({0:F5}) crossed above Signal ({1:F5})".format(
                macd_line, signal_line))

        # Update previous state
        self._prev_is_macd_above_signal = is_macd_above_signal

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return macd_trend_strategy()
