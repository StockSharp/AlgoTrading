import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class macd_breakout_strategy(Strategy):
    """
    MACD Breakout Strategy that enters positions when MACD Histogram breaks out of its normal range.
    """

    def __init__(self):
        super(macd_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._fast_ema_period = self.Param("FastEmaPeriod", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA Period", "Period for MACD fast EMA", "MACD Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(8, 20, 4)

        self._slow_ema_period = self.Param("SlowEmaPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA Period", "Period for MACD slow EMA", "MACD Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 40, 4)

        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Period", "Period for MACD signal line", "MACD Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 15, 2)

        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Period", "Period for MACD Histogram moving average", "Indicator Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout threshold", "Breakout Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 4.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Indicators
        self._macd = None
        self._macd_hist_sma = None
        self._macd_hist_stddev = None
        self._prev_macd_hist_value = 0.0
        self._prev_macd_hist_sma_value = 0.0

    @property
    def fast_ema_period(self):
        """MACD Fast EMA period."""
        return self._fast_ema_period.Value

    @fast_ema_period.setter
    def fast_ema_period(self, value):
        self._fast_ema_period.Value = value

    @property
    def slow_ema_period(self):
        """MACD Slow EMA period."""
        return self._slow_ema_period.Value

    @slow_ema_period.setter
    def slow_ema_period(self, value):
        self._slow_ema_period.Value = value

    @property
    def signal_period(self):
        """MACD Signal line period."""
        return self._signal_period.Value

    @signal_period.setter
    def signal_period(self, value):
        self._signal_period.Value = value

    @property
    def sma_period(self):
        """Period for MACD Histogram moving average."""
        return self._sma_period.Value

    @sma_period.setter
    def sma_period(self, value):
        self._sma_period.Value = value

    @property
    def deviation_multiplier(self):
        """Standard deviation multiplier for breakout threshold."""
        return self._deviation_multiplier.Value

    @deviation_multiplier.setter
    def deviation_multiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """Return the security and candle type this strategy works with."""
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(macd_breakout_strategy, self).OnStarted(time)

        self._prev_macd_hist_sma_value = 0
        self._prev_macd_hist_value = 0

        # Initialize indicators
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.fast_ema_period
        self._macd.Macd.LongMa.Length = self.slow_ema_period
        self._macd.SignalMa.Length = self.signal_period
        self._macd_hist_sma = SimpleMovingAverage()
        self._macd_hist_sma.Length = self.sma_period
        self._macd_hist_stddev = StandardDeviation()
        self._macd_hist_stddev.Length = self.sma_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._macd, self.ProcessCandle).Start()

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(self.stop_loss_percent, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_percent * 1.5, UnitTypes.Percent)
        )
        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._macd)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, macd_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return


        # Extract the histogram value (MACD Line - Signal Line)
        if macd_value.Macd is None or macd_value.Signal is None:
            return

        macd = float(macd_value.Macd)

        # Process indicators for MACD histogram
        macd_hist_sma_value = float(process_float(self._macd_hist_sma, macd, candle.ServerTime, candle.State == CandleStates.Finished))
        macd_hist_stddev_value = float(process_float(self._macd_hist_stddev, macd, candle.ServerTime, candle.State == CandleStates.Finished))

        # Store previous values on first call
        if self._prev_macd_hist_value == 0 and self._prev_macd_hist_sma_value == 0:
            self._prev_macd_hist_value = macd
            self._prev_macd_hist_sma_value = macd_hist_sma_value
            return

        # Calculate breakout thresholds
        upper_threshold = macd_hist_sma_value + self.deviation_multiplier * macd_hist_stddev_value
        lower_threshold = macd_hist_sma_value - self.deviation_multiplier * macd_hist_stddev_value

        # Trading logic
        if macd > upper_threshold and self.Position <= 0:
            # MACD Histogram broke above upper threshold - buy signal (long)
            self.BuyMarket(self.Volume)
            self.LogInfo("Buy signal: MACD Hist({0}) > Upper Threshold({1})".format(macd, upper_threshold))
        elif macd < lower_threshold and self.Position >= 0:
            # MACD Histogram broke below lower threshold - sell signal (short)
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Sell signal: MACD Hist({0}) < Lower Threshold({1})".format(macd, lower_threshold))
        # Exit conditions
        elif self.Position > 0 and macd < macd_hist_sma_value:
            # Exit long position when MACD Histogram returns below its mean
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exit long: MACD Hist({0}) < SMA({1})".format(macd, macd_hist_sma_value))
        elif self.Position < 0 and macd > macd_hist_sma_value:
            # Exit short position when MACD Histogram returns above its mean
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: MACD Hist({0}) > SMA({1})".format(macd, macd_hist_sma_value))

        # Update previous values
        self._prev_macd_hist_value = macd
        self._prev_macd_hist_sma_value = macd_hist_sma_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return macd_breakout_strategy()
