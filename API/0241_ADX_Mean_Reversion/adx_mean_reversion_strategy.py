import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Sides
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class adx_mean_reversion_strategy(Strategy):
    """
    ADX Mean Reversion strategy. This strategy enters positions when ADX is
    significantly below or above its average value.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(adx_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 5) \
            .SetDisplay("ADX Period", "Period for ADX indicator", "Indicators")

        self._average_period = self.Param("AveragePeriod", 20) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 10) \
            .SetDisplay("Average Period", "Period for calculating ADX average and standard deviation", "Settings")

        self._deviation_multiplier = self.Param("DeviationMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5) \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation", "Settings")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")

        # Internal state variables
        self._prev_adx = 0.0
        self._avg_adx = 0.0
        self._std_dev_adx = 0.0
        self._sum_adx = 0.0
        self._sum_squares_adx = 0.0
        self._count = 0
        self._adx_values = []

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def AveragePeriod(self):
        return self._average_period.Value

    @AveragePeriod.setter
    def AveragePeriod(self, value):
        self._average_period.Value = value

    @property
    def DeviationMultiplier(self):
        return self._deviation_multiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Resets statistics, creates indicators,
        and sets up charting.
        """
        super(adx_mean_reversion_strategy, self).OnStarted(time)

        # Reset variables
        self._prev_adx = 0.0
        self._avg_adx = 0.0
        self._std_dev_adx = 0.0
        self._sum_adx = 0.0
        self._sum_squares_adx = 0.0
        self._count = 0
        self._adx_values.clear()

        # Create ADX indicator
        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(adx, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=None,  # We'll manage exits ourselves based on ADX
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
        )

    def ProcessCandle(self, candle, adx_value):
        """
        Process candle with ADX indicator value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        adx_typed = adx_value
        if not hasattr(adx_typed, 'MovingAverage') or adx_typed.MovingAverage is None:
            return
        current_adx = float(adx_typed.MovingAverage)

        dx = getattr(adx_typed, 'Dx', None)
        if dx is None or not hasattr(dx, 'Plus') or dx.Plus is None or not hasattr(dx, 'Minus') or dx.Minus is None:
            return
        plus_di = float(dx.Plus)
        minus_di = float(dx.Minus)

        # Update ADX statistics
        self.UpdateAdxStatistics(current_adx)

        # Save current ADX for next iteration
        self._prev_adx = current_adx

        # If we don't have enough data yet for statistics
        if self._count < self.AveragePeriod:
            return

        if self.Position == 0:
            # Positive trend strength should correspond to price direction for entry
            direction = Sides.Buy if plus_di > minus_di else Sides.Sell

            # ADX significantly below average - expect rise
            if current_adx < self._avg_adx - self.DeviationMultiplier * self._std_dev_adx:
                if direction == Sides.Buy:
                    self.BuyMarket(self.Volume)
                    self.LogInfo(
                        f"Long entry: ADX = {current_adx}, Avg = {self._avg_adx}, StdDev = {self._std_dev_adx}, +DI > -DI")
                else:
                    self.SellMarket(self.Volume)
                    self.LogInfo(
                        f"Short entry: ADX = {current_adx}, Avg = {self._avg_adx}, StdDev = {self._std_dev_adx}, +DI < -DI")
            # ADX significantly above average - expect fall (trend exhaustion)
            elif current_adx > self._avg_adx + self.DeviationMultiplier * self._std_dev_adx:
                if direction == Sides.Sell:
                    self.BuyMarket(self.Volume)
                    self.LogInfo(
                        f"Long entry (trend strength exhaustion): ADX = {current_adx}, Avg = {self._avg_adx}, StdDev = {self._std_dev_adx}")
                else:
                    self.SellMarket(self.Volume)
                    self.LogInfo(
                        f"Short entry (trend strength exhaustion): ADX = {current_adx}, Avg = {self._avg_adx}, StdDev = {self._std_dev_adx}")
        elif self.Position > 0:
            # Long position exit condition
            if current_adx > self._avg_adx:
                self.ClosePosition()
                self.LogInfo(f"Long exit: ADX = {current_adx}, Avg = {self._avg_adx}")
        elif self.Position < 0:
            # Short position exit condition
            if current_adx < self._avg_adx:
                self.ClosePosition()
                self.LogInfo(f"Short exit: ADX = {current_adx}, Avg = {self._avg_adx}")

    def UpdateAdxStatistics(self, current_adx):
        """Update running mean and standard deviation of ADX."""
        self._adx_values.append(current_adx)
        self._sum_adx += current_adx
        self._sum_squares_adx += current_adx * current_adx
        self._count += 1

        if len(self._adx_values) > self.AveragePeriod:
            oldest_adx = self._adx_values.pop(0)
            self._sum_adx -= oldest_adx
            self._sum_squares_adx -= oldest_adx * oldest_adx
            self._count -= 1

        if self._count > 0:
            self._avg_adx = self._sum_adx / self._count
            if self._count > 1:
                variance = (self._sum_squares_adx - (self._sum_adx * self._sum_adx) / self._count) / (self._count - 1)
                self._std_dev_adx = 0 if variance <= 0 else Math.Sqrt(float(variance))
            else:
                self._std_dev_adx = 0

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adx_mean_reversion_strategy()

