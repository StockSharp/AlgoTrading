import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import JurikMovingAverage
from indicator_extensions import *

# FATL weights from the original C# indicator
_FATL_WEIGHTS = [
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
_MAX_FATL_PERIOD = len(_FATL_WEIGHTS)

class _ColorJfatlDigitState(object):
    """Internal JFATL Digit indicator calculator."""

    def __init__(self, jma_length, jma_phase, applied_price, digit, signal_bar, fatl_period):
        self._jma_length = jma_length
        self._jma_phase = max(-100, min(100, jma_phase))
        self._applied_price = applied_price  # 1=Close,2=Open,3=High,4=Low,5=Med,6=Typ,7=Wt
        self._digit = digit
        self._signal_bar = max(0, signal_bar)
        self._fatl_period = max(1, min(fatl_period, _MAX_FATL_PERIOD))

        self._jma = JurikMovingAverage()
        self._jma.Length = max(1, jma_length)
        self._price_buffer = []
        self._history = []  # list of (value, color)
        self._previous_raw = None

    def process(self, candle):
        price = self._get_price(candle)
        self._price_buffer.append(price)
        if len(self._price_buffer) > _MAX_FATL_PERIOD:
            self._price_buffer.pop(0)

        if len(self._price_buffer) < self._fatl_period:
            return None

        fatl = 0.0
        for i in range(self._fatl_period):
            pi = len(self._price_buffer) - 1 - i
            fatl += _FATL_WEIGHTS[i] * self._price_buffer[pi]

        jma_val = process_float(self._jma, Decimal(fatl), candle.ServerTime, True)
        base_value = float(jma_val.Value)
        adjusted = self._apply_phase(base_value)
        rounded = round(adjusted, max(0, self._digit))
        color = self._calc_color(rounded)

        self._history.append((rounded, color))
        required = max(5, self._signal_bar + 3)
        if len(self._history) > required:
            self._history = self._history[-required:]

        if len(self._history) <= self._signal_bar:
            return None

        index = len(self._history) - 1 - self._signal_bar
        if index < 1:
            return None

        entry = self._history[index]
        prev_entry = self._history[index - 1]

        return (entry[0], entry[1], prev_entry[1])

    def _get_price(self, candle):
        c = float(candle.ClosePrice)
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        p = self._applied_price
        if p == 2:
            return o
        if p == 3:
            return h
        if p == 4:
            return lo
        if p == 5:
            return (h + lo) / 2.0
        if p == 6:
            return (c + h + lo) / 3.0
        if p == 7:
            return (2.0 * c + h + lo) / 4.0
        return c

    def _apply_phase(self, base_value):
        adjusted = base_value
        if self._previous_raw is not None:
            diff = base_value - self._previous_raw
            adjusted = base_value + diff * (self._jma_phase / 100.0)
        self._previous_raw = base_value
        return adjusted

    def _calc_color(self, current_value):
        if len(self._history) == 0:
            return 1
        prev_value = self._history[-1][0]
        diff = current_value - prev_value
        if diff > 0:
            return 2
        if diff < 0:
            return 0
        return self._history[-1][1]

class color_jfatl_digit_duplex_strategy(Strategy):
    """Duplex strategy using two Color JFATL Digit indicators for independent long/short logic."""

    def __init__(self):
        super(color_jfatl_digit_duplex_strategy, self).__init__()

        self._long_candle_type = self.Param("LongCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Long Candle Type", "Timeframe for the long indicator", "General")
        self._short_candle_type = self.Param("ShortCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Short Candle Type", "Timeframe for the short indicator", "General")
        self._long_jma_length = self.Param("LongJmaLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Long JMA Length", "Period of JMA for longs", "Indicator")
        self._long_jma_phase = self.Param("LongJmaPhase", -100) \
            .SetDisplay("Long JMA Phase", "Phase adjustment for JMA", "Indicator")
        # 1=Close,2=Open,3=High,4=Low,5=Med,6=Typ,7=Wt
        self._long_applied_price = self.Param("LongAppliedPrice", 1) \
            .SetDisplay("Long Applied Price", "Price source for the long indicator", "Indicator")
        self._long_digit = self.Param("LongDigit", 2) \
            .SetDisplay("Long Rounding Digits", "Digits used to round the indicator", "Indicator")
        self._long_signal_bar = self.Param("LongSignalBar", 1) \
            .SetDisplay("Long Signal Bar", "Bar shift for long signals", "Indicator")
        self._long_stop_loss_points = self.Param("LongStopLossPoints", 1000) \
            .SetDisplay("Long Stop Loss (pts)", "Stop loss for long trades", "Risk")
        self._long_take_profit_points = self.Param("LongTakeProfitPoints", 2000) \
            .SetDisplay("Long Take Profit (pts)", "Take profit for long trades", "Risk")
        self._enable_long_open = self.Param("EnableLongOpen", True) \
            .SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading")
        self._enable_long_close = self.Param("EnableLongClose", True) \
            .SetDisplay("Enable Long Exits", "Allow closing long on signals", "Trading")

        self._short_jma_length = self.Param("ShortJmaLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Short JMA Length", "Period of JMA for shorts", "Indicator")
        self._short_jma_phase = self.Param("ShortJmaPhase", -100) \
            .SetDisplay("Short JMA Phase", "Phase adjustment for JMA", "Indicator")
        self._short_applied_price = self.Param("ShortAppliedPrice", 1) \
            .SetDisplay("Short Applied Price", "Price source for the short indicator", "Indicator")
        self._short_digit = self.Param("ShortDigit", 2) \
            .SetDisplay("Short Rounding Digits", "Digits used to round the indicator", "Indicator")
        self._short_signal_bar = self.Param("ShortSignalBar", 1) \
            .SetDisplay("Short Signal Bar", "Bar shift for short signals", "Indicator")
        self._short_stop_loss_points = self.Param("ShortStopLossPoints", 1000) \
            .SetDisplay("Short Stop Loss (pts)", "Stop loss for short trades", "Risk")
        self._short_take_profit_points = self.Param("ShortTakeProfitPoints", 2000) \
            .SetDisplay("Short Take Profit (pts)", "Take profit for short trades", "Risk")
        self._enable_short_open = self.Param("EnableShortOpen", True) \
            .SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading")
        self._enable_short_close = self.Param("EnableShortClose", True) \
            .SetDisplay("Enable Short Exits", "Allow closing short on signals", "Trading")
        self._fatl_period = self.Param("FatlPeriod", _MAX_FATL_PERIOD) \
            .SetDisplay("FATL Period", "Number of bars for the FATL calculation", "Indicator")

        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None

    @property
    def LongCandleType(self):
        return self._long_candle_type.Value
    @property
    def ShortCandleType(self):
        return self._short_candle_type.Value
    @property
    def LongJmaLength(self):
        return int(self._long_jma_length.Value)
    @property
    def LongJmaPhase(self):
        return int(self._long_jma_phase.Value)
    @property
    def LongAppliedPrice(self):
        return int(self._long_applied_price.Value)
    @property
    def LongDigit(self):
        return int(self._long_digit.Value)
    @property
    def LongSignalBar(self):
        return int(self._long_signal_bar.Value)
    @property
    def LongStopLossPoints(self):
        return int(self._long_stop_loss_points.Value)
    @property
    def LongTakeProfitPoints(self):
        return int(self._long_take_profit_points.Value)
    @property
    def EnableLongOpen(self):
        return self._enable_long_open.Value
    @property
    def EnableLongClose(self):
        return self._enable_long_close.Value
    @property
    def ShortJmaLength(self):
        return int(self._short_jma_length.Value)
    @property
    def ShortJmaPhase(self):
        return int(self._short_jma_phase.Value)
    @property
    def ShortAppliedPrice(self):
        return int(self._short_applied_price.Value)
    @property
    def ShortDigit(self):
        return int(self._short_digit.Value)
    @property
    def ShortSignalBar(self):
        return int(self._short_signal_bar.Value)
    @property
    def ShortStopLossPoints(self):
        return int(self._short_stop_loss_points.Value)
    @property
    def ShortTakeProfitPoints(self):
        return int(self._short_take_profit_points.Value)
    @property
    def EnableShortOpen(self):
        return self._enable_short_open.Value
    @property
    def EnableShortClose(self):
        return self._enable_short_close.Value
    @property
    def FatlPeriod(self):
        return int(self._fatl_period.Value)

    def OnStarted2(self, time):
        super(color_jfatl_digit_duplex_strategy, self).OnStarted2(time)

        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None

        self._long_state = _ColorJfatlDigitState(
            self.LongJmaLength, self.LongJmaPhase, self.LongAppliedPrice,
            self.LongDigit, self.LongSignalBar, self.FatlPeriod
        )
        self._short_state = _ColorJfatlDigitState(
            self.ShortJmaLength, self.ShortJmaPhase, self.ShortAppliedPrice,
            self.ShortDigit, self.ShortSignalBar, self.FatlPeriod
        )

        long_sub = self.SubscribeCandles(self.LongCandleType)
        long_sub.Bind(self._process_long).Start()

        short_sub = self.SubscribeCandles(self.ShortCandleType)
        short_sub.Bind(self._process_short).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, long_sub)
            self.DrawOwnTrades(area)

    def _process_long(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._check_long_risk(candle):
            return

        result = self._long_state.process(candle)
        if result is None:
            return

        value, current_color, previous_color = result

        if self.EnableLongClose and current_color == 0 and self.Position > 0:
            self._close_position()
            self._clear_long_risk()
            return

        if self.EnableLongOpen and current_color == 2 and previous_color < 2 and self.Position <= 0:
            self._open_long(float(candle.ClosePrice))

    def _process_short(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._check_short_risk(candle):
            return

        result = self._short_state.process(candle)
        if result is None:
            return

        value, current_color, previous_color = result

        if self.EnableShortClose and current_color == 2 and self.Position < 0:
            self._close_position()
            self._clear_short_risk()
            return

        if self.EnableShortOpen and current_color == 0 and previous_color > 0 and self.Position >= 0:
            self._open_short(float(candle.ClosePrice))

    def _open_long(self, entry_price):
        self.BuyMarket()
        self._setup_long_risk(entry_price)
        self._clear_short_risk()

    def _open_short(self, entry_price):
        self.SellMarket()
        self._setup_short_risk(entry_price)
        self._clear_long_risk()

    def _setup_long_risk(self, entry_price):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        self._long_stop_price = entry_price - self.LongStopLossPoints * step if self.LongStopLossPoints > 0 else None
        self._long_take_price = entry_price + self.LongTakeProfitPoints * step if self.LongTakeProfitPoints > 0 else None

    def _setup_short_risk(self, entry_price):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        self._short_stop_price = entry_price + self.ShortStopLossPoints * step if self.ShortStopLossPoints > 0 else None
        self._short_take_price = entry_price - self.ShortTakeProfitPoints * step if self.ShortTakeProfitPoints > 0 else None

    def _check_long_risk(self, candle):
        if self.Position <= 0:
            self._clear_long_risk()
            return False
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        if self._long_stop_price is not None and lo <= self._long_stop_price:
            self._close_position()
            self._clear_long_risk()
            return True
        if self._long_take_price is not None and h >= self._long_take_price:
            self._close_position()
            self._clear_long_risk()
            return True
        return False

    def _check_short_risk(self, candle):
        if self.Position >= 0:
            self._clear_short_risk()
            return False
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        if self._short_stop_price is not None and h >= self._short_stop_price:
            self._close_position()
            self._clear_short_risk()
            return True
        if self._short_take_price is not None and lo <= self._short_take_price:
            self._close_position()
            self._clear_short_risk()
            return True
        return False

    def _clear_long_risk(self):
        self._long_stop_price = None
        self._long_take_price = None

    def _clear_short_risk(self):
        self._short_stop_price = None
        self._short_take_price = None

    def _close_position(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

    def OnReseted(self):
        super(color_jfatl_digit_duplex_strategy, self).OnReseted()
        self._long_stop_price = None
        self._long_take_price = None
        self._short_stop_price = None
        self._short_take_price = None

    def CreateClone(self):
        return color_jfatl_digit_duplex_strategy()
