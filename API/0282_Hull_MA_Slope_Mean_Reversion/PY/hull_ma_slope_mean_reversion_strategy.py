import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hull_ma_slope_mean_reversion_strategy(Strategy):
    """
    Hull moving average slope mean reversion strategy.
    Trades reversions from extreme Hull MA slopes and exits when the slope returns to its recent average.
    """

    def __init__(self):
        super(hull_ma_slope_mean_reversion_strategy, self).__init__()

        self._hull_period = self.Param("HullPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Hull MA Period", "Hull Moving Average period", "Hull MA")

        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Lookback period for slope statistics", "Strategy Parameters")

        self._deviation_multiplier = self.Param("DeviationMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Strategy Parameters")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        self._hull_ma = None
        self._prev_hull_value = 0.0
        self._slope_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_ma_slope_mean_reversion_strategy, self).OnReseted()
        self._hull_ma = None
        self._prev_hull_value = 0.0
        lb = int(self._lookback_period.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted(self, time):
        super(hull_ma_slope_mean_reversion_strategy, self).OnStarted(time)

        lb = int(self._lookback_period.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        self._hull_ma = HullMovingAverage()
        self._hull_ma.Length = int(self._hull_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._hull_ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._hull_ma)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

    def _process_candle(self, candle, hull_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._hull_ma.IsFormed:
            return

        hv = float(hull_value)

        if not self._is_initialized:
            self._prev_hull_value = hv
            self._is_initialized = True
            return

        if self._prev_hull_value == 0:
            return

        slope = (hv - self._prev_hull_value) / self._prev_hull_value * 100.0
        self._prev_hull_value = hv

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
        high_threshold = avg_slope + std_slope * dm
        low_threshold = avg_slope - std_slope * dm

        if self.Position == 0:
            if slope < low_threshold:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif slope > high_threshold:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0 and slope >= avg_slope:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0 and slope <= avg_slope:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return hull_ma_slope_mean_reversion_strategy()
