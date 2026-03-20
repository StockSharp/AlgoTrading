import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class frank_ud_minimal_strategy(Strategy):
    """Hedged martingale grid strategy that liquidates both sides once the newest
    position reaches the configured profit in pips."""

    def __init__(self):
        super(frank_ud_minimal_strategy, self).__init__()

        self._take_profit_pips = self.Param("TakeProfitPips", 65.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Profit trigger (pips)", "Pip profit that forces an exit of all positions", "Risk")
        self._re_entry_pips = self.Param("ReEntryPips", 41.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Re-entry distance (pips)", "Pip distance required before adding the next grid order", "Grid")
        self._initial_volume = self.Param("InitialVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Initial volume", "Base lot used for the very first order", "Risk")
        self._extra_take_profit_pips = self.Param("ExtraTakeProfitPips", 25.0) \
            .SetDisplay("Buffer profit (pips)", "Additional pip distance applied when calculating buffered targets", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Candle series used for price tracking", "General")

        self._long_entries = []
        self._short_entries = []
        self._point_value = 0.0
        self._take_profit_threshold = 0.0
        self._take_profit_distance = 0.0
        self._re_entry_distance = 0.0
        self._base_volume = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def ReEntryPips(self):
        return self._re_entry_pips.Value

    @property
    def InitialVolume(self):
        return self._initial_volume.Value

    @property
    def ExtraTakeProfitPips(self):
        return self._extra_take_profit_pips.Value

    def OnReseted(self):
        super(frank_ud_minimal_strategy, self).OnReseted()
        self._long_entries = []
        self._short_entries = []
        self._point_value = 0.0
        self._take_profit_threshold = 0.0
        self._take_profit_distance = 0.0
        self._re_entry_distance = 0.0
        self._base_volume = 0.0

    def OnStarted(self, time):
        super(frank_ud_minimal_strategy, self).OnStarted(time)

        self._point_value = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                self._point_value = ps

        self._take_profit_threshold = float(self.TakeProfitPips)
        self._take_profit_distance = (float(self.TakeProfitPips) + float(self.ExtraTakeProfitPips)) * self._point_value
        self._re_entry_distance = float(self.ReEntryPips) * self._point_value
        self._base_volume = float(self.InitialVolume)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        bid = float(candle.ClosePrice)
        ask = float(candle.ClosePrice)

        if bid <= 0 or ask <= 0:
            return

        if self._should_close_long(bid):
            self._close_long_positions()

        if self._should_close_short(ask):
            self._close_short_positions()

        if self._should_open_long(ask):
            self._open_long_position(ask)

        if self._should_open_short(bid):
            self._open_short_position(bid)

    def _should_close_long(self, bid):
        if len(self._long_entries) == 0:
            return False

        entry = self._get_max_volume_entry(self._long_entries)
        if entry is None:
            return False

        profit_pips = (bid - entry[0]) / self._point_value
        buffered_target = entry[0] + self._take_profit_distance
        reached_buffered = self._take_profit_distance > 0 and bid >= buffered_target

        return profit_pips > self._take_profit_threshold or reached_buffered

    def _should_close_short(self, ask):
        if len(self._short_entries) == 0:
            return False

        entry = self._get_max_volume_entry(self._short_entries)
        if entry is None:
            return False

        profit_pips = (entry[0] - ask) / self._point_value
        buffered_target = entry[0] - self._take_profit_distance
        reached_buffered = self._take_profit_distance > 0 and ask <= buffered_target

        return profit_pips > self._take_profit_threshold or reached_buffered

    def _should_open_long(self, ask):
        if self._base_volume <= 0:
            return False

        if len(self._long_entries) == 0:
            return True

        lowest_price = self._get_extreme_price(self._long_entries, True)
        return lowest_price - self._re_entry_distance > ask

    def _should_open_short(self, bid):
        if self._base_volume <= 0:
            return False

        if len(self._short_entries) == 0:
            return True

        highest_price = self._get_extreme_price(self._short_entries, False)
        return highest_price + self._re_entry_distance < bid

    def _open_long_position(self, price):
        volume = self._determine_next_volume(self._long_entries)
        if volume <= 0:
            return

        self.BuyMarket(volume)
        self._long_entries.append([price, volume])

    def _open_short_position(self, price):
        volume = self._determine_next_volume(self._short_entries)
        if volume <= 0:
            return

        self.SellMarket(volume)
        self._short_entries.append([price, volume])

    def _close_long_positions(self):
        volume = self._get_total_volume(self._long_entries)
        if volume <= 0:
            return

        self.SellMarket(volume)
        self._long_entries = []

    def _close_short_positions(self):
        volume = self._get_total_volume(self._short_entries)
        if volume <= 0:
            return

        self.BuyMarket(volume)
        self._short_entries = []

    def _determine_next_volume(self, entries):
        if self._base_volume <= 0:
            return 0.0

        if len(entries) == 0:
            return self._base_volume

        max_vol = self._get_max_volume(entries)
        return max_vol * 2.0

    def _get_max_volume_entry(self, entries):
        result = None
        max_volume = 0.0

        for entry in entries:
            if entry[1] > max_volume:
                max_volume = entry[1]
                result = entry

        return result

    def _get_max_volume(self, entries):
        max_volume = 0.0
        for entry in entries:
            if entry[1] > max_volume:
                max_volume = entry[1]
        return max_volume

    def _get_total_volume(self, entries):
        total = 0.0
        for entry in entries:
            total += entry[1]
        return total

    def _get_extreme_price(self, entries, is_long):
        has_value = False
        result = 0.0

        for entry in entries:
            price = entry[0]
            if not has_value:
                result = price
                has_value = True
                continue

            if is_long:
                if price < result:
                    result = price
            else:
                if price > result:
                    result = price

        return result

    def CreateClone(self):
        return frank_ud_minimal_strategy()
