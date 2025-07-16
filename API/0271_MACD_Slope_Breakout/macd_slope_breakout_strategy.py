import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("System.Collections")

from System import TimeSpan, Math
from System.Collections.Generic import Queue
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, LinearRegression
from StockSharp.Algo.Strategies import Strategy

class macd_slope_breakout_strategy(Strategy):
    """
    MACD Slope Breakout Strategy (Strategy #271)
    """

    def __init__(self):
        super(macd_slope_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._fast_ema = self.Param("FastEma", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Fast EMA period", "MACD") \
            .SetCanOptimize(True) \
            .SetOptimize(8, 16, 2)

        self._slow_ema = self.Param("SlowEma", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Slow EMA period", "MACD") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 30, 2)

        self._signal_ma = self.Param("SignalMa", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal MA", "Signal MA period", "MACD") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 12, 1)

        self._slope_period = self.Param("SlopePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Period", "Period for slope average and standard deviation", "Signal") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._breakout_multiplier = self.Param("BreakoutMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Breakout Multiplier", "Standard deviation multiplier for breakout", "Signal") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._macd = None
        self._macd_hist_slope = None
        self._prev_slope_value = 0
        self._slope_avg = 0
        self._slope_std_dev = 0
        self._sum_slope = 0
        self._sum_slope_squared = 0
        self._slope_values = Queue[float]()

    @property
    def fast_ema(self):
        return self._fast_ema.Value

    @fast_ema.setter
    def fast_ema(self, value):
        self._fast_ema.Value = value

    @property
    def slow_ema(self):
        return self._slow_ema.Value

    @slow_ema.setter
    def slow_ema(self, value):
        self._slow_ema.Value = value

    @property
    def signal_ma(self):
        return self._signal_ma.Value

    @signal_ma.setter
    def signal_ma(self, value):
        self._signal_ma.Value = value

    @property
    def slope_period(self):
        return self._slope_period.Value

    @slope_period.setter
    def slope_period(self, value):
        self._slope_period.Value = value

    @property
    def breakout_multiplier(self):
        return self._breakout_multiplier.Value

    @breakout_multiplier.setter
    def breakout_multiplier(self, value):
        self._breakout_multiplier.Value = value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(macd_slope_breakout_strategy, self).OnStarted(time)

        # Initialize indicators
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.fast_ema
        self._macd.Macd.LongMa.Length = self.slow_ema
        self._macd.SignalMa.Length = self.signal_ma
        self._macd_hist_slope = LinearRegression()
        self._macd_hist_slope.Length = 2  # For calculating slope

        self._prev_slope_value = 0
        self._slope_avg = 0
        self._slope_std_dev = 0
        self._sum_slope = 0
        self._sum_slope_squared = 0
        self._slope_values.Clear()

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._macd, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._macd)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(None, Unit(self.stop_loss_percent, UnitTypes.Percent))

    def ProcessCandle(self, candle, macd_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd_typed = macd_value
        if macd_typed.Macd is None or macd_typed.Signal is None:
            return

        # Calculate MACD histogram value (MACD - Signal)
        macd_hist = macd_typed.Macd - macd_typed.Signal

        # Calculate MACD histogram slope
        current_slope_typed = self._macd_hist_slope.Process(macd_hist, candle.ServerTime, candle.State == CandleStates.Finished)
        current_slope_value = current_slope_typed.LinearReg
        if current_slope_value is None:
            return

        # Update slope stats when we have 2 values to calculate slope
        if self._prev_slope_value != 0:
            # Calculate simple slope from current and previous values
            slope = current_slope_value - self._prev_slope_value

            # Update running statistics
            self._slope_values.Enqueue(slope)
            self._sum_slope += slope
            self._sum_slope_squared += slope * slope

            # Remove oldest value if we have enough
            if self._slope_values.Count > self.slope_period:
                old_slope = self._slope_values.Dequeue()
                self._sum_slope -= old_slope
                self._sum_slope_squared -= old_slope * old_slope

            # Calculate average and standard deviation
            self._slope_avg = self._sum_slope / self._slope_values.Count
            variance = (self._sum_slope_squared / self._slope_values.Count) - (self._slope_avg * self._slope_avg)
            self._slope_std_dev = 0 if variance <= 0 else Math.Sqrt(float(variance))

            # Generate signals if we have enough data for statistics
            if self._slope_values.Count >= self.slope_period:
                # Breakout logic
                if slope > self._slope_avg + self.breakout_multiplier * self._slope_std_dev and self.Position <= 0:
                    # Long position on bullish histogram slope breakout
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo("Long entry: MACD histogram slope breakout above {0:F5}".format(self._slope_avg + self.breakout_multiplier * self._slope_std_dev))
                elif slope < self._slope_avg - self.breakout_multiplier * self._slope_std_dev and self.Position >= 0:
                    # Short position on bearish histogram slope breakout
                    self.SellMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo("Short entry: MACD histogram slope breakout below {0:F5}".format(self._slope_avg - self.breakout_multiplier * self._slope_std_dev))

                # Exit logic - Return to mean
                if self.Position > 0 and slope < self._slope_avg:
                    self.SellMarket(Math.Abs(self.Position))
                    self.LogInfo("Long exit: MACD histogram slope returned to mean")
                elif self.Position < 0 and slope > self._slope_avg:
                    self.BuyMarket(Math.Abs(self.Position))
                    self.LogInfo("Short exit: MACD histogram slope returned to mean")

        # Update previous value for next iteration
        self._prev_slope_value = current_slope_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return macd_slope_breakout_strategy()
