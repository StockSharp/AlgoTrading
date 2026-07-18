import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

import math
from System import TimeSpan, Decimal, DateTime
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class color_jfatl_digit_tm_strategy(Strategy):
    """Color JFATL Digit indicator strategy with trading window and SL/TP."""

    FATL_COEFFICIENTS = [
        0.4360409450, 0.3658689069, 0.2460452079, 0.1104506886,
        -0.0054034585, -0.0760367731, -0.0933058722, -0.0670110374,
        -0.0190795053, 0.0259609206, 0.0502044896, 0.0477818607,
        0.0249252327, -0.0047706151, -0.0272432537, -0.0338917071,
        -0.0244141482, -0.0055774838, 0.0128149838, 0.0226522218,
        0.0208778257, 0.0100299086, -0.0036771622, -0.0136744850,
        -0.0160483392, -0.0108597376, -0.0016060704, 0.0069480557,
        0.0110573605, 0.0095711419, 0.0040444064, -0.0023824623,
        -0.0067093714, -0.0072003400, -0.0047717710, 0.0005541115,
        0.0007860160, 0.0130129076, 0.0040364019,
    ]

    def __init__(self):
        super(color_jfatl_digit_tm_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", Decimal(1)) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Trade volume per position", "Risk")
        self._enable_time_filter = self.Param("EnableTimeFilter", False) \
            .SetDisplay("Enable Time Filter", "Restrict trading to session hours", "Session")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Session start hour", "Session")
        self._start_minute = self.Param("StartMinute", 0) \
            .SetDisplay("Start Minute", "Session start minute", "Session")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("End Hour", "Session end hour", "Session")
        self._end_minute = self.Param("EndMinute", 59) \
            .SetDisplay("End Minute", "Session end minute", "Session")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Take Profit (points)", "Take profit in points", "Risk")
        self._buy_open = self.Param("BuyOpenEnabled", True) \
            .SetDisplay("Enable Buy Open", "Allow opening long positions", "Signals")
        self._sell_open = self.Param("SellOpenEnabled", True) \
            .SetDisplay("Enable Sell Open", "Allow opening short positions", "Signals")
        self._buy_close = self.Param("BuyCloseEnabled", True) \
            .SetDisplay("Enable Buy Close", "Allow closing long positions", "Signals")
        self._sell_close = self.Param("SellCloseEnabled", True) \
            .SetDisplay("Enable Sell Close", "Allow closing short positions", "Signals")
        self._candle_type = self.Param("SignalCandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Signal Candle Type", "Timeframe used for indicator", "Indicator")
        self._jma_length = self.Param("JmaLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("JMA Length", "Period for Jurik moving average", "Indicator")
        self._jma_phase = self.Param("JmaPhase", -100) \
            .SetDisplay("JMA Phase", "Phase shift for Jurik moving average", "Indicator")
        self._digit_rounding = self.Param("DigitRounding", 0) \
            .SetDisplay("Digit Rounding", "Rounding precision multiplier", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Bar", "Shift for analyzing colors", "Signals")

        self._price_buffer = []
        self._color_history = []
        self._previous_line = None
        self._ema_value = None
        self._ema_count = 0
        self._next_buy_time = DateTime.MinValue
        self._next_sell_time = DateTime.MinValue

    @property
    def OrderVolume(self):
        return self._order_volume.Value
    @property
    def EnableTimeFilter(self):
        return self._enable_time_filter.Value
    @property
    def StartHour(self):
        return self._start_hour.Value
    @property
    def StartMinute(self):
        return self._start_minute.Value
    @property
    def EndHour(self):
        return self._end_hour.Value
    @property
    def EndMinute(self):
        return self._end_minute.Value
    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value
    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value
    @property
    def BuyOpenEnabled(self):
        return self._buy_open.Value
    @property
    def SellOpenEnabled(self):
        return self._sell_open.Value
    @property
    def BuyCloseEnabled(self):
        return self._buy_close.Value
    @property
    def SellCloseEnabled(self):
        return self._sell_close.Value
    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def JmaLength(self):
        return self._jma_length.Value
    @property
    def JmaPhase(self):
        return self._jma_phase.Value
    @property
    def DigitRounding(self):
        return self._digit_rounding.Value
    @property
    def SignalBar(self):
        return self._signal_bar.Value

    def OnStarted2(self, time):
        super(color_jfatl_digit_tm_strategy, self).OnStarted2(time)

        self.Volume = self.OrderVolume

        self._ema_value = None
        self._ema_count = 0
        self._ema_length = self.JmaLength
        self._ema_multiplier = 2.0 / (self._ema_length + 1)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        price_step = Decimal(0)
        sec = self.Security
        if sec is not None and sec.PriceStep is not None and sec.PriceStep > Decimal(0):
            price_step = sec.PriceStep

        tp_unit = None
        sl_unit = None
        if self.TakeProfitPoints > 0 and price_step > Decimal(0):
            tp_unit = Unit(Decimal(self.TakeProfitPoints) * price_step, UnitTypes.Absolute)
        if self.StopLossPoints > 0 and price_step > Decimal(0):
            sl_unit = Unit(Decimal(self.StopLossPoints) * price_step, UnitTypes.Absolute)

        self.StartProtection(takeProfit=tp_unit, stopLoss=sl_unit)

    def _process_ema(self, value):
        """Manual EMA matching ExponentialMovingAverage behavior."""
        self._ema_count += 1
        if self._ema_value is None:
            self._ema_value = value
        else:
            self._ema_value = value * self._ema_multiplier + self._ema_value * (1.0 - self._ema_multiplier)
        return self._ema_value

    def _ema_is_formed(self):
        return self._ema_count >= self._ema_length

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        self._price_buffer.append(price)
        coeffs_len = len(self.FATL_COEFFICIENTS)
        if len(self._price_buffer) > coeffs_len:
            self._price_buffer.pop(0)

        if len(self._price_buffer) < coeffs_len:
            return

        fatl = 0.0
        for i in range(coeffs_len):
            fatl += self.FATL_COEFFICIENTS[i] * self._price_buffer[len(self._price_buffer) - 1 - i]

        jma_val = self._process_ema(fatl)
        if not self._ema_is_formed():
            return

        rounding_step = self._get_rounding_step()
        rounded_line = self._round_to_step(jma_val, rounding_step)

        color = 1
        if self._previous_line is not None:
            diff = rounded_line - self._previous_line
            if diff > 0:
                color = 2
            elif diff < 0:
                color = 0
            elif len(self._color_history) > 0:
                color = self._color_history[0]

        self._previous_line = rounded_line
        self._color_history.insert(0, color)
        if len(self._color_history) > 100:
            self._color_history.pop()

        if len(self._color_history) <= self.SignalBar:
            return

        current_color = self._color_history[self.SignalBar - 1]
        previous_color = self._color_history[self.SignalBar]
        now = candle.CloseTime

        in_session = (not self.EnableTimeFilter) or self._is_within_trading_window(now)
        if self.EnableTimeFilter and not in_session:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            return

        buy_open_signal = self.BuyOpenEnabled and current_color == 2 and previous_color != 2
        sell_close_signal = self.SellCloseEnabled and current_color == 2
        sell_open_signal = self.SellOpenEnabled and current_color == 0 and previous_color != 0
        buy_close_signal = self.BuyCloseEnabled and current_color == 0

        if buy_close_signal and self.Position > 0:
            self.SellMarket()

        if sell_close_signal and self.Position < 0:
            self.BuyMarket()

        if buy_open_signal and self.Position == 0 and now >= self._next_buy_time:
            self.BuyMarket()
            self._next_buy_time = now

        if sell_open_signal and self.Position == 0 and now >= self._next_sell_time:
            self.SellMarket()
            self._next_sell_time = now

    def _get_rounding_step(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None or float(sec.PriceStep) <= 0:
            return 0.0
        step = float(sec.PriceStep)
        multiplier = math.pow(10, self.DigitRounding)
        return step * multiplier

    def _round_to_step(self, value, step):
        if step <= 0:
            return value
        return round(value / step) * step

    def _is_within_trading_window(self, time):
        h = time.Hour
        m = time.Minute
        sh = self.StartHour
        sm = self.StartMinute
        eh = self.EndHour
        em = self.EndMinute

        if sh < eh:
            if h == sh and m >= sm:
                return True
            if h > sh and h < eh:
                return True
            if h > sh and h == eh and m < em:
                return True
            return False
        elif sh == eh:
            return h == sh and m >= sm and m < em
        else:
            if h > sh or (h == sh and m >= sm):
                return True
            if h < eh:
                return True
            if h == eh and m < em:
                return True
            return False

    def OnReseted(self):
        super(color_jfatl_digit_tm_strategy, self).OnReseted()
        self._price_buffer = []
        self._color_history = []
        self._previous_line = None
        self._ema_value = None
        self._ema_count = 0
        self._next_buy_time = DateTime.MinValue
        self._next_sell_time = DateTime.MinValue

    def CreateClone(self):
        return color_jfatl_digit_tm_strategy()
