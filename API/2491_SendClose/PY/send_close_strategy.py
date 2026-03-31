import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


UP = 0
DOWN = 1


class send_close_strategy(Strategy):
    def __init__(self):
        super(send_close_strategy, self).__init__()
        self._enable_sell_line = self.Param("EnableSellLine", True)
        self._enable_buy_line = self.Param("EnableBuyLine", True)
        self._enable_close_sell_line = self.Param("EnableCloseSellLine", True)
        self._enable_close_buy_line = self.Param("EnableCloseBuyLine", True)
        self._max_positions = self.Param("MaxPositions", 1)
        self._order_volume = self.Param("OrderVolume", 0.10)
        self._line_offset_steps = self.Param("LineOffsetSteps", 60)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(send_close_strategy, self).OnReseted()
        self._reset_state()

    def _reset_state(self):
        self._h = [0.0] * 5
        self._l = [0.0] * 5
        self._t = [None] * 5
        self._buffer_count = 0
        self._fractals = [None] * 6
        self._sell_line = None
        self._buy_line = None

    def OnStarted2(self, time):
        super(send_close_strategy, self).OnStarted2(time)
        self._reset_state()

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_buffers(candle)
        self._update_fractal_lines()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        offset = self._get_offset()
        should_close = False

        if self._enable_close_sell_line.Value and self._sell_line is not None:
            close_price = self._get_line_price(self._sell_line, candle.CloseTime) + offset
            if self._is_touched(close_price, candle):
                should_close = True

        if self._enable_close_buy_line.Value and self._buy_line is not None:
            close_price = self._get_line_price(self._buy_line, candle.CloseTime) - offset
            if self._is_touched(close_price, candle):
                should_close = True

        if should_close and self.Position != 0:
            if self.Position > 0:
                self.SellMarket()
            else:
                self.BuyMarket()
            return

        if self._enable_sell_line.Value and self._sell_line is not None:
            sell_price = self._get_line_price(self._sell_line, candle.CloseTime)
            if self._is_touched(sell_price, candle):
                if self.Position > 0:
                    self.SellMarket()
                elif self._can_increase_short():
                    self.SellMarket(float(self._order_volume.Value))

        if self._enable_buy_line.Value and self._buy_line is not None:
            buy_price = self._get_line_price(self._buy_line, candle.CloseTime)
            if self._is_touched(buy_price, candle):
                if self.Position < 0:
                    self.BuyMarket()
                elif self._can_increase_long():
                    self.BuyMarket(float(self._order_volume.Value))

    def _update_buffers(self, candle):
        h = self._h
        l = self._l
        t = self._t
        h[4] = h[3]; h[3] = h[2]; h[2] = h[1]; h[1] = h[0]; h[0] = float(candle.HighPrice)
        l[4] = l[3]; l[3] = l[2]; l[2] = l[1]; l[1] = l[0]; l[0] = float(candle.LowPrice)
        t[4] = t[3]; t[3] = t[2]; t[2] = t[1]; t[1] = t[0]; t[0] = candle.OpenTime

        if self._buffer_count < 5:
            self._buffer_count += 1
            return

        if self._is_up_fractal():
            self._register_fractal(UP, t[2], h[2])
        if self._is_down_fractal():
            self._register_fractal(DOWN, t[2], l[2])

    def _is_up_fractal(self):
        h = self._h
        return h[2] >= h[3] and h[2] > h[4] and h[2] >= h[1] and h[2] > h[0]

    def _is_down_fractal(self):
        l = self._l
        return l[2] <= l[3] and l[2] < l[4] and l[2] <= l[1] and l[2] < l[0]

    def _register_fractal(self, ftype, time, price):
        f = self._fractals
        if f[0] is not None and f[0][1] == time and f[0][0] == ftype:
            return
        f[5] = f[4]; f[4] = f[3]; f[3] = f[2]; f[2] = f[1]; f[1] = f[0]
        f[0] = (ftype, time, price)

    def _update_fractal_lines(self):
        sell_line = self._try_build_line(UP)
        if sell_line is not None:
            self._sell_line = sell_line
        buy_line = self._try_build_line(DOWN)
        if buy_line is not None:
            self._buy_line = buy_line

    def _try_build_line(self, target):
        latest = None
        middle = None
        oldest = None
        for item in self._fractals:
            if item is None:
                continue
            ftype, ftime, fprice = item
            if latest is None:
                if ftype == target:
                    latest = item
                continue
            if middle is None:
                if ftype != target:
                    middle = item
                continue
            if ftype == target:
                oldest = item
                break

        if latest is None or middle is None or oldest is None:
            return None

        _, lt, lp = latest
        _, ot, op = oldest
        if lt == ot:
            return None

        if lt < ot:
            return (ot, op, lt, lp)
        else:
            return (lt, lp, ot, op)

    def _get_line_price(self, line, time):
        recent_time, recent_price, older_time, older_price = line
        total_seconds = (recent_time - older_time).TotalSeconds
        if total_seconds == 0:
            return recent_price
        offset_seconds = (time - older_time).TotalSeconds
        return older_price + (recent_price - older_price) * (offset_seconds / total_seconds)

    def _is_touched(self, price, candle):
        return price <= float(candle.HighPrice) and price >= float(candle.LowPrice)

    def _can_increase_short(self):
        ov = float(self._order_volume.Value)
        mp = int(self._max_positions.Value)
        if ov <= 0 or mp <= 0:
            return False
        lots = abs(float(self.Position)) / ov if ov != 0 else 0
        return lots < mp

    def _can_increase_long(self):
        ov = float(self._order_volume.Value)
        mp = int(self._max_positions.Value)
        if ov <= 0 or mp <= 0:
            return False
        lots = abs(float(self.Position)) / ov if ov != 0 else 0
        return lots < mp

    def _get_offset(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        return step * int(self._line_offset_steps.Value)

    def CreateClone(self):
        return send_close_strategy()
