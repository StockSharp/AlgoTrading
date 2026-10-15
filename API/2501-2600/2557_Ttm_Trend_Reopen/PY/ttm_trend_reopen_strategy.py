import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BaseIndicator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class ttm_trend_reopen_strategy(Strategy):
    def __init__(self):
        super(ttm_trend_reopen_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe", "General")
        self._comp_bars = self.Param("CompBars", 6).SetGreaterThanZero().SetDisplay("Comparison Bars", "HA bars for color smoothing", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1).SetNotNegative().SetDisplay("Signal Bar", "Offset of signal bar", "Indicator")
        self._price_step_points = self.Param("PriceStepPoints", 1000.0).SetNotNegative().SetDisplay("Re-entry Step", "Min favorable move for pyramiding", "Risk")
        self._max_positions = self.Param("MaxPositions", 1).SetGreaterThanZero().SetDisplay("Max Entries", "Max stacked entries per direction", "Risk")
        self._enable_long_entries = self.Param("EnableLongEntries", True).SetDisplay("Enable Long Entries", "Allow buying on bullish", "Trading Rules")
        self._enable_short_entries = self.Param("EnableShortEntries", True).SetDisplay("Enable Short Entries", "Allow selling on bearish", "Trading Rules")
        self._enable_long_exits = self.Param("EnableLongExits", True).SetDisplay("Enable Long Exits", "Close longs on bearish", "Trading Rules")
        self._enable_short_exits = self.Param("EnableShortExits", True).SetDisplay("Enable Short Exits", "Close shorts on bullish", "Trading Rules")
        self._sl_points = self.Param("StopLossPoints", 1000.0).SetNotNegative().SetDisplay("Stop Loss (points)", "SL distance", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 2000.0).SetNotNegative().SetDisplay("Take Profit (points)", "TP distance", "Risk")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(ttm_trend_reopen_strategy, self).OnReseted()
        self._color_history = []
        self._long_entries = 0
        self._short_entries = 0
        self._entry_price = 0
        self._prev_ha_open = None
        self._prev_ha_close = None
        self._ha_history = []

    def OnStarted2(self, time):
        super(ttm_trend_reopen_strategy, self).OnStarted2(time)
        self._color_history = []
        self._long_entries = 0
        self._short_entries = 0
        self._entry_price = 0
        self._prev_ha_open = None
        self._prev_ha_close = None
        self._ha_history = []

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._on_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)

        sl = float(self._sl_points.Value)
        tp = float(self._tp_points.Value)
        sl_unit = Unit(sl * step, UnitTypes.Absolute) if sl > 0 else None
        tp_unit = Unit(tp * step, UnitTypes.Absolute) if tp > 0 else None
        if sl_unit is not None or tp_unit is not None:
            self.StartProtection(sl_unit, tp_unit)

    def _calc_base_color(self, candle, ha_open, ha_close):
        if ha_close > ha_open:
            return 4 if float(candle.OpenPrice) <= float(candle.ClosePrice) else 3
        elif ha_close < ha_open:
            return 0 if float(candle.OpenPrice) > float(candle.ClosePrice) else 1
        else:
            return 2

    def _on_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        ha_close = (o + h + l + c) / 4.0
        if self._prev_ha_open is None or self._prev_ha_close is None:
            ha_open = (o + c) / 2.0
        else:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0

        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close

        color = self._calc_base_color(candle, ha_open, ha_close)

        comp = int(self._comp_bars.Value)
        for entry in self._ha_history:
            e_high = max(entry[0], entry[1])
            e_low = min(entry[0], entry[1])
            if ha_open <= e_high and ha_open >= e_low and ha_close <= e_high and ha_close >= e_low:
                color = entry[2]
                break

        self._ha_history.insert(0, (ha_open, ha_close, color))
        while len(self._ha_history) > max(1, comp):
            self._ha_history.pop()

        self._color_history.append(color)

        signal_bar = int(self._signal_bar.Value)
        offset = max(0, signal_bar - 1)
        signal_index = len(self._color_history) - 1 - offset
        if signal_index < 0:
            return

        current_color = self._color_history[signal_index]
        previous_color = self._color_history[signal_index - 1] if signal_index > 0 else None

        is_bullish = current_color == 1 or current_color == 4
        is_bearish = current_color == 0 or current_color == 3

        was_bullish = previous_color is not None and (previous_color == 1 or previous_color == 4)
        was_bearish = previous_color is not None and (previous_color == 0 or previous_color == 3)

        enable_long_exits = self._enable_long_exits.Value
        enable_short_exits = self._enable_short_exits.Value
        enable_long_entries = self._enable_long_entries.Value
        enable_short_entries = self._enable_short_entries.Value

        pos = self.Position

        if enable_long_exits and is_bearish and pos > 0:
            self.SellMarket()
            self._long_entries = 0

        if enable_short_exits and is_bullish and pos < 0:
            self.BuyMarket()
            self._short_entries = 0

        pos = self.Position

        if enable_long_entries and is_bullish and previous_color is not None and not was_bullish and pos <= 0:
            if pos < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._long_entries = 1
            self._short_entries = 0
            self._entry_price = float(candle.ClosePrice)
        elif enable_short_entries and is_bearish and previous_color is not None and not was_bearish and pos >= 0:
            if pos > 0:
                self.SellMarket()
            self.SellMarket()
            self._short_entries = 1
            self._long_entries = 0
            self._entry_price = float(candle.ClosePrice)

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        reentry_step = float(self._price_step_points.Value) * step
        max_pos = int(self._max_positions.Value)

        pos = self.Position
        if enable_long_entries and pos > 0 and reentry_step > 0 and self._long_entries > 0 and self._long_entries < max_pos:
            distance = float(candle.ClosePrice) - self._entry_price
            if distance >= reentry_step:
                self.BuyMarket()
                self._long_entries += 1
                self._entry_price = float(candle.ClosePrice)
        elif enable_short_entries and pos < 0 and reentry_step > 0 and self._short_entries > 0 and self._short_entries < max_pos:
            distance = self._entry_price - float(candle.ClosePrice)
            if distance >= reentry_step:
                self.SellMarket()
                self._short_entries += 1
                self._entry_price = float(candle.ClosePrice)

        keep = max(offset + 2, 3)
        if len(self._color_history) > keep:
            self._color_history = self._color_history[len(self._color_history) - keep:]

    def CreateClone(self):
        return ttm_trend_reopen_strategy()
