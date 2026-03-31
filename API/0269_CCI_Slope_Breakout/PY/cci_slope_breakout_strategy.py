import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class cci_slope_breakout_strategy(Strategy):
    """
    Strategy based on CCI slope breakout.
    Opens positions when CCI slope deviates from its recent average by a multiple of standard deviation.
    """

    def __init__(self):
        super(cci_slope_breakout_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "Period for CCI calculation", "Indicator Parameters") \
            .SetOptimize(10, 30, 5)

        self._slope_period = self.Param("SlopePeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Period", "Period for slope statistics calculation", "Strategy Parameters") \
            .SetOptimize(10, 50, 5)

        self._breakout_multiplier = self.Param("BreakoutMultiplier", 2.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Breakout Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters") \
            .SetOptimize(1.5, 4.0, 0.5)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._min_cci_magnitude = self.Param("MinCciMagnitude", 100.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min CCI Magnitude", "Minimum absolute CCI value required for entries", "Signal Filters")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cci = None
        self._prev_cci = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        self._slopes = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cci_slope_breakout_strategy, self).OnReseted()
        self._cci = None
        self._prev_cci = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        sp = int(self._slope_period.Value)
        self._slopes = [0.0] * sp
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted2(self, time):
        super(cci_slope_breakout_strategy, self).OnStarted2(time)

        sp = int(self._slope_period.Value)
        self._slopes = [0.0] * sp
        self._cooldown = 0
        self._filled_count = 0
        self._current_index = 0

        self._cci = CommodityChannelIndex()
        self._cci.Length = int(self._cci_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._cci, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._cci)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

    def _process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._cci.IsFormed:
            return

        cci_val = float(cci_value)

        if not self._is_initialized:
            self._prev_cci = cci_val
            self._is_initialized = True
            return

        self._current_slope = cci_val - self._prev_cci
        self._prev_cci = cci_val

        sp = int(self._slope_period.Value)
        self._slopes[self._current_index] = self._current_slope
        self._current_index = (self._current_index + 1) % sp

        if self._filled_count < sp:
            self._filled_count += 1

        if self._filled_count < sp:
            return

        self._calculate_statistics()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._std_dev_slope <= 0:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        bm = float(self._breakout_multiplier.Value)
        upper_threshold = self._avg_slope + bm * self._std_dev_slope
        lower_threshold = self._avg_slope - bm * self._std_dev_slope
        abs_cci = abs(cci_val)
        min_mag = float(self._min_cci_magnitude.Value)

        if self.Position == 0:
            if self._current_slope > upper_threshold and cci_val > 0 and abs_cci >= min_mag:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif self._current_slope < lower_threshold and cci_val < 0 and abs_cci >= min_mag:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0:
            if self._current_slope <= self._avg_slope:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0:
            if self._current_slope >= self._avg_slope:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = int(self._cooldown_bars.Value)

    def _calculate_statistics(self):
        sp = int(self._slope_period.Value)
        self._avg_slope = 0.0
        sum_sq = 0.0

        for i in range(sp):
            self._avg_slope += self._slopes[i]
        self._avg_slope /= float(sp)

        for i in range(sp):
            diff = self._slopes[i] - self._avg_slope
            sum_sq += diff * diff

        self._std_dev_slope = math.sqrt(sum_sq / float(sp))

    def CreateClone(self):
        return cci_slope_breakout_strategy()
