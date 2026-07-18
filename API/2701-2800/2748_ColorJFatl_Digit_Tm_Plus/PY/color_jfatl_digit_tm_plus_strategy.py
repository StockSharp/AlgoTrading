import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal

from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

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

_FATL_LEN = len(_FATL_COEFF)

class color_jfatl_digit_tm_plus_strategy(Strategy):
    def __init__(self):
        super(color_jfatl_digit_tm_plus_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", Decimal(1))
        self._stop_loss_points = self.Param("StopLossPoints", 0)
        self._take_profit_points = self.Param("TakeProfitPoints", 0)
        self._enable_buy_entries = self.Param("EnableBuyEntries", True)
        self._enable_sell_entries = self.Param("EnableSellEntries", True)
        self._enable_buy_exits = self.Param("EnableBuyExits", True)
        self._enable_sell_exits = self.Param("EnableSellExits", True)
        self._use_time_exit = self.Param("UseTimeExit", False)
        self._holding_minutes = self.Param("HoldingMinutes", 240)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._jma_length = self.Param("JmaLength", 14)
        self._applied_price = self.Param("AppliedPrice", PRICE_CLOSE)
        self._digit_rounding = self.Param("DigitRounding", 0)
        self._signal_bar = self.Param("SignalBar", 1)

        self._jma = None
        self._price_buffer = []
        self._color_history = []
        self._previous_line = None
        self._entry_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(color_jfatl_digit_tm_plus_strategy, self).OnStarted2(time)

        self.Volume = self._trade_volume.Value
        self._jma = ExponentialMovingAverage()
        self._jma.Length = self._jma_length.Value

        self._price_buffer = []
        self._color_history = []
        self._previous_line = None
        self._entry_time = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        sec = self.Security
        price_step = sec.PriceStep if sec is not None and sec.PriceStep is not None else Decimal(0)

        take_profit_unit = None
        stop_loss_unit = None

        tp = self._take_profit_points.Value
        sl = self._stop_loss_points.Value

        if tp > 0 and price_step > Decimal(0):
            take_profit_unit = Unit(Decimal(tp) * price_step, UnitTypes.Absolute)

        if sl > 0 and price_step > Decimal(0):
            stop_loss_unit = Unit(Decimal(sl) * price_step, UnitTypes.Absolute)

        self.StartProtection(take_profit_unit, stop_loss_unit)

    def _get_applied_price(self, candle):
        ap = self._applied_price.Value
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        low = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        if ap == PRICE_CLOSE:
            return c
        elif ap == PRICE_OPEN:
            return o
        elif ap == PRICE_HIGH:
            return h
        elif ap == PRICE_LOW:
            return low
        elif ap == PRICE_MEDIAN:
            return (h + low) / 2.0
        elif ap == PRICE_TYPICAL:
            return (c + h + low) / 3.0
        elif ap == PRICE_WEIGHTED:
            return (2.0 * c + h + low) / 4.0
        elif ap == PRICE_AVERAGE_OC:
            return (o + c) / 2.0
        elif ap == PRICE_AVERAGE_OHLC:
            return (o + c + h + low) / 4.0
        elif ap == PRICE_TREND_FOLLOW1:
            if c > o:
                return h
            elif c < o:
                return low
            else:
                return c
        elif ap == PRICE_TREND_FOLLOW2:
            if c > o:
                return (h + c) / 2.0
            elif c < o:
                return (low + c) / 2.0
            else:
                return c
        elif ap == PRICE_DEMARK:
            return self._get_demark_price(o, h, low, c)
        else:
            return c

    def _get_demark_price(self, o, h, low, c):
        res = h + low + c
        if c < o:
            res = (res + low) / 2.0
        elif c > o:
            res = (res + h) / 2.0
        else:
            res = (res + c) / 2.0
        return ((res - low) + (res - h)) / 2.0

    def _get_rounding_step(self):
        sec = self.Security
        step = sec.PriceStep if sec is not None and sec.PriceStep is not None else Decimal(0)
        if step <= Decimal(0):
            return 0.0
        multiplier = Math.Pow(10.0, float(self._digit_rounding.Value))
        return float(step) * multiplier

    def _round_to_step(self, value, step):
        if step <= 0.0:
            return value
        return round(value / step) * step

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = self._get_applied_price(candle)
        self._price_buffer.append(price)
        if len(self._price_buffer) > _FATL_LEN:
            self._price_buffer.pop(0)

        if len(self._price_buffer) < _FATL_LEN:
            return

        fatl = 0.0
        for i in range(_FATL_LEN):
            fatl += _FATL_COEFF[i] * self._price_buffer[len(self._price_buffer) - 1 - i]

        jma_result = process_float(self._jma, Decimal(fatl), candle.OpenTime, True)
        if not self._jma.IsFormed:
            return

        jma_val = float(jma_result.Value)
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

        signal_bar = self._signal_bar.Value
        if len(self._color_history) <= signal_bar:
            return

        current_color = self._color_history[signal_bar - 1]
        previous_color = self._color_history[signal_bar]

        # Time-based exit
        if self._use_time_exit.Value and self.Position != 0 and self._entry_time is not None and self._holding_minutes.Value > 0:
            elapsed = candle.CloseTime.Subtract(self._entry_time)
            if elapsed >= TimeSpan.FromMinutes(self._holding_minutes.Value):
                if self.Position > 0:
                    self.SellMarket()
                elif self.Position < 0:
                    self.BuyMarket()
                self._entry_time = None

        buy_open_signal = self._enable_buy_entries.Value and current_color == 2 and previous_color != 2
        sell_close_signal = self._enable_sell_exits.Value and current_color == 2
        sell_open_signal = self._enable_sell_entries.Value and current_color == 0 and previous_color != 0
        buy_close_signal = self._enable_buy_exits.Value and current_color == 0

        if buy_close_signal and self.Position > 0:
            self.SellMarket()
            self._entry_time = None

        if sell_close_signal and self.Position < 0:
            self.BuyMarket()
            self._entry_time = None

        if buy_open_signal and self.Position == 0:
            self.BuyMarket()
            self._entry_time = candle.CloseTime

        if sell_open_signal and self.Position == 0:
            self.SellMarket()
            self._entry_time = candle.CloseTime

    def OnReseted(self):
        super(color_jfatl_digit_tm_plus_strategy, self).OnReseted()
        self._jma = None
        self._price_buffer = []
        self._color_history = []
        self._previous_line = None
        self._entry_time = None

    def CreateClone(self):
        return color_jfatl_digit_tm_plus_strategy()
