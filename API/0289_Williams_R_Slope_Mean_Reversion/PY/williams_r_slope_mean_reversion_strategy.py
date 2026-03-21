import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class williams_r_slope_mean_reversion_strategy(Strategy):
    """
    Williams %R slope mean reversion strategy.
    Trades reversions from extreme Williams %R slopes and exits when the slope returns to its recent average.
    """

    def __init__(self):
        super(williams_r_slope_mean_reversion_strategy, self).__init__()

        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicator Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 2)

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._deviation_multiplier = self.Param("DeviationMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation to determine entry threshold", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._long_williams_level = self.Param("LongWilliamsLevel", -80.0) \
            .SetDisplay("Long Williams Level", "Maximum Williams %R level for long entries", "Signal Filters")

        self._short_williams_level = self.Param("ShortWilliamsLevel", -20.0) \
            .SetDisplay("Short Williams Level", "Minimum Williams %R level for short entries", "Signal Filters")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._williams_r = None
        self._previous_williams_value = 0.0
        self._slope_history = []
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    @property
    def WilliamsRPeriod(self):
        return self._williams_r_period.Value

    @WilliamsRPeriod.setter
    def WilliamsRPeriod(self, value):
        self._williams_r_period.Value = value

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookback_period.Value = value

    @property
    def DeviationMultiplier(self):
        return self._deviation_multiplier.Value

    @DeviationMultiplier.setter
    def DeviationMultiplier(self, value):
        self._deviation_multiplier.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def LongWilliamsLevel(self):
        return self._long_williams_level.Value

    @LongWilliamsLevel.setter
    def LongWilliamsLevel(self, value):
        self._long_williams_level.Value = value

    @property
    def ShortWilliamsLevel(self):
        return self._short_williams_level.Value

    @ShortWilliamsLevel.setter
    def ShortWilliamsLevel(self, value):
        self._short_williams_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(williams_r_slope_mean_reversion_strategy, self).OnReseted()
        self._williams_r = None
        self._previous_williams_value = 0.0
        self._slope_history = [0.0] * self.LookbackPeriod
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted(self, time):
        super(williams_r_slope_mean_reversion_strategy, self).OnStarted(time)

        self._williams_r = WilliamsR()
        self._williams_r.Length = self.WilliamsRPeriod
        self._slope_history = [0.0] * self.LookbackPeriod
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._williams_r, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._williams_r)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, williams_r_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._williams_r.IsFormed:
            return

        williams_r_value = float(williams_r_value)

        if not self._is_initialized:
            self._previous_williams_value = williams_r_value
            self._is_initialized = True
            return

        slope = williams_r_value - self._previous_williams_value
        self._previous_williams_value = williams_r_value

        self._slope_history[self._current_index] = slope
        self._current_index = (self._current_index + 1) % self.LookbackPeriod

        if self._filled_count < self.LookbackPeriod:
            self._filled_count += 1

        if self._filled_count < self.LookbackPeriod:
            return

        # Calculate average slope
        average_slope = sum(self._slope_history) / self.LookbackPeriod

        # Calculate std dev
        sum_sq = 0.0
        for s in self._slope_history:
            diff = s - average_slope
            sum_sq += diff * diff
        slope_std_dev = Math.Sqrt(sum_sq / self.LookbackPeriod)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        lower_threshold = average_slope - self.DeviationMultiplier * slope_std_dev
        upper_threshold = average_slope + self.DeviationMultiplier * slope_std_dev

        if self.Position == 0:
            if slope < lower_threshold and williams_r_value <= self.LongWilliamsLevel:
                self.BuyMarket()
                self._cooldown = self.CooldownBars
            elif slope > upper_threshold and williams_r_value >= self.ShortWilliamsLevel:
                self.SellMarket()
                self._cooldown = self.CooldownBars
        elif self.Position > 0 and slope >= average_slope:
            self.SellMarket(abs(self.Position))
            self._cooldown = self.CooldownBars
        elif self.Position < 0 and slope <= average_slope:
            self.BuyMarket(abs(self.Position))
            self._cooldown = self.CooldownBars

    def CreateClone(self):
        return williams_r_slope_mean_reversion_strategy()
