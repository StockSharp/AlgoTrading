import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


# Applied price constants
PRICE_CLOSE = 1
PRICE_OPEN = 2
PRICE_HIGH = 3
PRICE_LOW = 4
PRICE_MEDIAN = 5
PRICE_TYPICAL = 6
PRICE_WEIGHTED = 7
PRICE_AVERAGE_OC = 8
PRICE_AVERAGE_OHLC = 9
PRICE_TREND_FOLLOW1 = 10
PRICE_TREND_FOLLOW2 = 11
PRICE_DEMARK = 12

_FATL_COEFF = [
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


class color_jfatl_digit_tm_plus_strategy(Strategy):
    def __init__(self):
        super(color_jfatl_digit_tm_plus_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._jma_length = self.Param("JmaLength", 5)
        self._applied_price = self.Param("AppliedPrice", PRICE_CLOSE)
        self._rounding_digits = self.Param("RoundingDigits", 2)
        self._signal_bar = self.Param("SignalBar", 1)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)
        self._enable_buy_entries = self.Param("EnableBuyEntries", True)
        self._enable_sell_entries = self.Param("EnableSellEntries", True)
        self._enable_buy_exits = self.Param("EnableBuyExits", True)
        self._enable_sell_exits = self.Param("EnableSellExits", True)
        self._use_time_exit = self.Param("UseTimeExit", True)
        self._holding_minutes = self.Param("HoldingMinutes", 240)

        self._jma = None
        self._price_buffer = [0.0] * len(_FATL_COEFF)
        self._buf_idx = 0
        self._buf_count = 0
        self._prev_line = None
        self._prev_color = None
        self._color_history = []
        self._entry_price = None
        self._entry_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def JmaLength(self):
        return self._jma_length.Value

    @property
    def AppliedPrice(self):
        return self._applied_price.Value

    @property
    def RoundingDigits(self):
        return self._rounding_digits.Value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def EnableBuyEntries(self):
        return self._enable_buy_entries.Value

    @property
    def EnableSellEntries(self):
        return self._enable_sell_entries.Value

    @property
    def EnableBuyExits(self):
        return self._enable_buy_exits.Value

    @property
    def EnableSellExits(self):
        return self._enable_sell_exits.Value

    @property
    def UseTimeExit(self):
        return self._use_time_exit.Value

    @property
    def HoldingMinutes(self):
        return self._holding_minutes.Value

    def OnStarted2(self, time):
        super(color_jfatl_digit_tm_plus_strategy, self).OnStarted2(time)

        self._jma = JurikMovingAverage()
        self._jma.Length = max(1, self.JmaLength)

        self._price_buffer = [0.0] * len(_FATL_COEFF)
        self._buf_idx = 0
        self._buf_count = 0
        self._prev_line = None
        self._prev_color = None
        self._color_history = []
        self._entry_price = None
        self._entry_time = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _get_price(self, candle):
        ap = self.AppliedPrice
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        if ap == PRICE_OPEN:
            return o
        elif ap == PRICE_HIGH:
            return h
        elif ap == PRICE_LOW:
            return l
        elif ap == PRICE_MEDIAN:
            return (h + l) / 2.0
        elif ap == PRICE_TYPICAL:
            return (h + l + c) / 3.0
        elif ap == PRICE_WEIGHTED:
            return (2.0 * c + h + l) / 4.0
        elif ap == PRICE_AVERAGE_OC:
            return (o + c) / 2.0
        elif ap == PRICE_AVERAGE_OHLC:
            return (o + h + l + c) / 4.0
        elif ap == PRICE_TREND_FOLLOW1:
            if c > o:
                return h
            elif c < o:
                return l
            else:
                return c
        elif ap == PRICE_TREND_FOLLOW2:
            if c > o:
                return (h + c) / 2.0
            elif c < o:
                return (l + c) / 2.0
            else:
                return c
        elif ap == PRICE_DEMARK:
            res = h + l + c
            if c < o:
                res = (res + l) / 2.0
            elif c > o:
                res = (res + h) / 2.0
            else:
                res = (res + c) / 2.0
            return ((res - l) + (res - h)) / 2.0
        else:
            return c

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = self._get_price(candle)
        buf_len = len(self._price_buffer)
        self._price_buffer[self._buf_idx] = price
        self._buf_idx = (self._buf_idx + 1) % buf_len
        if self._buf_count < buf_len:
            self._buf_count += 1

        if self._buf_count < buf_len:
            return

        fatl = 0.0
        idx = self._buf_idx
        for i in range(len(_FATL_COEFF)):
            idx = (idx - 1 + buf_len) % buf_len
            fatl += _FATL_COEFF[i] * self._price_buffer[idx]

        t = candle.OpenTime
        jma_val = self._jma.Process(DecimalIndicatorValue(self._jma, fatl, t))
        if not self._jma.IsFormed:
            return

        smoothed = round(float(jma_val), self.RoundingDigits)

        color = 1
        if self._prev_line is not None:
            diff = smoothed - self._prev_line
            if diff > 0:
                color = 2
            elif diff < 0:
                color = 0
            elif self._prev_color is not None:
                color = self._prev_color

        self._prev_line = smoothed
        self._prev_color = color

        self._color_history.append(color)
        max_hist = max(self.SignalBar + 2, 2)
        while len(self._color_history) > max_hist:
            self._color_history.pop(0)

        offset = max(self.SignalBar, 1)
        if len(self._color_history) < offset + 1:
            return

        current_color = self._color_history[-offset]
        previous_color = self._color_history[-(offset + 1)]

        buy_open = False
        sell_open = False
        buy_close = False
        sell_close = False

        if current_color == 2:
            if self.EnableBuyEntries and previous_color < 2:
                buy_open = True
            if self.EnableSellExits:
                sell_close = True
        elif current_color == 0:
            if self.EnableSellEntries and previous_color > 0:
                sell_open = True
            if self.EnableBuyExits:
                buy_close = True

        self._handle_time_exit(candle)
        if self._handle_stops(candle):
            return

        if buy_close and self.Position > 0:
            self.SellMarket()
            self._entry_price = None
            self._entry_time = None

        if sell_close and self.Position < 0:
            self.BuyMarket()
            self._entry_price = None
            self._entry_time = None

        if buy_open and self.Position == 0:
            self.BuyMarket()
            self._entry_price = float(candle.ClosePrice)
            self._entry_time = candle.CloseTime
        elif sell_open and self.Position == 0:
            self.SellMarket()
            self._entry_price = float(candle.ClosePrice)
            self._entry_time = candle.CloseTime

        if self.Position == 0:
            self._entry_price = None
            self._entry_time = None

    def _handle_time_exit(self, candle):
        if not self.UseTimeExit or self.Position == 0 or self._entry_time is None:
            return
        if self.HoldingMinutes <= 0:
            return

        elapsed = candle.CloseTime - self._entry_time
        if elapsed < TimeSpan.FromMinutes(self.HoldingMinutes):
            return

        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

        self._entry_price = None
        self._entry_time = None

    def _handle_stops(self, candle):
        if self.Position == 0 or self._entry_price is None:
            return False

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        stop_offset = self.StopLossPoints * step if self.StopLossPoints > 0 else 0.0
        take_offset = self.TakeProfitPoints * step if self.TakeProfitPoints > 0 else 0.0

        if self.Position > 0:
            if stop_offset > 0 and float(candle.LowPrice) <= self._entry_price - stop_offset:
                self.SellMarket()
                self._entry_price = None
                self._entry_time = None
                return True
            if take_offset > 0 and float(candle.HighPrice) >= self._entry_price + take_offset:
                self.SellMarket()
                self._entry_price = None
                self._entry_time = None
                return True
        elif self.Position < 0:
            if stop_offset > 0 and float(candle.HighPrice) >= self._entry_price + stop_offset:
                self.BuyMarket()
                self._entry_price = None
                self._entry_time = None
                return True
            if take_offset > 0 and float(candle.LowPrice) <= self._entry_price - take_offset:
                self.BuyMarket()
                self._entry_price = None
                self._entry_time = None
                return True

        return False

    def OnReseted(self):
        super(color_jfatl_digit_tm_plus_strategy, self).OnReseted()
        self._color_history = []
        self._entry_price = None
        self._entry_time = None
        self._price_buffer = [0.0] * len(_FATL_COEFF)
        self._buf_idx = 0
        self._buf_count = 0
        self._prev_line = None
        self._prev_color = None

    def CreateClone(self):
        return color_jfatl_digit_tm_plus_strategy()
