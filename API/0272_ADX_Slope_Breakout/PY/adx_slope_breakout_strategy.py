import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class adx_slope_breakout_strategy(Strategy):
    """
    Strategy based on ADX slope breakout.
    Opens positions when ADX slope deviates from its recent average and the dominant DI confirms direction.
    """

    def __init__(self):
        super(adx_slope_breakout_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicator Parameters") \
            .SetOptimize(10, 20, 2)

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

        self._min_adx = self.Param("MinAdx", 25.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min ADX", "Minimum ADX level required for entries", "Signal Filters")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._adx = None
        self._prev_adx = 0.0
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
        super(adx_slope_breakout_strategy, self).OnReseted()
        self._adx = None
        self._prev_adx = 0.0
        self._current_slope = 0.0
        self._avg_slope = 0.0
        self._std_dev_slope = 0.0
        sp = int(self._slope_period.Value)
        self._slopes = [0.0] * sp
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted(self, time):
        super(adx_slope_breakout_strategy, self).OnStarted(time)

        sp = int(self._slope_period.Value)
        self._slopes = [0.0] * sp
        self._cooldown = 0
        self._filled_count = 0
        self._current_index = 0

        self._adx = AverageDirectionalIndex()
        self._adx.Length = int(self._adx_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._adx, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

    def _process_candle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._adx.IsFormed:
            return

        adx_ma = adx_value.MovingAverage
        if adx_ma is None:
            return

        dx = adx_value.Dx
        di_plus = dx.Plus
        di_minus = dx.Minus
        if di_plus is None or di_minus is None:
            return

        adx_val = float(adx_ma)
        di_plus_val = float(di_plus)
        di_minus_val = float(di_minus)

        if not self._is_initialized:
            self._prev_adx = adx_val
            self._is_initialized = True
            return

        self._current_slope = adx_val - self._prev_adx
        self._prev_adx = adx_val

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
        is_bullish = di_plus_val > di_minus_val
        is_bearish = di_minus_val > di_plus_val
        min_adx = float(self._min_adx.Value)

        if self.Position == 0:
            if self._current_slope > upper_threshold and adx_val >= min_adx:
                if is_bullish:
                    self.BuyMarket()
                    self._cooldown = int(self._cooldown_bars.Value)
                elif is_bearish:
                    self.SellMarket()
                    self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0:
            if self._current_slope <= self._avg_slope or not is_bullish:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0:
            if self._current_slope <= self._avg_slope or not is_bearish:
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
        return adx_slope_breakout_strategy()
