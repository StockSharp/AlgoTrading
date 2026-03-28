import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, CommodityChannelIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class exp_trend_magic_strategy(Strategy):
    """CCI + ATR TrendMagic strategy with color change signals and virtual SL/TP."""

    # Applied price modes
    PRICE_CLOSE = 0
    PRICE_OPEN = 1
    PRICE_HIGH = 2
    PRICE_LOW = 3
    PRICE_MEDIAN = 4
    PRICE_TYPICAL = 5
    PRICE_WEIGHTED = 6
    PRICE_AVERAGE = 7

    def __init__(self):
        super(exp_trend_magic_strategy, self).__init__()

        self._money_management = self.Param("MoneyManagement", 0.1) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 1000.0) \
            .SetDisplay("Stop Loss", "Protective stop in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000.0) \
            .SetDisplay("Take Profit", "Profit target in points", "Risk")
        self._allow_buy_entry = self.Param("AllowBuyEntry", True) \
            .SetDisplay("Allow Buy Entry", "Enable long entries", "Permissions")
        self._allow_sell_entry = self.Param("AllowSellEntry", True) \
            .SetDisplay("Allow Sell Entry", "Enable short entries", "Permissions")
        self._allow_buy_exit = self.Param("AllowBuyExit", True) \
            .SetDisplay("Allow Buy Exit", "Enable exits for short trades", "Permissions")
        self._allow_sell_exit = self.Param("AllowSellExit", True) \
            .SetDisplay("Allow Sell Exit", "Enable exits for long trades", "Permissions")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary candle series", "Data")
        self._cci_period = self.Param("CciPeriod", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "Length of the CCI", "Indicator")
        self._cci_price = self.Param("CciPrice", 4) \
            .SetDisplay("CCI Price", "Applied price for the CCI (4=Median)", "Indicator")
        self._atr_period = self.Param("AtrPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Length of the ATR", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Bar shift used for signals", "Indicator")

        self._cci = None
        self._atr = None
        self._color_history = None
        self._previous_trend_magic_value = None
        self._entry_price = None
        self._candle_time_frame = None
        self._next_long_trade_allowed = None
        self._next_short_trade_allowed = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MoneyManagement(self):
        return self._money_management.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def AllowBuyEntry(self):
        return self._allow_buy_entry.Value

    @property
    def AllowSellEntry(self):
        return self._allow_sell_entry.Value

    @property
    def AllowBuyExit(self):
        return self._allow_buy_exit.Value

    @property
    def AllowSellExit(self):
        return self._allow_sell_exit.Value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @property
    def CciPrice(self):
        return self._cci_price.Value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    def OnReseted(self):
        super(exp_trend_magic_strategy, self).OnReseted()
        self._cci = None
        self._atr = None
        self._color_history = None
        self._previous_trend_magic_value = None
        self._entry_price = None
        self._candle_time_frame = None
        self._next_long_trade_allowed = None
        self._next_short_trade_allowed = None

    def OnStarted(self, time):
        super(exp_trend_magic_strategy, self).OnStarted(time)

        self._color_history = []
        self._candle_time_frame = TimeSpan.FromHours(4)

        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._atr, self._process_candle).Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        cci = self._cci
        atr = self._atr
        if cci is None or atr is None:
            return

        price = self._get_applied_price(candle, self.CciPrice)
        cci_input = DecimalIndicatorValue(cci, price, candle.OpenTime)
        cci_input.IsFinal = True
        cci_indicator_value = cci.Process(cci_input)

        if not cci.IsFormed or not atr.IsFormed:
            return

        atr_decimal = float(atr_value)
        cci_decimal = float(cci_indicator_value)

        self._update_trend_magic(candle, cci_decimal, atr_decimal)

    def _update_trend_magic(self, candle, cci_value, atr_value):
        color = self._calculate_color(candle, cci_value, atr_value)
        self._color_history.insert(0, color)

        max_history = max(2, self.SignalBar + 2)
        if len(self._color_history) > max_history:
            self._color_history = self._color_history[:max_history]

        if len(self._color_history) <= self.SignalBar + 1:
            self._manage_risk(candle)
            return

        recent = self._color_history[self.SignalBar]
        older = self._color_history[self.SignalBar + 1]

        if older == 0 and self.AllowSellExit and self.Position < 0:
            self.BuyMarket(abs(self.Position))
            self._entry_price = None
        elif older == 1 and self.AllowBuyExit and self.Position > 0:
            self.SellMarket(self.Position)
            self._entry_price = None

        if older == 0 and recent == 1 and self.AllowBuyEntry:
            self._try_enter_long(candle)
        elif older == 1 and recent == 0 and self.AllowSellEntry:
            self._try_enter_short(candle)

        self._manage_risk(candle)

    def _try_enter_long(self, candle):
        if self._next_long_trade_allowed is not None and candle.OpenTime < self._next_long_trade_allowed:
            return

        volume = self._calculate_order_volume()
        if volume <= 0:
            return

        if self.Position < 0:
            self.BuyMarket(abs(self.Position))
            self._entry_price = None

        self.BuyMarket(volume)
        self._entry_price = float(candle.ClosePrice)
        self._next_long_trade_allowed = candle.OpenTime + self._candle_time_frame \
            if self._candle_time_frame > TimeSpan.Zero else candle.OpenTime

    def _try_enter_short(self, candle):
        if self._next_short_trade_allowed is not None and candle.OpenTime < self._next_short_trade_allowed:
            return

        volume = self._calculate_order_volume()
        if volume <= 0:
            return

        if self.Position > 0:
            self.SellMarket(self.Position)
            self._entry_price = None

        self.SellMarket(volume)
        self._entry_price = float(candle.ClosePrice)
        self._next_short_trade_allowed = candle.OpenTime + self._candle_time_frame \
            if self._candle_time_frame > TimeSpan.Zero else candle.OpenTime

    def _manage_risk(self, candle):
        if self.Position == 0 or self._entry_price is None:
            return

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                step = ps

        if self.Position > 0:
            if float(self.StopLossPoints) > 0:
                stop_price = self._entry_price - float(self.StopLossPoints) * step
                if float(candle.LowPrice) <= stop_price:
                    self.SellMarket(self.Position)
                    self._entry_price = None
                    return
            if float(self.TakeProfitPoints) > 0:
                take_price = self._entry_price + float(self.TakeProfitPoints) * step
                if float(candle.HighPrice) >= take_price:
                    self.SellMarket(self.Position)
                    self._entry_price = None
        elif self.Position < 0:
            if float(self.StopLossPoints) > 0:
                stop_price = self._entry_price + float(self.StopLossPoints) * step
                if float(candle.HighPrice) >= stop_price:
                    self.BuyMarket(abs(self.Position))
                    self._entry_price = None
                    return
            if float(self.TakeProfitPoints) > 0:
                take_price = self._entry_price - float(self.TakeProfitPoints) * step
                if float(candle.LowPrice) <= take_price:
                    self.BuyMarket(abs(self.Position))
                    self._entry_price = None

    def _calculate_color(self, candle, cci_value, atr_value):
        previous = self._previous_trend_magic_value

        if cci_value >= 0:
            trend_magic = float(candle.LowPrice) - atr_value
            if previous is not None and trend_magic < previous:
                trend_magic = previous
            color = 0
        else:
            trend_magic = float(candle.HighPrice) + atr_value
            if previous is not None and trend_magic > previous:
                trend_magic = previous
            color = 1

        self._previous_trend_magic_value = trend_magic
        return color

    def _calculate_order_volume(self):
        mm = float(self.MoneyManagement)
        if mm == 0:
            return float(self.Volume) if self.Volume > 0 else 1.0
        if mm < 0:
            return abs(mm)
        return mm

    def _get_applied_price(self, candle, mode):
        if mode == self.PRICE_CLOSE:
            return float(candle.ClosePrice)
        elif mode == self.PRICE_OPEN:
            return float(candle.OpenPrice)
        elif mode == self.PRICE_HIGH:
            return float(candle.HighPrice)
        elif mode == self.PRICE_LOW:
            return float(candle.LowPrice)
        elif mode == self.PRICE_MEDIAN:
            return (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        elif mode == self.PRICE_TYPICAL:
            return (float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 3.0
        elif mode == self.PRICE_WEIGHTED:
            return (float(candle.HighPrice) + float(candle.LowPrice) + 2.0 * float(candle.ClosePrice)) / 4.0
        elif mode == self.PRICE_AVERAGE:
            return (float(candle.OpenPrice) + float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 4.0
        return float(candle.ClosePrice)

    def CreateClone(self):
        return exp_trend_magic_strategy()
