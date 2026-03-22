import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, ExponentialMovingAverage,
    SmoothedMovingAverage, WeightedMovingAverage,
    KaufmanAdaptiveMovingAverage, DecimalIndicatorValue
)


class color_xmuv_time_strategy(Strategy):
    """Color XMUV trend-following strategy with session filter."""

    def __init__(self):
        super(color_xmuv_time_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Source candles for the Color XMUV line", "General")
        self._order_volume = self.Param("OrderVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Size of market orders", "Trading")
        self._enable_buy_entries = self.Param("EnableBuyEntries", True) \
            .SetDisplay("Enable Long Entries", "Allow entering long positions", "Trading")
        self._enable_sell_entries = self.Param("EnableSellEntries", True) \
            .SetDisplay("Enable Short Entries", "Allow entering short positions", "Trading")
        self._enable_buy_exits = self.Param("EnableBuyExits", True) \
            .SetDisplay("Close Longs", "Close long positions on bearish flips", "Trading")
        self._enable_sell_exits = self.Param("EnableSellExits", True) \
            .SetDisplay("Close Shorts", "Close short positions on bullish flips", "Trading")
        self._use_time_filter = self.Param("UseTimeFilter", True) \
            .SetDisplay("Use Time Filter", "Restrict trading to the specified session", "Time Filter")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Trading session start hour", "Time Filter")
        self._start_minute = self.Param("StartMinute", 0) \
            .SetDisplay("Start Minute", "Trading session start minute", "Time Filter")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("End Hour", "Trading session end hour", "Time Filter")
        self._end_minute = self.Param("EndMinute", 59) \
            .SetDisplay("End Minute", "Trading session end minute", "Time Filter")
        self._x_length = self.Param("XLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Length", "Smoothing length", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Number of completed bars to delay signals", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 1000.0) \
            .SetDisplay("Stop Loss (pts)", "Stop loss distance in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000.0) \
            .SetDisplay("Take Profit (pts)", "Take profit distance in points", "Risk")

        self._color_history = []
        self._previous_xmuv = None
        self._xma = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def OrderVolume(self):
        return self._order_volume.Value
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
    def UseTimeFilter(self):
        return self._use_time_filter.Value
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
    def XLength(self):
        return self._x_length.Value
    @property
    def SignalBar(self):
        return self._signal_bar.Value
    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value
    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    def OnStarted(self, time):
        super(color_xmuv_time_strategy, self).OnStarted(time)

        self._xma = SimpleMovingAverage()
        self._xma.Length = self.XLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        sec = self.Security
        step = 0.0
        if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0:
            step = float(sec.PriceStep)

        tp_unit = None
        sl_unit = None
        if self.TakeProfitPoints > 0 and step > 0:
            tp_unit = Unit(self.TakeProfitPoints * step, UnitTypes.Absolute)
        if self.StopLossPoints > 0 and step > 0:
            sl_unit = Unit(self.StopLossPoints * step, UnitTypes.Absolute)

        self.StartProtection(takeProfit=tp_unit, stopLoss=sl_unit)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = self._calc_signal_price(candle)
        ind_out = self._xma.Process(DecimalIndicatorValue(self._xma, price, candle.OpenTime))

        if not self._xma.IsFormed:
            self._previous_xmuv = float(ind_out)
            return

        xmuv = float(ind_out)
        color = self._determine_color(xmuv)
        self._store_color(color)
        self._previous_xmuv = xmuv

        sb = self.SignalBar
        count = len(self._color_history)
        if count <= sb:
            return

        idx = count - 1 - sb
        if idx <= 0:
            return

        current_color = self._color_history[idx]
        previous_color = self._color_history[idx - 1]

        in_session = (not self.UseTimeFilter) or self._is_inside_session(candle.CloseTime)

        if not in_session:
            if self.Position > 0 and self.EnableBuyExits:
                self.SellMarket()
            elif self.Position < 0 and self.EnableSellExits:
                self.BuyMarket()
            return

        # 2=bullish, 0=bearish, 1=neutral
        bullish_flip = current_color == 2 and previous_color != 2
        bearish_flip = current_color == 0 and previous_color != 0

        if bullish_flip:
            if self.EnableSellExits and self.Position < 0:
                self.BuyMarket()
            if self.EnableBuyEntries and self.Position <= 0:
                self.BuyMarket()
        elif bearish_flip:
            if self.EnableBuyExits and self.Position > 0:
                self.SellMarket()
            if self.EnableSellEntries and self.Position >= 0:
                self.SellMarket()

    def _calc_signal_price(self, candle):
        c = float(candle.ClosePrice)
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        if c < o:
            return (lo + c) / 2.0
        if c > o:
            return (h + c) / 2.0
        return c

    def _determine_color(self, current_xmuv):
        if self._previous_xmuv is None:
            return 1
        if current_xmuv > self._previous_xmuv:
            return 2
        if current_xmuv < self._previous_xmuv:
            return 0
        return 1

    def _store_color(self, color):
        max_size = max(self.SignalBar + 2, 2)
        self._color_history.append(color)
        if len(self._color_history) > max_size:
            self._color_history.pop(0)

    def _is_inside_session(self, time):
        start = TimeSpan(self.StartHour, self.StartMinute, 0)
        end = TimeSpan(self.EndHour, self.EndMinute, 0)
        current = time.TimeOfDay
        if start == end:
            return False
        if start < end:
            return current >= start and current <= end
        return current >= start or current <= end

    def OnReseted(self):
        super(color_xmuv_time_strategy, self).OnReseted()
        self._color_history = []
        self._previous_xmuv = None
        self._xma = None

    def CreateClone(self):
        return color_xmuv_time_strategy()
