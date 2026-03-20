import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ChandeMomentumOscillator


class cmo_duplex_strategy(Strategy):
    """CMO Duplex: two-sided strategy using Chande Momentum Oscillator zero-line crossings."""

    def __init__(self):
        super(cmo_duplex_strategy, self).__init__()

        self._long_candle_type = self.Param("LongCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Long Candle Type", "Candle type for the long leg", "Long Leg")
        self._long_cmo_period = self.Param("LongCmoPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Long CMO Period", "CMO period for the long leg", "Long Leg")
        self._long_signal_bar = self.Param("LongSignalBar", 1) \
            .SetDisplay("Long Signal Bar", "Offset in bars for long signals", "Long Leg")
        self._enable_long_entries = self.Param("EnableLongEntries", True) \
            .SetDisplay("Enable Long Entries", "Allow opening long trades", "Long Leg")
        self._enable_long_exits = self.Param("EnableLongExits", True) \
            .SetDisplay("Enable Long Exits", "Allow closing long trades on signals", "Long Leg")
        self._long_stop_loss_points = self.Param("LongStopLossPoints", 1000) \
            .SetDisplay("Long Stop Loss", "Stop loss in price steps for longs", "Risk Management")
        self._long_take_profit_points = self.Param("LongTakeProfitPoints", 2000) \
            .SetDisplay("Long Take Profit", "Take profit in price steps for longs", "Risk Management")

        self._short_candle_type = self.Param("ShortCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Short Candle Type", "Candle type for the short leg", "Short Leg")
        self._short_cmo_period = self.Param("ShortCmoPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Short CMO Period", "CMO period for the short leg", "Short Leg")
        self._short_signal_bar = self.Param("ShortSignalBar", 1) \
            .SetDisplay("Short Signal Bar", "Offset in bars for short signals", "Short Leg")
        self._enable_short_entries = self.Param("EnableShortEntries", True) \
            .SetDisplay("Enable Short Entries", "Allow opening short trades", "Short Leg")
        self._enable_short_exits = self.Param("EnableShortExits", True) \
            .SetDisplay("Enable Short Exits", "Allow closing short trades on signals", "Short Leg")
        self._short_stop_loss_points = self.Param("ShortStopLossPoints", 1000) \
            .SetDisplay("Short Stop Loss", "Stop loss in price steps for shorts", "Risk Management")
        self._short_take_profit_points = self.Param("ShortTakeProfitPoints", 2000) \
            .SetDisplay("Short Take Profit", "Take profit in price steps for shorts", "Risk Management")

        self._long_values = []
        self._short_values = []
        self._entry_price = None

    @property
    def LongCandleType(self):
        return self._long_candle_type.Value
    @property
    def LongCmoPeriod(self):
        return int(self._long_cmo_period.Value)
    @property
    def LongSignalBar(self):
        return int(self._long_signal_bar.Value)
    @property
    def EnableLongEntries(self):
        return self._enable_long_entries.Value
    @property
    def EnableLongExits(self):
        return self._enable_long_exits.Value
    @property
    def LongStopLossPoints(self):
        return int(self._long_stop_loss_points.Value)
    @property
    def LongTakeProfitPoints(self):
        return int(self._long_take_profit_points.Value)
    @property
    def ShortCandleType(self):
        return self._short_candle_type.Value
    @property
    def ShortCmoPeriod(self):
        return int(self._short_cmo_period.Value)
    @property
    def ShortSignalBar(self):
        return int(self._short_signal_bar.Value)
    @property
    def EnableShortEntries(self):
        return self._enable_short_entries.Value
    @property
    def EnableShortExits(self):
        return self._enable_short_exits.Value
    @property
    def ShortStopLossPoints(self):
        return int(self._short_stop_loss_points.Value)
    @property
    def ShortTakeProfitPoints(self):
        return int(self._short_take_profit_points.Value)

    def OnStarted(self, time):
        super(cmo_duplex_strategy, self).OnStarted(time)

        self._long_values = []
        self._short_values = []
        self._entry_price = None

        self._long_cmo = ChandeMomentumOscillator()
        self._long_cmo.Length = self.LongCmoPeriod

        self._short_cmo = ChandeMomentumOscillator()
        self._short_cmo.Length = self.ShortCmoPeriod

        long_subscription = self.SubscribeCandles(self.LongCandleType)

        same_type = (self.LongCandleType == self.ShortCandleType)
        if same_type:
            long_subscription.Bind(self._long_cmo, self.process_long_candle)
            long_subscription.Bind(self._short_cmo, self.process_short_candle).Start()
        else:
            long_subscription.Bind(self._long_cmo, self.process_long_candle).Start()
            short_subscription = self.SubscribeCandles(self.ShortCandleType)
            short_subscription.Bind(self._short_cmo, self.process_short_candle).Start()

    def process_long_candle(self, candle, cmo_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._long_cmo.IsFormed:
            return

        cmo_val = float(cmo_value)
        self._long_values.append(cmo_val)
        shift = max(1, self.LongSignalBar)
        self._trim_buffer(self._long_values, shift + 3)

        if len(self._long_values) < shift + 1:
            return

        current_index = len(self._long_values) - shift
        previous_index = current_index - 1
        if previous_index < 0:
            return

        current = self._long_values[current_index]
        previous = self._long_values[previous_index]

        # Manage long exits
        if self.Position > 0 and self._entry_price is not None:
            sec = self.Security
            step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
            stop_price = self._entry_price - self.LongStopLossPoints * step if self.LongStopLossPoints > 0 else None
            take_price = self._entry_price + self.LongTakeProfitPoints * step if self.LongTakeProfitPoints > 0 else None
            exit_by_signal = self.EnableLongExits and previous < 0

            hit_take = take_price is not None and float(candle.HighPrice) >= take_price
            hit_stop = stop_price is not None and float(candle.LowPrice) <= stop_price

            if hit_take or hit_stop or exit_by_signal:
                self.SellMarket()
                self._entry_price = None

        # Long entry: CMO crosses down through zero
        cross_down = previous > 0 and current <= 0
        if self.EnableLongEntries and cross_down and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = float(candle.ClosePrice)

    def process_short_candle(self, candle, cmo_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._short_cmo.IsFormed:
            return

        cmo_val = float(cmo_value)
        self._short_values.append(cmo_val)
        shift = max(1, self.ShortSignalBar)
        self._trim_buffer(self._short_values, shift + 3)

        if len(self._short_values) < shift + 1:
            return

        current_index = len(self._short_values) - shift
        previous_index = current_index - 1
        if previous_index < 0:
            return

        current = self._short_values[current_index]
        previous = self._short_values[previous_index]

        # Manage short exits
        if self.Position < 0 and self._entry_price is not None:
            sec = self.Security
            step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
            stop_price = self._entry_price + self.ShortStopLossPoints * step if self.ShortStopLossPoints > 0 else None
            take_price = self._entry_price - self.ShortTakeProfitPoints * step if self.ShortTakeProfitPoints > 0 else None
            exit_by_signal = self.EnableShortExits and previous > 0

            hit_take = take_price is not None and float(candle.LowPrice) <= take_price
            hit_stop = stop_price is not None and float(candle.HighPrice) >= stop_price

            if hit_take or hit_stop or exit_by_signal:
                self.BuyMarket()
                self._entry_price = None

        # Short entry: CMO crosses up through zero
        cross_up = previous < 0 and current >= 0
        if self.EnableShortEntries and cross_up and self.Position >= 0:
            self.SellMarket()
            self._entry_price = float(candle.ClosePrice)

    def _trim_buffer(self, values, max_count):
        if len(values) > max_count:
            del values[:len(values) - max_count]

    def OnReseted(self):
        super(cmo_duplex_strategy, self).OnReseted()
        self._long_values = []
        self._short_values = []
        self._entry_price = None

    def CreateClone(self):
        return cmo_duplex_strategy()
