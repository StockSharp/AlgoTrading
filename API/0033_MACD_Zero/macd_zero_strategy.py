import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class macd_zero_strategy(Strategy):
    """
    Strategy that trades MACD reversions to zero line.
    It enters when MACD is below/above zero and trending back towards zero line,
    and exits when MACD crosses its signal line.
    
    """
    
    def __init__(self):
        super(macd_zero_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast EMA Period", "Fast EMA period for MACD calculation", "MACD Parameters")
        
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow EMA Period", "Slow EMA period for MACD calculation", "MACD Parameters")
        
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Signal line period for MACD calculation", "MACD Parameters")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")
        
        # State tracking
        self._prev_macd = 0.0

    @property
    def fast_period(self):
        """Fast EMA period for MACD calculation (default: 12)"""
        return self._fast_period.Value

    @fast_period.setter
    def fast_period(self, value):
        self._fast_period.Value = value

    @property
    def slow_period(self):
        """Slow EMA period for MACD calculation (default: 26)"""
        return self._slow_period.Value

    @slow_period.setter
    def slow_period(self, value):
        self._slow_period.Value = value

    @property
    def signal_period(self):
        """Signal line period for MACD calculation (default: 9)"""
        return self._signal_period.Value

    @signal_period.setter
    def signal_period(self, value):
        self._signal_period.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss as percentage from entry price (default: 2%)"""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Type of candles used for strategy calculation"""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(macd_zero_strategy, self).OnReseted()
        self._prev_macd = 0.0

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(macd_zero_strategy, self).OnStarted(time)

        # Reset state variables
        self._prev_macd = 0.0

        # Create MACD indicator with signal line
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.fast_period
        macd.Macd.LongMa.Length = self.slow_period
        macd.SignalMa.Length = self.signal_period

        # Create subscription and bind MACD indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self.ProcessCandle).Start()

        # Configure chart
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

        # Setup protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, macd_value):
        """
        Process candle and check for MACD signals
        
        :param candle: The processed candle message.
        :param macd_value: The current value of the MACD indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract MACD values
        try:
            if macd_value.Macd is None:
                return
            macd = float(macd_value.Macd)

            if macd_value.Signal is None:
                return
            signal = float(macd_value.Signal)
        except:
            # If we can't extract values, skip this candle
            return

        # Initialize _prev_macd on first formed candle
        if self._prev_macd == 0:
            self._prev_macd = macd
            return

        # Check if MACD is trending towards zero
        is_trending_towards_zero = False
        
        if macd < 0 and macd > self._prev_macd:
            # MACD is negative but increasing (moving towards zero from below)
            is_trending_towards_zero = True
        elif macd > 0 and macd < self._prev_macd:
            # MACD is positive but decreasing (moving towards zero from above)
            is_trending_towards_zero = True

        if self.Position == 0:
            # No position - check for entry signals
            if macd < 0 and is_trending_towards_zero:
                # MACD is below zero and trending back to zero - buy (long)
                self.BuyMarket(self.Volume)
                self.LogInfo("Buy signal: MACD {0:F4} below zero and trending towards zero (prev: {1:F4})".format(
                    macd, self._prev_macd))
            elif macd > 0 and is_trending_towards_zero:
                # MACD is above zero and trending back to zero - sell (short)
                self.SellMarket(self.Volume)
                self.LogInfo("Sell signal: MACD {0:F4} above zero and trending towards zero (prev: {1:F4})".format(
                    macd, self._prev_macd))
        elif self.Position > 0:
            # Long position - check for exit signal
            if macd > signal:
                # MACD crossed above signal line - exit long
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: MACD {0:F4} crossed above signal {1:F4}".format(macd, signal))
        elif self.Position < 0:
            # Short position - check for exit signal
            if macd < signal:
                # MACD crossed below signal line - exit short
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: MACD {0:F4} crossed below signal {1:F4}".format(macd, signal))

        # Update previous MACD value
        self._prev_macd = macd

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return macd_zero_strategy()