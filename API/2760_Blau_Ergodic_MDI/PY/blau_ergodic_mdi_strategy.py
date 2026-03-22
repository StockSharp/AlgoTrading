import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage,
    ExponentialMovingAverage,
    SmoothedMovingAverage,
    WeightedMovingAverage,
    DecimalIndicatorValue,
)
from StockSharp.Algo.Strategies import Strategy


class blau_ergodic_mdi_strategy(Strategy):
    MODE_BREAKDOWN = 0
    MODE_TWIST = 1
    MODE_CLOUD_TWIST = 2
    SMOOTH_EMA = 0
    SMOOTH_SMA = 1
    SMOOTH_SMMA = 2
    SMOOTH_WMA = 3
    AP_CLOSE = 0
    AP_OPEN = 1
    AP_HIGH = 2
    AP_LOW = 3
    AP_MEDIAN = 4
    AP_TYPICAL = 5
    AP_WEIGHTED = 6
    AP_SIMPLE = 7
    AP_QUARTER = 8
    AP_TREND0 = 9
    AP_TREND1 = 10

    def __init__(self):
        super(blau_ergodic_mdi_strategy, self).__init__()
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)
        self._slippage_points = self.Param("SlippagePoints", 10)
        self._allow_long_entries = self.Param("AllowLongEntries", True)
        self._allow_short_entries = self.Param("AllowShortEntries", True)
        self._allow_long_exits = self.Param("AllowLongExits", True)
        self._allow_short_exits = self.Param("AllowShortExits", True)
        self._entry_mode = self.Param("Mode", self.MODE_TWIST)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._smoothing_method = self.Param("SmoothingMethod", self.SMOOTH_EMA)
        self._primary_length = self.Param("PrimaryLength", 20)
        self._first_smoothing_length = self.Param("FirstSmoothingLength", 5)
        self._second_smoothing_length = self.Param("SecondSmoothingLength", 3)
        self._signal_length = self.Param("SignalLength", 8)
        self._applied_price = self.Param("AppliedPrice", self.AP_CLOSE)
        self._signal_bar_shift = self.Param("SignalBarShift", 1)
        self._phase = self.Param("Phase", 15)
        self._price_average = None
        self._first_smoothing = None
        self._second_smoothing = None
        self._signal_smoothing = None
        self._histogram_buffer = []
        self._signal_buffer = []
        self._buffer_index = 0
        self._buffer_filled = 0
        self._point_value = 1.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def AllowLongEntries(self):
        return self._allow_long_entries.Value

    @property
    def AllowShortEntries(self):
        return self._allow_short_entries.Value

    @property
    def AllowLongExits(self):
        return self._allow_long_exits.Value

    @property
    def AllowShortExits(self):
        return self._allow_short_exits.Value

    @property
    def Mode(self):
        return self._entry_mode.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def SmoothingMethod(self):
        return self._smoothing_method.Value

    @property
    def PrimaryLength(self):
        return self._primary_length.Value

    @property
    def FirstSmoothingLength(self):
        return self._first_smoothing_length.Value

    @property
    def SecondSmoothingLength(self):
        return self._second_smoothing_length.Value

    @property
    def SignalLength(self):
        return self._signal_length.Value

    @property
    def AppliedPrice(self):
        return self._applied_price.Value

    @property
    def SignalBarShift(self):
        return self._signal_bar_shift.Value

    @property
    def Phase(self):
        return self._phase.Value

    def OnStarted(self, time):
        super(blau_ergodic_mdi_strategy, self).OnStarted(time)
        sec = self.Security
        self._point_value = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        self._price_average = self._create_ma(self.SmoothingMethod, self.PrimaryLength)
        self._first_smoothing = self._create_ma(self.SmoothingMethod, self.FirstSmoothingLength)
        self._second_smoothing = self._create_ma(self.SmoothingMethod, self.SecondSmoothingLength)
        self._signal_smoothing = self._create_ma(self.SmoothingMethod, self.SignalLength)
        self._initialize_buffers()
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._apply_risk_management(candle)
        price = self._select_price(candle)
        t = candle.CloseTime
        base_val = self._price_average.Process(DecimalIndicatorValue(self._price_average, price, t))
        if not base_val.IsFormed:
            return
        base_price = float(base_val)
        momentum = (float(price) - base_price) / self._point_value if self._point_value != 0 else 0.0
        first_val = self._first_smoothing.Process(DecimalIndicatorValue(self._first_smoothing, momentum, t))
        if not first_val.IsFormed:
            return
        second_val = self._second_smoothing.Process(DecimalIndicatorValue(self._second_smoothing, float(first_val), t))
        if not second_val.IsFormed:
            return
        histogram = float(second_val)
        signal_val = self._signal_smoothing.Process(DecimalIndicatorValue(self._signal_smoothing, histogram, t))
        if not signal_val.IsFormed:
            return
        signal = float(signal_val)
        self._add_to_buffer(histogram, signal)
        shift = self.SignalBarShift
        latest_hist = self._try_get_hist(shift)
        prev_hist = self._try_get_hist(shift + 1)
        if latest_hist is None or prev_hist is None:
            return
        pos = float(self.Position)
        buy_signal = False
        sell_signal = False
        mode = self.Mode
        if mode == self.MODE_BREAKDOWN:
            buy_signal = latest_hist > 0 and prev_hist <= 0
            sell_signal = latest_hist < 0 and prev_hist >= 0
        elif mode == self.MODE_TWIST:
            older_hist = self._try_get_hist(shift + 2)
            if older_hist is None:
                return
            buy_signal = prev_hist < latest_hist and older_hist > prev_hist
            sell_signal = prev_hist > latest_hist and older_hist < prev_hist
        elif mode == self.MODE_CLOUD_TWIST:
            latest_sig = self._try_get_signal(shift)
            prev_sig = self._try_get_signal(shift + 1)
            if latest_sig is None or prev_sig is None:
                return
            buy_signal = latest_hist > latest_sig and prev_hist <= prev_sig
            sell_signal = latest_hist < latest_sig and prev_hist >= prev_sig
        if buy_signal:
            self._execute_buy(pos, float(candle.ClosePrice))
        elif sell_signal:
            self._execute_sell(pos, float(candle.ClosePrice))

    def _execute_buy(self, current_pos, price):
        volume = 0.0
        if self.AllowShortExits and current_pos < 0:
            volume += abs(current_pos)
        if self.AllowLongEntries and (current_pos <= 0 or (self.AllowShortExits and current_pos < 0)):
            volume += float(self.Volume)
        if volume > 0:
            self.BuyMarket(volume)
            self._entry_price = price
            sl_dist = self.StopLossPoints * self._point_value if self.StopLossPoints > 0 else 0.0
            tp_dist = self.TakeProfitPoints * self._point_value if self.TakeProfitPoints > 0 else 0.0
            self._stop_price = price - sl_dist if sl_dist > 0 else None
            self._take_price = price + tp_dist if tp_dist > 0 else None

    def _execute_sell(self, current_pos, price):
        volume = 0.0
        if self.AllowLongExits and current_pos > 0:
            volume += abs(current_pos)
        if self.AllowShortEntries and (current_pos >= 0 or (self.AllowLongExits and current_pos > 0)):
            volume += float(self.Volume)
        if volume > 0:
            self.SellMarket(volume)
            self._entry_price = price
            sl_dist = self.StopLossPoints * self._point_value if self.StopLossPoints > 0 else 0.0
            tp_dist = self.TakeProfitPoints * self._point_value if self.TakeProfitPoints > 0 else 0.0
            self._stop_price = price + sl_dist if sl_dist > 0 else None
            self._take_price = price - tp_dist if tp_dist > 0 else None

    def _apply_risk_management(self, candle):
        pos = float(self.Position)
        if pos > 0:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(pos)
                self._reset_targets()
                return
            if self._take_price is not None and float(candle.HighPrice) >= self._take_price:
                self.SellMarket(pos)
                self._reset_targets()
        elif pos < 0:
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(abs(pos))
                self._reset_targets()
                return
            if self._take_price is not None and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket(abs(pos))
                self._reset_targets()

    def _reset_targets(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    def _initialize_buffers(self):
        size = max(3, self.SignalBarShift + 3)
        self._histogram_buffer = [0.0] * size
        self._signal_buffer = [0.0] * size
        self._buffer_index = 0
        self._buffer_filled = 0

    def _add_to_buffer(self, histogram, signal):
        if len(self._histogram_buffer) == 0:
            return
        self._histogram_buffer[self._buffer_index] = histogram
        self._signal_buffer[self._buffer_index] = signal
        self._buffer_index = (self._buffer_index + 1) % len(self._histogram_buffer)
        if self._buffer_filled < len(self._histogram_buffer):
            self._buffer_filled += 1

    def _try_get_hist(self, shift):
        return self._try_get_buffered(self._histogram_buffer, shift)

    def _try_get_signal(self, shift):
        return self._try_get_buffered(self._signal_buffer, shift)

    def _try_get_buffered(self, buf, shift):
        if shift < 0 or shift >= self._buffer_filled:
            return None
        idx = self._buffer_index - 1 - shift
        if idx < 0:
            idx += len(buf)
        return buf[idx]

    def _select_price(self, candle):
        ap = self.AppliedPrice
        if ap == self.AP_OPEN:
            return candle.OpenPrice
        elif ap == self.AP_HIGH:
            return candle.HighPrice
        elif ap == self.AP_LOW:
            return candle.LowPrice
        elif ap == self.AP_MEDIAN:
            return (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        elif ap == self.AP_TYPICAL:
            return (float(candle.ClosePrice) + float(candle.HighPrice) + float(candle.LowPrice)) / 3.0
        elif ap == self.AP_WEIGHTED:
            return (2.0 * float(candle.ClosePrice) + float(candle.HighPrice) + float(candle.LowPrice)) / 4.0
        elif ap == self.AP_SIMPLE:
            return (float(candle.OpenPrice) + float(candle.ClosePrice)) / 2.0
        elif ap == self.AP_QUARTER:
            return (float(candle.OpenPrice) + float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 4.0
        elif ap == self.AP_TREND0:
            if float(candle.ClosePrice) > float(candle.OpenPrice):
                return candle.HighPrice
            elif float(candle.ClosePrice) < float(candle.OpenPrice):
                return candle.LowPrice
            else:
                return candle.ClosePrice
        elif ap == self.AP_TREND1:
            if float(candle.ClosePrice) > float(candle.OpenPrice):
                return (float(candle.HighPrice) + float(candle.ClosePrice)) / 2.0
            elif float(candle.ClosePrice) < float(candle.OpenPrice):
                return (float(candle.LowPrice) + float(candle.ClosePrice)) / 2.0
            else:
                return candle.ClosePrice
        else:
            return candle.ClosePrice

    def _create_ma(self, method, length):
        if method == self.SMOOTH_SMA:
            ma = SimpleMovingAverage()
            ma.Length = length
            return ma
        elif method == self.SMOOTH_SMMA:
            ma = SmoothedMovingAverage()
            ma.Length = length
            return ma
        elif method == self.SMOOTH_WMA:
            ma = WeightedMovingAverage()
            ma.Length = length
            return ma
        else:
            ma = ExponentialMovingAverage()
            ma.Length = length
            return ma

    def OnReseted(self):
        super(blau_ergodic_mdi_strategy, self).OnReseted()
        self._histogram_buffer = []
        self._signal_buffer = []
        self._buffer_index = 0
        self._buffer_filled = 0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return blau_ergodic_mdi_strategy()
