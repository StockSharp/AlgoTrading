import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage, SimpleMovingAverage,
    SmoothedMovingAverage, WeightedMovingAverage,
    DecimalIndicatorValue
)
from StockSharp.Algo.Strategies import Strategy


class exp_x_bulls_bears_eyes_vol_strategy(Strategy):
    def __init__(self):
        super(exp_x_bulls_bears_eyes_vol_strategy, self).__init__()

        self._primary_volume = self.Param("PrimaryVolume", 0.1) \
            .SetDisplay("Primary Volume", "Order volume used by the first long/short slot", "Trading")
        self._secondary_volume = self.Param("SecondaryVolume", 0.2) \
            .SetDisplay("Secondary Volume", "Order volume used by the second long/short slot", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Take Profit (points)", "Target distance expressed in price steps", "Risk")
        self._allow_long_entry = self.Param("AllowLongEntry", True) \
            .SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading")
        self._allow_short_entry = self.Param("AllowShortEntry", True) \
            .SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading")
        self._allow_long_exit = self.Param("AllowLongExit", True) \
            .SetDisplay("Allow Long Exit", "Enable closing long positions on bearish colours", "Trading")
        self._allow_short_exit = self.Param("AllowShortExit", True) \
            .SetDisplay("Allow Short Exit", "Enable closing short positions on bullish colours", "Trading")
        self._indicator_period = self.Param("IndicatorPeriod", 13) \
            .SetDisplay("Indicator Period", "EMA period used by Bulls/Bears power", "Indicator")
        self._gamma_param = self.Param("Gamma", 0.6) \
            .SetDisplay("Gamma", "Adaptive smoothing factor used by the four-stage filter", "Indicator")
        self._high_level2 = self.Param("HighLevel2", 25) \
            .SetDisplay("High Level 2", "Upper level that marks strong bullish pressure", "Indicator")
        self._high_level1 = self.Param("HighLevel1", 10) \
            .SetDisplay("High Level 1", "Upper level that marks moderate bullish pressure", "Indicator")
        self._low_level1 = self.Param("LowLevel1", -10) \
            .SetDisplay("Low Level 1", "Lower level that marks moderate bearish pressure", "Indicator")
        self._low_level2 = self.Param("LowLevel2", -25) \
            .SetDisplay("Low Level 2", "Lower level that marks strong bearish pressure", "Indicator")
        self._smooth_length = self.Param("SmoothingLength", 12) \
            .SetDisplay("Smoothing Length", "Length of the smoothing filter", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Shift applied before evaluating colour transitions", "Trading")

        self._ema = None
        self._value_smoother = None
        self._volume_smoother = None
        self._color_history = []

        self._l0 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0

        self._last_long_primary_time = None
        self._last_long_secondary_time = None
        self._last_short_primary_time = None
        self._last_short_secondary_time = None
        self._is_long_primary_open = False
        self._is_long_secondary_open = False
        self._is_short_primary_open = False
        self._is_short_secondary_open = False

    @property
    def primary_volume(self):
        return self._primary_volume.Value

    @property
    def secondary_volume(self):
        return self._secondary_volume.Value

    @property
    def stop_loss_points(self):
        return self._stop_loss_points.Value

    @property
    def take_profit_points(self):
        return self._take_profit_points.Value

    @property
    def allow_long_entry(self):
        return self._allow_long_entry.Value

    @property
    def allow_short_entry(self):
        return self._allow_short_entry.Value

    @property
    def allow_long_exit(self):
        return self._allow_long_exit.Value

    @property
    def allow_short_exit(self):
        return self._allow_short_exit.Value

    @property
    def indicator_period(self):
        return self._indicator_period.Value

    @property
    def gamma_val(self):
        return self._gamma_param.Value

    @property
    def high_level2(self):
        return self._high_level2.Value

    @property
    def high_level1(self):
        return self._high_level1.Value

    @property
    def low_level1(self):
        return self._low_level1.Value

    @property
    def low_level2(self):
        return self._low_level2.Value

    @property
    def smooth_length(self):
        return self._smooth_length.Value

    @property
    def signal_bar(self):
        return self._signal_bar.Value

    def OnReseted(self):
        super(exp_x_bulls_bears_eyes_vol_strategy, self).OnReseted()
        self._ema = None
        self._value_smoother = None
        self._volume_smoother = None
        self._color_history = []
        self._l0 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._last_long_primary_time = None
        self._last_long_secondary_time = None
        self._last_short_primary_time = None
        self._last_short_secondary_time = None
        self._is_long_primary_open = False
        self._is_long_secondary_open = False
        self._is_short_primary_open = False
        self._is_short_secondary_open = False

    def OnStarted(self, time):
        super(exp_x_bulls_bears_eyes_vol_strategy, self).OnStarted(time)

        period = max(1, self.indicator_period)
        self._ema = ExponentialMovingAverage()
        self._ema.Length = period

        length = max(1, self.smooth_length)
        self._value_smoother = SimpleMovingAverage()
        self._value_smoother.Length = length
        self._volume_smoother = SimpleMovingAverage()
        self._volume_smoother.Length = length

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromHours(8)))
        subscription.Bind(self._process_candle)
        subscription.Start()

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        sl = None
        tp = None
        if self.stop_loss_points > 0:
            sl = Unit(float(self.stop_loss_points) * step, UnitTypes.Absolute)
        if self.take_profit_points > 0:
            tp = Unit(float(self.take_profit_points) * step, UnitTypes.Absolute)
        if sl is not None or tp is not None:
            self.StartProtection(stopLoss=sl, takeProfit=tp, useMarketOrders=True)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._ema is None:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        ema_iv = DecimalIndicatorValue(self._ema, candle.ClosePrice, candle.OpenTime)
        ema_iv.IsFinal = True
        ema_result = self._ema.Process(ema_iv)
        if not self._ema.IsFormed:
            return
        ema_val = float(ema_result)

        bulls = high - ema_val
        bears = low - ema_val
        combined = bulls + bears

        gamma = min(0.999, max(0.0, float(self.gamma_val)))

        l0 = (1.0 - gamma) * combined + gamma * self._l0
        l1 = -gamma * l0 + self._l0 + gamma * self._l1
        l2 = -gamma * l1 + self._l1 + gamma * self._l2
        l3 = -gamma * l2 + self._l2 + gamma * self._l3

        self._l0 = l0
        self._l1 = l1
        self._l2 = l2
        self._l3 = l3

        cu = 0.0
        cd = 0.0
        if l0 >= l1:
            cu += l0 - l1
        else:
            cd += l1 - l0
        if l1 >= l2:
            cu += l1 - l2
        else:
            cd += l2 - l1
        if l2 >= l3:
            cu += l2 - l3
        else:
            cd += l3 - l2

        total = cu + cd
        ratio = cu / total if total > 0.0 else 0.0
        base_value = ratio * 100.0 - 50.0

        volume = float(candle.TotalVolume) if candle.TotalVolume > 0 else 1.0
        scaled = base_value * volume

        from System import Decimal
        sv_iv = DecimalIndicatorValue(self._value_smoother, Decimal(scaled), candle.OpenTime)
        sv_iv.IsFinal = True
        sv_result = self._value_smoother.Process(sv_iv)
        vv_iv = DecimalIndicatorValue(self._volume_smoother, Decimal(volume), candle.OpenTime)
        vv_iv.IsFinal = True
        vv_result = self._volume_smoother.Process(vv_iv)

        if not self._value_smoother.IsFormed or not self._volume_smoother.IsFormed:
            return

        smoothed_value = float(sv_result)
        smoothed_volume = float(vv_result)

        color = self._determine_color(smoothed_value, smoothed_volume)

        signal_time = candle.CloseTime if candle.CloseTime is not None else candle.OpenTime
        self._color_history.append((signal_time, smoothed_value, smoothed_volume, color))
        if len(self._color_history) > 1024:
            self._color_history = self._color_history[-1024:]

        ctx = self._get_signal_context()
        if ctx is None:
            return
        current_color, previous_color, color_time = ctx

        open_long_primary = False
        open_long_secondary = False
        open_short_primary = False
        open_short_secondary = False
        close_long = False
        close_short = False

        if current_color == 1:
            if self.allow_long_entry and previous_color > 1:
                open_long_primary = True
            if self.allow_short_exit:
                close_short = True

        if current_color == 0:
            if self.allow_long_entry and previous_color > 0:
                open_long_secondary = True
            if self.allow_short_exit:
                close_short = True

        if current_color == 3:
            if self.allow_short_entry and previous_color < 3:
                open_short_primary = True
            if self.allow_long_exit:
                close_long = True

        if current_color == 4:
            if self.allow_short_entry and previous_color < 4:
                open_short_secondary = True
            if self.allow_long_exit:
                close_long = True

        if close_long and self.Position > 0:
            self.SellMarket()
            self._is_long_primary_open = False
            self._is_long_secondary_open = False
            self._last_long_primary_time = None
            self._last_long_secondary_time = None

        if close_short and self.Position < 0:
            self.BuyMarket()
            self._is_short_primary_open = False
            self._is_short_secondary_open = False
            self._last_short_primary_time = None
            self._last_short_secondary_time = None

        if open_long_primary and not self._is_long_primary_open and self._last_long_primary_time != color_time:
            if float(self.primary_volume) > 0.0:
                self.BuyMarket()
                self._is_long_primary_open = True
                self._last_long_primary_time = color_time

        if open_long_secondary and not self._is_long_secondary_open and self._last_long_secondary_time != color_time:
            if float(self.secondary_volume) > 0.0:
                self.BuyMarket()
                self._is_long_secondary_open = True
                self._last_long_secondary_time = color_time

        if open_short_primary and not self._is_short_primary_open and self._last_short_primary_time != color_time:
            if float(self.primary_volume) > 0.0:
                self.SellMarket()
                self._is_short_primary_open = True
                self._last_short_primary_time = color_time

        if open_short_secondary and not self._is_short_secondary_open and self._last_short_secondary_time != color_time:
            if float(self.secondary_volume) > 0.0:
                self.SellMarket()
                self._is_short_secondary_open = True
                self._last_short_secondary_time = color_time

    def _determine_color(self, value, volume):
        max_level = float(self.high_level2) * volume
        up_level = float(self.high_level1) * volume
        down_level = float(self.low_level1) * volume
        min_level = float(self.low_level2) * volume

        if value > max_level:
            return 0
        if value > up_level:
            return 1
        if value < min_level:
            return 4
        if value < down_level:
            return 3
        return 2

    def _get_signal_context(self):
        sb = self.signal_bar
        if sb < 0:
            return None
        index = len(self._color_history) - 1 - sb
        if index < 0 or index >= len(self._color_history):
            return None
        prev_index = index - 1
        if prev_index < 0:
            return None
        current_sample = self._color_history[index]
        previous_sample = self._color_history[prev_index]
        return (current_sample[3], previous_sample[3], current_sample[0])

    def CreateClone(self):
        return exp_x_bulls_bears_eyes_vol_strategy()
