import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage, LinearRegression
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class atr_slope_breakout_strategy(Strategy):
    """
    ATR Slope Breakout Strategy
    """

    def __init__(self):
        super(atr_slope_breakout_strategy, self).__init__()

        # Initialize strategy parameters
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicator") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        self._slope_period = self.Param("SlopePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Period", "Period for slope average and standard deviation", "Indicator") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._breakout_multiplier = self.Param("BreakoutMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Breakout Multiplier", "Standard deviation multiplier for breakout", "Signal") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss ATR Multiplier", "ATR multiplier for stop loss", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._atr = None
        self._price_ema = None
        self._atr_slope = None
        self._prev_slope_value = 0.0
        self._slope_avg = 0.0
        self._slope_std_dev = 0.0
        self._sum_slope = 0.0
        self._sum_slope_squared = 0.0
        self._slope_values = []
        self._last_atr = 0.0

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def SlopePeriod(self):
        return self._slope_period.Value

    @SlopePeriod.setter
    def SlopePeriod(self, value):
        self._slope_period.Value = value

    @property
    def BreakoutMultiplier(self):
        return self._breakout_multiplier.Value

    @BreakoutMultiplier.setter
    def BreakoutMultiplier(self, value):
        self._breakout_multiplier.Value = value

    @property
    def StopLossAtrMultiplier(self):
        return self._stop_loss_atr_multiplier.Value

    @StopLossAtrMultiplier.setter
    def StopLossAtrMultiplier(self, value):
        self._stop_loss_atr_multiplier.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED!! Return securities and candle types used."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(atr_slope_breakout_strategy, self).OnStarted(time)

        # Initialize indicators
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod
        self._price_ema = ExponentialMovingAverage()
        self._price_ema.Length = 20  # For trend direction
        self._atr_slope = LinearRegression()
        self._atr_slope.Length = 2  # For calculating slope

        self._prev_slope_value = 0.0
        self._slope_avg = 0.0
        self._slope_std_dev = 0.0
        self._sum_slope = 0.0
        self._sum_slope_squared = 0.0
        self._slope_values.clear()
        self._last_atr = 0.0

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)

        # Using BindEx to get the full indicator value
        subscription.BindEx(self._atr, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._atr)
            self.DrawIndicator(area, self._price_ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, atr_value):
        """Process candle and execute trading logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get ATR value and track it for stop loss calculations
        atr = to_float(atr_value)
        self._last_atr = atr

        # Process price for trend direction
        ema_value = to_float(process_candle(self._price_ema, candle))
        price_above_ema = candle.ClosePrice > ema_value

        # Calculate ATR slope
        current_slope_typed = process_float(
            self._atr_slope,
            atr,
            candle.ServerTime,
            candle.State == CandleStates.Finished,
        )
        if current_slope_typed.LinearReg is None:
            return  # Skip if we can't get the slope value
        current_slope_value = float(current_slope_typed.LinearReg)

        # Update slope stats when we have 2 values to calculate slope
        if self._prev_slope_value != 0:
            # Calculate simple slope from current and previous values
            slope = current_slope_value - self._prev_slope_value

            # Update running statistics
            self._slope_values.append(slope)
            self._sum_slope += slope
            self._sum_slope_squared += slope * slope

            # Remove oldest value if we have enough
            if len(self._slope_values) > self.SlopePeriod:
                old_slope = self._slope_values.pop(0)
                self._sum_slope -= old_slope
                self._sum_slope_squared -= old_slope * old_slope

            # Calculate average and standard deviation
            self._slope_avg = self._sum_slope / len(self._slope_values)
            variance = (self._sum_slope_squared / len(self._slope_values)) - (self._slope_avg * self._slope_avg)
            self._slope_std_dev = 0 if variance <= 0 else Math.Sqrt(float(variance))

            # Generate signals if we have enough data for statistics
            if len(self._slope_values) >= self.SlopePeriod:
                # Breakout logic - ATR slope increase indicates volatility expansion
                if slope > self._slope_avg + self.BreakoutMultiplier * self._slope_std_dev:
                    # Volatility breakout with price direction
                    if price_above_ema and self.Position <= 0:
                        # Go long when price is above EMA (uptrend) during volatility expansion
                        self.BuyMarket(self.Volume + Math.Abs(self.Position))
                        self.LogInfo(
                            "Long entry: ATR slope breakout above {0:F5} with price above EMA".format(
                                self._slope_avg + self.BreakoutMultiplier * self._slope_std_dev))
                    elif not price_above_ema and self.Position >= 0:
                        # Go short when price is below EMA (downtrend) during volatility expansion
                        self.SellMarket(self.Volume + Math.Abs(self.Position))
                        self.LogInfo(
                            "Short entry: ATR slope breakout above {0:F5} with price below EMA".format(
                                self._slope_avg + self.BreakoutMultiplier * self._slope_std_dev))

                # Exit logic - Return to mean (volatility contraction)
                if slope < self._slope_avg:
                    if self.Position > 0:
                        self.SellMarket(Math.Abs(self.Position))
                        self.LogInfo("Long exit: ATR slope returned to mean (volatility contraction)")
                    elif self.Position < 0:
                        self.BuyMarket(Math.Abs(self.Position))
                        self.LogInfo("Short exit: ATR slope returned to mean (volatility contraction)")

        # Update previous value for next iteration
        self._prev_slope_value = current_slope_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return atr_slope_breakout_strategy()
