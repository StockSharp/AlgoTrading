import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates, Level1Fields
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class volume_slope_mean_reversion_strategy(Strategy):
    """
    Volume slope mean reversion strategy.
    Trades reversion of extreme volume-ratio slope values.
    """

    def __init__(self):
        super(volume_slope_mean_reversion_strategy, self).__init__()

        self._volume_ma_period = self.Param("VolumeMaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume MA Period", "Period for the volume moving average", "Indicator Parameters")

        self._slope_lookback = self.Param("SlopeLookback", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slope Lookback", "Period for slope statistics", "Strategy Parameters")

        self._threshold_multiplier = self.Param("ThresholdMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Threshold Multiplier", "Standard deviation multiplier for entries", "Strategy Parameters")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")

        self._cooldown_bars = self.Param("CooldownBars", 1200) \
            .SetDisplay("Cooldown Bars", "Bars to wait between orders", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._volume_average = None
        self._previous_volume_ratio = 0.0
        self._slope_history = None
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_slope_mean_reversion_strategy, self).OnReseted()
        self._volume_average = None
        self._previous_volume_ratio = 0.0
        lb = int(self._slope_lookback.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0
        self._is_initialized = False

    def OnStarted2(self, time):
        super(volume_slope_mean_reversion_strategy, self).OnStarted2(time)

        lb = int(self._slope_lookback.Value)
        self._slope_history = [0.0] * lb
        self._current_index = 0
        self._filled_count = 0
        self._cooldown = 0

        self._volume_average = SimpleMovingAverage()
        self._volume_average.Length = int(self._volume_ma_period.Value)
        self._volume_average.Source = Level1Fields.Volume

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._volume_average, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(self._stop_loss_percent.Value, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._volume_average)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, average_volume):
        if candle.State != CandleStates.Finished:
            return

        if not self._volume_average.IsFormed:
            return

        av = float(average_volume)
        if av <= 0:
            return

        volume_ratio = float(candle.TotalVolume) / av

        if not self._is_initialized:
            self._previous_volume_ratio = volume_ratio
            self._is_initialized = True
            return

        slope = volume_ratio - self._previous_volume_ratio
        self._previous_volume_ratio = volume_ratio

        lb = int(self._slope_lookback.Value)
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

        if std_slope <= 0:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        tm = float(self._threshold_multiplier.Value)
        lower_threshold = avg_slope - tm * std_slope
        upper_threshold = avg_slope + tm * std_slope
        is_bullish_candle = float(candle.ClosePrice) >= float(candle.OpenPrice)
        is_bearish_candle = float(candle.ClosePrice) <= float(candle.OpenPrice)

        if self.Position == 0:
            if slope <= lower_threshold and is_bullish_candle:
                self.BuyMarket()
                self._cooldown = int(self._cooldown_bars.Value)
            elif slope >= upper_threshold and is_bearish_candle:
                self.SellMarket()
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position > 0:
            if slope >= avg_slope or is_bearish_candle:
                self.SellMarket(Math.Abs(self.Position))
                self._cooldown = int(self._cooldown_bars.Value)
        elif self.Position < 0:
            if slope <= avg_slope or is_bullish_candle:
                self.BuyMarket(Math.Abs(self.Position))
                self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return volume_slope_mean_reversion_strategy()
