import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class color_pema_envelopes_digit_system_strategy(Strategy):
    """PEMA Envelopes with color-coded breakout signals and SL/TP protection."""

    def __init__(self):
        super(color_pema_envelopes_digit_system_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for calculations", "General")
        self._ema_length = self.Param("EmaLength", 50.01) \
            .SetGreaterThanZero() \
            .SetDisplay("PEMA Length", "Length of each EMA stage in PEMA", "Indicator")
        self._deviation_pct = self.Param("DeviationPercent", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Envelope Deviation", "Percentage width of envelopes", "Indicator")
        self._shift = self.Param("Shift", 1) \
            .SetDisplay("Shift", "Bars used to offset envelope comparison", "Indicator")
        self._price_shift = self.Param("PriceShift", 0.0) \
            .SetDisplay("Price Shift", "Additional absolute shift applied to envelopes", "Indicator")
        self._digit = self.Param("Digit", 2) \
            .SetDisplay("Rounding Digits", "Extra precision digits for rounding", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "How many completed bars back to check colors", "Logic")
        self._allow_buy_open = self.Param("AllowBuyOpen", True) \
            .SetDisplay("Allow Buy Open", "Enable new long entries", "Logic")
        self._allow_sell_open = self.Param("AllowSellOpen", True) \
            .SetDisplay("Allow Sell Open", "Enable new short entries", "Logic")
        self._allow_buy_close = self.Param("AllowBuyClose", True) \
            .SetDisplay("Allow Buy Close", "Allow closing long positions on opposite signal", "Logic")
        self._allow_sell_close = self.Param("AllowSellClose", True) \
            .SetDisplay("Allow Sell Close", "Allow closing short positions on opposite signal", "Logic")
        self._stop_loss_points = self.Param("StopLossPoints", 10.0) \
            .SetDisplay("Stop Loss Points", "Distance for protective stop", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 20.0) \
            .SetDisplay("Take Profit Points", "Distance for profit target", "Risk")

        # PEMA state
        self._ema_values = [0.0] * 8
        self._has_history = False
        self._pema_count = 0

        # History buffers
        self._upper_history = []
        self._lower_history = []
        self._color_history = []

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def EmaLength(self):
        return self._ema_length.Value
    @property
    def DeviationPercent(self):
        return self._deviation_pct.Value
    @property
    def Shift(self):
        return self._shift.Value
    @property
    def PriceShift(self):
        return self._price_shift.Value
    @property
    def Digit(self):
        return self._digit.Value
    @property
    def SignalBar(self):
        return self._signal_bar.Value
    @property
    def AllowBuyOpen(self):
        return self._allow_buy_open.Value
    @property
    def AllowSellOpen(self):
        return self._allow_sell_open.Value
    @property
    def AllowBuyClose(self):
        return self._allow_buy_close.Value
    @property
    def AllowSellClose(self):
        return self._allow_sell_close.Value
    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value
    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    def OnStarted(self, time):
        super(color_pema_envelopes_digit_system_strategy, self).OnStarted(time)

        self._ema_values = [0.0] * 8
        self._has_history = False
        self._pema_count = 0
        self._upper_history = []
        self._lower_history = []
        self._color_history = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        sl = float(self.StopLossPoints) * step
        tp = float(self.TakeProfitPoints) * step
        if sl > 0 or tp > 0:
            self.StartProtection(
                takeProfit=Unit(tp, UnitTypes.Absolute) if tp > 0 else None,
                stopLoss=Unit(sl, UnitTypes.Absolute) if sl > 0 else None
            )

    def _calc_pema(self, price):
        length = float(self.EmaLength)
        if length <= 0:
            length = 1.0
        alpha = 2.0 / (length + 1.0)
        one_minus = 1.0 - alpha

        current = price
        for i in range(8):
            prev = self._ema_values[i] if self._has_history else current
            ema = alpha * current + one_minus * prev
            self._ema_values[i] = ema
            current = ema

        self._has_history = True
        self._pema_count += 1

        pema = (8.0 * self._ema_values[0]
                - 28.0 * self._ema_values[1]
                + 56.0 * self._ema_values[2]
                - 70.0 * self._ema_values[3]
                + 56.0 * self._ema_values[4]
                - 28.0 * self._ema_values[5]
                + 8.0 * self._ema_values[6]
                - self._ema_values[7])

        digits = max(0, int(self.Digit))
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        factor = step * (10.0 ** digits)
        if factor > 0:
            pema = round(pema / factor) * factor

        is_formed = self._pema_count > 8
        return pema, is_formed

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        pema, is_formed = self._calc_pema(price)

        dev = float(self.DeviationPercent)
        ps = float(self.PriceShift)
        upper_current = (1.0 + dev / 100.0) * pema + ps
        lower_current = (1.0 - dev / 100.0) * pema + ps

        shift = max(0, int(self.Shift))

        if shift == 0:
            upper_for_color = upper_current
            lower_for_color = lower_current
        else:
            upper_for_color = self._upper_history[0] if len(self._upper_history) >= shift else None
            lower_for_color = self._lower_history[0] if len(self._lower_history) >= shift else None

        current_color = self._calc_color(candle, upper_for_color, lower_for_color)

        if not is_formed:
            self._update_histories(current_color, upper_current, lower_current, shift)
            return

        sig_bar = int(self.SignalBar)
        has_recent, recent_color = self._try_get_color(sig_bar)
        has_older, older_color = self._try_get_color(sig_bar + 1)

        buy_open_signal = False
        sell_open_signal = False
        buy_close_signal = False
        sell_close_signal = False

        if has_older:
            if older_color > 2:
                if self.AllowBuyOpen and has_recent and recent_color < 3:
                    buy_open_signal = True
                if self.AllowSellClose:
                    sell_close_signal = True
            elif older_color < 2:
                if self.AllowSellOpen and has_recent and recent_color > 1:
                    sell_open_signal = True
                if self.AllowBuyClose:
                    buy_close_signal = True

        if buy_close_signal and self.Position > 0:
            self.SellMarket()
        if sell_close_signal and self.Position < 0:
            self.BuyMarket()

        if buy_open_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_open_signal and self.Position >= 0:
            self.SellMarket()

        self._update_histories(current_color, upper_current, lower_current, shift)

    def _calc_color(self, candle, upper, lower):
        color = 2
        close = float(candle.ClosePrice)
        o = float(candle.OpenPrice)

        if upper is not None:
            if close > upper:
                color = 4 if o <= close else 3

        if lower is not None:
            if close < lower:
                color = 0 if o > close else 1

        return color

    def _try_get_color(self, bars_ago):
        if bars_ago <= 0 or len(self._color_history) < bars_ago:
            return False, 0
        return True, self._color_history[-bars_ago]

    def _update_histories(self, current_color, upper_current, lower_current, shift):
        self._color_history.append(current_color)
        max_colors = max(3, max(shift, int(self.SignalBar)) + 3)
        while len(self._color_history) > max_colors:
            self._color_history.pop(0)

        if shift > 0:
            self._upper_history.append(upper_current)
            while len(self._upper_history) > shift:
                self._upper_history.pop(0)
            self._lower_history.append(lower_current)
            while len(self._lower_history) > shift:
                self._lower_history.pop(0)

    def OnReseted(self):
        super(color_pema_envelopes_digit_system_strategy, self).OnReseted()
        self._ema_values = [0.0] * 8
        self._has_history = False
        self._pema_count = 0
        self._upper_history = []
        self._lower_history = []
        self._color_history = []

    def CreateClone(self):
        return color_pema_envelopes_digit_system_strategy()
