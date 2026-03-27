import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math, Decimal


class blau_ergodic_strategy(Strategy):
    def __init__(self):
        super(blau_ergodic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8)))
        self._mode = self.Param("Mode", 1)
        self._momentum_length = self.Param("MomentumLength", 2)
        self._first_smoothing_length = self.Param("FirstSmoothingLength", 20)
        self._second_smoothing_length = self.Param("SecondSmoothingLength", 5)
        self._third_smoothing_length = self.Param("ThirdSmoothingLength", 3)
        self._signal_smoothing_length = self.Param("SignalSmoothingLength", 3)
        self._signal_bar = self.Param("SignalBar", 1)
        self._allow_buy_entry = self.Param("AllowBuyEntry", True)
        self._allow_sell_entry = self.Param("AllowSellEntry", True)
        self._allow_buy_exit = self.Param("AllowBuyExit", True)
        self._allow_sell_exit = self.Param("AllowSellExit", True)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)

        self._mom_ema1 = None
        self._mom_ema2 = None
        self._mom_ema3 = None
        self._abs_mom_ema1 = None
        self._abs_mom_ema2 = None
        self._abs_mom_ema3 = None
        self._signal_ema = None
        self._price_history = []
        self._main_history = []
        self._signal_history = []
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(blau_ergodic_strategy, self).OnStarted(time)

        self._mom_ema1 = ExponentialMovingAverage()
        self._mom_ema1.Length = self._first_smoothing_length.Value
        self._mom_ema2 = ExponentialMovingAverage()
        self._mom_ema2.Length = self._second_smoothing_length.Value
        self._mom_ema3 = ExponentialMovingAverage()
        self._mom_ema3.Length = self._third_smoothing_length.Value

        self._abs_mom_ema1 = ExponentialMovingAverage()
        self._abs_mom_ema1.Length = self._first_smoothing_length.Value
        self._abs_mom_ema2 = ExponentialMovingAverage()
        self._abs_mom_ema2.Length = self._second_smoothing_length.Value
        self._abs_mom_ema3 = ExponentialMovingAverage()
        self._abs_mom_ema3.Length = self._third_smoothing_length.Value

        self._signal_ema = ExponentialMovingAverage()
        self._signal_ema.Length = self._signal_smoothing_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        self._price_history.append(price)
        max_hist = self._momentum_length.Value + self._signal_bar.Value + 10
        while len(self._price_history) > max_hist:
            self._price_history.pop(0)

        if self._momentum_length.Value <= 0:
            return

        back_shift = self._momentum_length.Value - 1
        if len(self._price_history) <= back_shift:
            return

        ref_index = len(self._price_history) - 1 - back_shift
        ref_price = self._price_history[ref_index]
        momentum = price - ref_price
        abs_momentum = abs(momentum)

        t = candle.ServerTime

        d1 = DecimalIndicatorValue(self._mom_ema1, Decimal(float(momentum)), t)
        d1.IsFinal = True
        mom1 = self._mom_ema1.Process(d1)
        d2 = DecimalIndicatorValue(self._abs_mom_ema1, Decimal(float(abs_momentum)), t)
        d2.IsFinal = True
        abs1 = self._abs_mom_ema1.Process(d2)
        if mom1.IsEmpty or abs1.IsEmpty:
            return

        d3 = DecimalIndicatorValue(self._mom_ema2, Decimal(float(mom1.Value)), t)
        d3.IsFinal = True
        mom2 = self._mom_ema2.Process(d3)
        d4 = DecimalIndicatorValue(self._abs_mom_ema2, Decimal(float(abs1.Value)), t)
        d4.IsFinal = True
        abs2 = self._abs_mom_ema2.Process(d4)
        if mom2.IsEmpty or abs2.IsEmpty:
            return

        d5 = DecimalIndicatorValue(self._mom_ema3, Decimal(float(mom2.Value)), t)
        d5.IsFinal = True
        mom3 = self._mom_ema3.Process(d5)
        d6 = DecimalIndicatorValue(self._abs_mom_ema3, Decimal(float(abs2.Value)), t)
        d6.IsFinal = True
        abs3 = self._abs_mom_ema3.Process(d6)
        if mom3.IsEmpty or abs3.IsEmpty:
            return

        smoothed_mom = float(mom3.Value)
        smoothed_abs = float(abs3.Value)

        main = 0.0 if smoothed_abs == 0.0 else 100.0 * smoothed_mom / smoothed_abs

        d7 = DecimalIndicatorValue(self._signal_ema, Decimal(float(main)), t)
        d7.IsFinal = True
        signal_result = self._signal_ema.Process(d7)
        signal = float(signal_result.Value) if not signal_result.IsEmpty else None

        self._main_history.append(main)
        self._signal_history.append(signal)

        max_size = max(self._signal_bar.Value + 5, 10)
        while len(self._main_history) > max_size:
            self._main_history.pop(0)
        while len(self._signal_history) > max_size:
            self._signal_history.pop(0)

        self._evaluate_signals(candle)

    def _evaluate_signals(self, candle):
        current_index = self._signal_bar.Value - 1
        if current_index < 0:
            return

        current_main = self._try_get_main(current_index)
        if current_main is None:
            return

        buy_open = False
        sell_open = False
        buy_close = False
        sell_close = False
        mode = self._mode.Value

        if mode == 0:
            previous_main = self._try_get_main(current_index + 1)
            if previous_main is None:
                return
            if self._allow_sell_exit.Value and current_main > 0:
                sell_close = True
            if self._allow_buy_exit.Value and current_main < 0:
                buy_close = True
            if self._allow_buy_entry.Value and previous_main <= 0 and current_main > 0:
                buy_open = True
            if self._allow_sell_entry.Value and previous_main >= 0 and current_main < 0:
                sell_open = True

        elif mode == 1:
            previous_main = self._try_get_main(current_index + 1)
            older_main = self._try_get_main(current_index + 2)
            if previous_main is None or older_main is None:
                return
            if self._allow_sell_exit.Value and previous_main < current_main:
                sell_close = True
            if self._allow_buy_exit.Value and previous_main > current_main:
                buy_close = True
            if self._allow_buy_entry.Value and older_main > previous_main and previous_main < current_main:
                buy_open = True
            if self._allow_sell_entry.Value and older_main < previous_main and previous_main > current_main:
                sell_open = True

        elif mode == 2:
            previous_main = self._try_get_main(current_index + 1)
            current_signal = self._try_get_signal(current_index)
            previous_signal = self._try_get_signal(current_index + 1)
            if previous_main is None or current_signal is None or previous_signal is None:
                return
            if self._allow_sell_exit.Value and current_main > current_signal:
                sell_close = True
            if self._allow_buy_exit.Value and current_main < current_signal:
                buy_close = True
            if self._allow_buy_entry.Value and previous_main <= previous_signal and current_main > current_signal:
                buy_open = True
            if self._allow_sell_entry.Value and previous_main >= previous_signal and current_main < current_signal:
                sell_open = True

        close_long_stops, close_short_stops = self._evaluate_stops(candle)

        if close_long_stops:
            buy_close = True
        if close_short_stops:
            sell_close = True

        self._execute_orders(candle, buy_open, sell_open, buy_close, sell_close, close_long_stops, close_short_stops)

    def _evaluate_stops(self, candle):
        close_long = False
        close_short = False
        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        sl_dist = self._stop_loss_points.Value * price_step if price_step > 0 and self._stop_loss_points.Value > 0 else 0.0
        tp_dist = self._take_profit_points.Value * price_step if price_step > 0 and self._take_profit_points.Value > 0 else 0.0

        if self.Position > 0:
            if sl_dist > 0 and float(candle.LowPrice) <= self._entry_price - sl_dist:
                close_long = True
            if tp_dist > 0 and float(candle.HighPrice) >= self._entry_price + tp_dist:
                close_long = True
        elif self.Position < 0:
            if sl_dist > 0 and float(candle.HighPrice) >= self._entry_price + sl_dist:
                close_short = True
            if tp_dist > 0 and float(candle.LowPrice) <= self._entry_price - tp_dist:
                close_short = True

        return close_long, close_short

    def _execute_orders(self, candle, buy_open, sell_open, buy_close, sell_close, force_buy_close, force_sell_close):
        if ((buy_close and self._allow_buy_exit.Value) or force_buy_close) and self.Position > 0:
            self.SellMarket(self.Position)
            self._entry_price = 0.0

        if ((sell_close and self._allow_sell_exit.Value) or force_sell_close) and self.Position < 0:
            self.BuyMarket(abs(self.Position))
            self._entry_price = 0.0

        if buy_open and self._allow_buy_entry.Value and self.Position <= 0:
            volume = float(self.Volume) + abs(self.Position)
            self.BuyMarket(volume)
            self._entry_price = float(candle.ClosePrice)

        if sell_open and self._allow_sell_entry.Value and self.Position >= 0:
            volume = float(self.Volume) + abs(self.Position)
            self.SellMarket(volume)
            self._entry_price = float(candle.ClosePrice)

    def _try_get_main(self, shift):
        index = len(self._main_history) - 1 - shift
        if index < 0 or index >= len(self._main_history):
            return None
        return self._main_history[index]

    def _try_get_signal(self, shift):
        index = len(self._signal_history) - 1 - shift
        if index < 0 or index >= len(self._signal_history):
            return None
        return self._signal_history[index]

    def OnReseted(self):
        super(blau_ergodic_strategy, self).OnReseted()
        self._mom_ema1 = None
        self._mom_ema2 = None
        self._mom_ema3 = None
        self._abs_mom_ema1 = None
        self._abs_mom_ema2 = None
        self._abs_mom_ema3 = None
        self._signal_ema = None
        self._price_history = []
        self._main_history = []
        self._signal_history = []
        self._entry_price = 0.0

    def CreateClone(self):
        return blau_ergodic_strategy()
