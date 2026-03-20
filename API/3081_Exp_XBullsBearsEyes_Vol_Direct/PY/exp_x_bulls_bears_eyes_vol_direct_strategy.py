import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage, SimpleMovingAverage,
    DecimalIndicatorValue
)
from StockSharp.Algo.Strategies import Strategy


class exp_x_bulls_bears_eyes_vol_direct_strategy(Strategy):
    def __init__(self):
        super(exp_x_bulls_bears_eyes_vol_direct_strategy, self).__init__()

        self._period = self.Param("Period", 13) \
            .SetDisplay("Bulls/Bears Period", "Lookback window of Bulls/Bears Power", "Indicator")
        self._gamma_param = self.Param("Gamma", 0.6) \
            .SetDisplay("Gamma", "Adaptive filter smoothing factor", "Indicator")
        self._smooth_length = self.Param("SmoothingLength", 12) \
            .SetDisplay("Smoothing Length", "Length of the smoothing moving averages", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Shift applied when evaluating direction", "Trading")
        self._allow_buy_open = self.Param("AllowBuyOpen", True) \
            .SetDisplay("Allow Buy Open", "Enable opening long positions", "Trading")
        self._allow_sell_open = self.Param("AllowSellOpen", True) \
            .SetDisplay("Allow Sell Open", "Enable opening short positions", "Trading")
        self._allow_buy_close = self.Param("AllowBuyClose", True) \
            .SetDisplay("Allow Buy Close", "Enable closing longs on bearish flips", "Trading")
        self._allow_sell_close = self.Param("AllowSellClose", True) \
            .SetDisplay("Allow Sell Close", "Enable closing shorts on bullish flips", "Trading")
        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetDisplay("Order Volume", "Default market order size", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Stop Loss", "Protective stop in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Take Profit", "Protective target in price steps", "Risk")

        self._ema = None
        self._histogram_smoother = None
        self._volume_smoother = None

        self._direction_history = []
        self._l0 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._previous_smoothed_value = None
        self._previous_direction = 0

    @property
    def period(self):
        return self._period.Value

    @property
    def gamma_val(self):
        return self._gamma_param.Value

    @property
    def smooth_length(self):
        return self._smooth_length.Value

    @property
    def signal_bar(self):
        return self._signal_bar.Value

    @property
    def allow_buy_open(self):
        return self._allow_buy_open.Value

    @property
    def allow_sell_open(self):
        return self._allow_sell_open.Value

    @property
    def allow_buy_close(self):
        return self._allow_buy_close.Value

    @property
    def allow_sell_close(self):
        return self._allow_sell_close.Value

    @property
    def order_volume(self):
        return self._order_volume.Value

    @property
    def stop_loss_points(self):
        return self._stop_loss_points.Value

    @property
    def take_profit_points(self):
        return self._take_profit_points.Value

    def OnReseted(self):
        super(exp_x_bulls_bears_eyes_vol_direct_strategy, self).OnReseted()
        self._ema = None
        self._histogram_smoother = None
        self._volume_smoother = None
        self._direction_history = []
        self._l0 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0
        self._previous_smoothed_value = None
        self._previous_direction = 0

    def OnStarted(self, time):
        super(exp_x_bulls_bears_eyes_vol_direct_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = max(1, self.period)

        length = max(1, self.smooth_length)
        self._histogram_smoother = SimpleMovingAverage()
        self._histogram_smoother.Length = length
        self._volume_smoother = SimpleMovingAverage()
        self._volume_smoother.Length = length

        self._direction_history = []
        self._previous_smoothed_value = None
        self._previous_direction = 0
        self._l0 = 0.0
        self._l1 = 0.0
        self._l2 = 0.0
        self._l3 = 0.0

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromHours(2)))
        subscription.Bind(self._process_candle)
        subscription.Start()

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        sl = None
        tp = None
        if self.stop_loss_points > 0 and step > 0.0:
            sl = Unit(float(self.stop_loss_points) * step, UnitTypes.Absolute)
        if self.take_profit_points > 0 and step > 0.0:
            tp = Unit(float(self.take_profit_points) * step, UnitTypes.Absolute)
        if sl is not None or tp is not None:
            self.StartProtection(stopLoss=sl, takeProfit=tp)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._ema is None or self._histogram_smoother is None or self._volume_smoother is None:
            return

        ema_result = self._ema.Process(DecimalIndicatorValue(self._ema, candle.ClosePrice, candle.OpenTime))
        if not self._ema.IsFormed:
            return
        ema = float(ema_result.ToDecimal())

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        bulls = high - ema
        bears = low - ema
        combined = bulls + bears

        gamma = float(self.gamma_val)

        l0_prev = self._l0
        l1_prev = self._l1
        l2_prev = self._l2
        l3_prev = self._l3

        self._l0 = (1.0 - gamma) * combined + gamma * l0_prev
        self._l1 = -gamma * self._l0 + l0_prev + gamma * l1_prev
        self._l2 = -gamma * self._l1 + l1_prev + gamma * l2_prev
        self._l3 = -gamma * self._l2 + l2_prev + gamma * l3_prev

        cu = 0.0
        cd = 0.0
        if self._l0 >= self._l1:
            cu = self._l0 - self._l1
        else:
            cd = self._l1 - self._l0
        if self._l1 >= self._l2:
            cu += self._l1 - self._l2
        else:
            cd += self._l2 - self._l1
        if self._l2 >= self._l3:
            cu += self._l2 - self._l3
        else:
            cd += self._l3 - self._l2

        denom = cu + cd
        result = cu / denom if denom != 0.0 else 0.0
        histogram = result * 100.0 - 50.0

        volume = float(candle.TotalVolume) if candle.TotalVolume > 0 else 1.0
        scaled_histogram = histogram * volume

        hist_result = self._histogram_smoother.Process(
            DecimalIndicatorValue(self._histogram_smoother, scaled_histogram, candle.OpenTime))
        vol_result = self._volume_smoother.Process(
            DecimalIndicatorValue(self._volume_smoother, volume, candle.OpenTime))

        if not self._histogram_smoother.IsFormed or not self._volume_smoother.IsFormed:
            return

        smoothed_histogram = float(hist_result.ToDecimal())
        direction = self._calculate_direction(smoothed_histogram)
        self._update_history(direction)

        older_color, current_color = self._try_get_colors()
        if older_color is None or current_color is None:
            return

        self._handle_signals(older_color, current_color)

    def _calculate_direction(self, current_value):
        if self._previous_smoothed_value is None:
            self._previous_smoothed_value = current_value
            self._previous_direction = 0
            return self._previous_direction

        if current_value > self._previous_smoothed_value:
            direction = 0
        elif current_value < self._previous_smoothed_value:
            direction = 1
        else:
            direction = self._previous_direction

        self._previous_smoothed_value = current_value
        self._previous_direction = direction
        return direction

    def _update_history(self, direction):
        self._direction_history.append(direction)
        max_history = max(4, self.signal_bar + 3)
        if len(self._direction_history) > max_history:
            self._direction_history.pop(0)

    def _try_get_colors(self):
        shift = max(0, self.signal_bar)
        current_index = len(self._direction_history) - 1 - shift
        older_index = current_index - 1
        if current_index < 0 or older_index < 0:
            return None, None
        return self._direction_history[older_index], self._direction_history[current_index]

    def _handle_signals(self, older_color, current_color):
        if older_color == 0:
            if self.allow_sell_close and self.Position < 0:
                self.BuyMarket()
            if self.allow_buy_open and current_color == 1 and self.Position <= 0:
                self.BuyMarket()
        elif older_color == 1:
            if self.allow_buy_close and self.Position > 0:
                self.SellMarket()
            if self.allow_sell_open and current_color == 0 and self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return exp_x_bulls_bears_eyes_vol_direct_strategy()
