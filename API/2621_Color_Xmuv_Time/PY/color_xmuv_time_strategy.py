import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import SimpleMovingAverage, DecimalIndicatorValue


# TrendColors: 0=Bearish, 1=Neutral, 2=Bullish
BEARISH = 0
NEUTRAL = 1
BULLISH = 2


class color_xmuv_time_strategy(Strategy):
    """Color XMUV trend-following strategy with session filter."""

    def __init__(self):
        super(color_xmuv_time_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Source candles for the Color XMUV line", "General")
        self._order_volume = self.Param("OrderVolume", Decimal(1)) \
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
        self._use_time_filter = self.Param("UseTimeFilter", False) \
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
        self._max_color_history = self.Param("MaxColorHistory", 64) \
            .SetDisplay("Max Color History", "Maximum stored trend color values", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", Decimal(0)) \
            .SetDisplay("Stop Loss (pts)", "Stop loss distance in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", Decimal(0)) \
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
    def MaxColorHistory(self):
        return self._max_color_history.Value
    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value
    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    def OnStarted2(self, time):
        super(color_xmuv_time_strategy, self).OnStarted2(time)

        self._xma = SimpleMovingAverage()
        self._xma.Length = self.XLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        self.StartProtection(self._create_tp_unit(), self._create_sl_unit())

    def _create_sl_unit(self):
        sl = self.StopLossPoints
        if sl <= 0:
            return Unit()
        sec = self.Security
        if sec is None or sec.PriceStep is None:
            return Unit()
        step = sec.PriceStep
        if step <= 0:
            return Unit()
        return Unit(step * sl, UnitTypes.Absolute)

    def _create_tp_unit(self):
        tp = self.TakeProfitPoints
        if tp <= 0:
            return Unit()
        sec = self.Security
        if sec is None or sec.PriceStep is None:
            return Unit()
        step = sec.PriceStep
        if step <= 0:
            return Unit()
        return Unit(step * tp, UnitTypes.Absolute)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = self._calc_signal_price(candle)
        ind_val = DecimalIndicatorValue(self._xma, price, candle.OpenTime)
        ind_val.IsFinal = True
        ind_out = self._xma.Process(ind_val)

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
            self._force_exit_if_needed()
            return

        bullish_flip = current_color == BULLISH and previous_color != BULLISH
        bearish_flip = current_color == BEARISH and previous_color != BEARISH

        if bullish_flip:
            if self.Position < 0 and self.EnableSellExits:
                self.BuyMarket()
            elif self.Position == 0 and self.EnableBuyEntries:
                self.BuyMarket()
        elif bearish_flip:
            if self.Position > 0 and self.EnableBuyExits:
                self.SellMarket()
            elif self.Position == 0 and self.EnableSellEntries:
                self.SellMarket()

    def _calc_signal_price(self, candle):
        c = float(candle.ClosePrice)
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        if c < o:
            return Decimal((lo + c) / 2.0)
        if c > o:
            return Decimal((h + c) / 2.0)
        return candle.ClosePrice

    def _determine_color(self, current_xmuv):
        if self._previous_xmuv is None:
            return NEUTRAL
        if current_xmuv > self._previous_xmuv:
            return BULLISH
        if current_xmuv < self._previous_xmuv:
            return BEARISH
        return NEUTRAL

    def _store_color(self, color):
        max_size = max(self.SignalBar + 2, 2)
        if max_size > self.MaxColorHistory:
            max_size = self.MaxColorHistory
        self._color_history.append(color)
        if len(self._color_history) > max_size:
            self._color_history.pop(0)

    def _force_exit_if_needed(self):
        if self.Position > 0 and self.EnableBuyExits:
            self.SellMarket()
        elif self.Position < 0 and self.EnableSellExits:
            self.BuyMarket()

    def _is_inside_session(self, time):
        start = TimeSpan(self.StartHour, self.StartMinute, 0)
        end = TimeSpan(self.EndHour, self.EndMinute, 0)
        moment = time.TimeOfDay
        if start == end:
            return moment >= start and moment < end
        if start < end:
            return moment >= start and moment <= end
        return moment >= start or moment <= end

    def OnReseted(self):
        super(color_xmuv_time_strategy, self).OnReseted()
        self._color_history = []
        self._previous_xmuv = None
        self._xma = None

    def CreateClone(self):
        return color_xmuv_time_strategy()
