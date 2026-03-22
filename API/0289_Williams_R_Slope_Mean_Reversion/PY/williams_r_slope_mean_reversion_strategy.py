import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy

class williams_r_slope_mean_reversion_strategy(Strategy):
    """
    Williams %R slope mean reversion strategy.
    Trades reversions from extreme Williams %R slopes and exits when the slope returns to its recent average.
    """

    def __init__(self):
        super(williams_r_slope_mean_reversion_strategy, self).__init__()

        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicator Parameters")

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for slope statistics", "Strategy Parameters")

        self._deviation_multiplier = self.Param("DeviationMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Multiplier for standard deviation to determine entry threshold", "Strategy Parameters")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._long_williams_level = self.Param("LongWilliamsLevel", -80.0) \
            .SetDisplay("Long Williams Level", "Maximum Williams %R level for long entries", "Signal Filters")

        self._short_williams_level = self.Param("ShortWilliamsLevel", -20.0) \
            .SetDisplay("Short Williams Level", "Minimum Williams %R level for short entries", "Signal Filters")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._williams_r = None
        self._previous_williams_value = 0.0
        self._slope_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(williams_r_slope_mean_reversion_strategy, self).OnReseted()
        self._williams_r = None
        self._previous_williams_value = 0.0
        lb = int(self._lookback_period.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted(self, time):
        super(williams_r_slope_mean_reversion_strategy, self).OnStarted(time)

        lb = int(self._lookback_period.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        self._williams_r = WilliamsR()
        self._williams_r.Length = int(self._williams_r_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._williams_r, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._williams_r)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

    def _process_candle(self, candle, williams_r_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._williams_r.IsFormed:
            return

        wv = float(williams_r_value)

        if not self._is_initialized:
            self._previous_williams_value = wv
            self._is_initialized = True
            return

        slope = wv - self._previous_williams_value
        self._previous_williams_value = wv

        lb = int(self._lookback_period.Value)
        self._slope_history[self._current_index] = slope
        self._current_index = (self._current_index + 1) % lb

        if self._filled_count < lb:
            self._filled_count += 1

        if self._filled_count < lb:
            return

        avg_slope = 0.0
        for i in range(lb):
            avg_slope += self._slope_history[i]
        avg_slope /= float(lb)

        sum_sq = 0.0
        for i in range(lb):
            diff = self._slope_history[i] - avg_slope
            sum_sq += diff * diff
        std_slope = math.sqrt(sum_sq / float(lb))

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        dm = float(self._deviation_multiplier.Value)
        lower_threshold = avg_slope - dm * std_slope
        upper_threshold = avg_slope + dm * std_slope
        long_level = float(self._long_williams_level.Value)
        short_level = float(self._short_williams_level.Value)

        if self.Position == 0:
            if slope < lower_threshold and wv <= long_level:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif slope > upper_threshold and wv >= short_level:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0 and slope >= avg_slope:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0 and slope <= avg_slope:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return williams_r_slope_mean_reversion_strategy()
