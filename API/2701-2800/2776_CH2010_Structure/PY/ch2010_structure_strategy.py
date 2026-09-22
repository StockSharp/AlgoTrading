import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides, OrderTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Order


# Daily bias produced by the close/open relationship of the higher time frame candle.
BIAS_NEUTRAL = 0
BIAS_LONG = 1
BIAS_SHORT = 2


class InstrumentContext:
    """Per-instrument state: the daily levels with their session date and the live position."""

    def __init__(self, alias, security):
        self.alias = alias
        self.security = security
        self.reset()

    def reset(self):
        self.daily_date = None
        self.daily_high = 0.0
        self.daily_low = 0.0
        self.daily_close = 0.0
        self.bias = BIAS_NEUTRAL
        self.has_levels = False
        self.long_triggered = False
        self.short_triggered = False
        self.reset_position()

    def reset_position(self):
        self.last_known_position = 0.0
        self.entry_side = None
        self.entry_price = None
        self.stop_price = None
        self.take_profit_price = None
        self.exit_in_progress = False


class ch2010_structure_strategy(Strategy):
    """
    Multi-currency breakout strategy converted from the original CH2010 structure expert.
    Watches daily candles to define trend bias and 30-minute candles for entries and exits.
    """

    def __init__(self):
        super(ch2010_structure_strategy, self).__init__()

        self._usd_chf = self.Param[Security]("UsdChfSecurity", None) \
            .SetDisplay("USD/CHF", "USDCHF symbol to trade", "Instruments")

        self._gbp_usd = self.Param[Security]("GbpUsdSecurity", None) \
            .SetDisplay("GBP/USD", "GBPUSD symbol to trade", "Instruments")

        self._aud_usd = self.Param[Security]("AudUsdSecurity", None) \
            .SetDisplay("AUD/USD", "AUDUSD symbol to trade", "Instruments")

        self._usd_jpy = self.Param[Security]("UsdJpySecurity", None) \
            .SetDisplay("USD/JPY", "USDJPY symbol to trade", "Instruments")

        self._eur_gbp = self.Param[Security]("EurGbpSecurity", None) \
            .SetDisplay("EUR/GBP", "EURGBP symbol to trade", "Instruments")

        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Nominal volume used for entries", "Risk")

        self._min_trade_volume = self.Param("MinTradeVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Minimum Volume", "Lower bound that mirrors the MQL expert", "Risk")

        self._max_trade_volume = self.Param("MaxTradeVolume", 5.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Maximum Volume", "Upper bound for a single position", "Risk")

        self._max_aggregate_volume = self.Param("MaxAggregateVolume", 15.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Aggregate Volume", "Cap across all instruments", "Risk")

        self._stop_loss_percent = self.Param("StopLossPercent", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Protective stop percentage", "Risk")

        self._take_profit_percent = self.Param("TakeProfitPercent", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit %", "Profit target percentage", "Risk")

        self._breakout_buffer_percent = self.Param("BreakoutBufferPercent", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Buffer %", "Percentage of daily range added above/below breakout", "Logic")

        self._daily_candle_type = self.Param("DailyCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Daily Candle", "Time frame used for the daily bias", "Data")

        self._intraday_candle_type = self.Param("IntradayCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Intraday Candle", "Time frame used for intraday execution", "Data")

        self._contexts = []

    @property
    def UsdChfSecurity(self):
        return self._usd_chf.Value

    @UsdChfSecurity.setter
    def UsdChfSecurity(self, value):
        self._usd_chf.Value = value

    @property
    def GbpUsdSecurity(self):
        return self._gbp_usd.Value

    @GbpUsdSecurity.setter
    def GbpUsdSecurity(self, value):
        self._gbp_usd.Value = value

    @property
    def AudUsdSecurity(self):
        return self._aud_usd.Value

    @AudUsdSecurity.setter
    def AudUsdSecurity(self, value):
        self._aud_usd.Value = value

    @property
    def UsdJpySecurity(self):
        return self._usd_jpy.Value

    @UsdJpySecurity.setter
    def UsdJpySecurity(self, value):
        self._usd_jpy.Value = value

    @property
    def EurGbpSecurity(self):
        return self._eur_gbp.Value

    @EurGbpSecurity.setter
    def EurGbpSecurity(self, value):
        self._eur_gbp.Value = value

    @property
    def TradeVolume(self):
        return float(self._trade_volume.Value)

    @TradeVolume.setter
    def TradeVolume(self, value):
        self._trade_volume.Value = value

    @property
    def MinTradeVolume(self):
        return float(self._min_trade_volume.Value)

    @MinTradeVolume.setter
    def MinTradeVolume(self, value):
        self._min_trade_volume.Value = value

    @property
    def MaxTradeVolume(self):
        return float(self._max_trade_volume.Value)

    @MaxTradeVolume.setter
    def MaxTradeVolume(self, value):
        self._max_trade_volume.Value = value

    @property
    def MaxAggregateVolume(self):
        return float(self._max_aggregate_volume.Value)

    @MaxAggregateVolume.setter
    def MaxAggregateVolume(self, value):
        self._max_aggregate_volume.Value = value

    @property
    def StopLossPercent(self):
        return float(self._stop_loss_percent.Value)

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def TakeProfitPercent(self):
        return float(self._take_profit_percent.Value)

    @TakeProfitPercent.setter
    def TakeProfitPercent(self, value):
        self._take_profit_percent.Value = value

    @property
    def BreakoutBufferPercent(self):
        return float(self._breakout_buffer_percent.Value)

    @BreakoutBufferPercent.setter
    def BreakoutBufferPercent(self, value):
        self._breakout_buffer_percent.Value = value

    @property
    def DailyCandleType(self):
        return self._daily_candle_type.Value

    @DailyCandleType.setter
    def DailyCandleType(self, value):
        self._daily_candle_type.Value = value

    @property
    def IntradayCandleType(self):
        return self._intraday_candle_type.Value

    @IntradayCandleType.setter
    def IntradayCandleType(self, value):
        self._intraday_candle_type.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED !! Returns securities for strategy."""
        result = []
        for _, security in self._traded_slots():
            result.append((security, self.DailyCandleType))
            result.append((security, self.IntradayCandleType))
        return result

    def OnReseted(self):
        super(ch2010_structure_strategy, self).OnReseted()
        self._contexts = []

    def OnStarted2(self, time):
        super(ch2010_structure_strategy, self).OnStarted2(time)

        self._contexts = []

        for alias, security in self._traded_slots():
            context = InstrumentContext(alias, security)
            self._contexts.append(context)

            self.SubscribeCandles(self.DailyCandleType, True, security) \
                .Bind(lambda candle, c=context: self._process_daily_candle(c, candle)) \
                .Start()

            self.SubscribeCandles(self.IntradayCandleType, True, security) \
                .Bind(lambda candle, c=context: self._process_intraday_candle(c, candle)) \
                .Start()

        if len(self._contexts) == 0:
            raise Exception("At least one security must be configured.")

    def _traded_slots(self):
        """Configured currency pairs, or the security the strategy was started on."""
        slots = []

        pairs = [
            ("USDCHF", self.UsdChfSecurity),
            ("GBPUSD", self.GbpUsdSecurity),
            ("AUDUSD", self.AudUsdSecurity),
            ("USDJPY", self.UsdJpySecurity),
            ("EURGBP", self.EurGbpSecurity),
        ]

        for alias, security in pairs:
            if security is not None:
                slots.append((alias, security))

        # With no pair slot filled the example still trades the security it was started on.
        if len(slots) == 0 and self.Security is not None:
            slots.append((self.Security.Id, self.Security))

        return slots

    def _process_daily_candle(self, context, candle):
        if candle.State != CandleStates.Finished:
            return

        context.daily_date = candle.OpenTime.Date
        context.daily_high = float(candle.HighPrice)
        context.daily_low = float(candle.LowPrice)
        context.daily_close = float(candle.ClosePrice)
        context.has_levels = True
        context.long_triggered = False
        context.short_triggered = False

        if float(candle.ClosePrice) > float(candle.OpenPrice):
            context.bias = BIAS_LONG
        elif float(candle.ClosePrice) < float(candle.OpenPrice):
            context.bias = BIAS_SHORT
        else:
            context.bias = BIAS_NEUTRAL

        self.LogInfo("[{0}] Daily candle captured. High={1} Low={2} Close={3}".format(
            context.alias, candle.HighPrice, candle.LowPrice, candle.ClosePrice))

    def _process_intraday_candle(self, context, candle):
        if candle.State != CandleStates.Finished:
            return

        if not context.has_levels:
            return

        # Levels captured on an earlier session say nothing about the current one.
        if context.daily_date != candle.OpenTime.Date:
            return

        close = float(candle.ClosePrice)
        position = self._position_of(context)

        self._update_position_snapshot(context, close, position)

        if position != 0:
            self._manage_open_position(context, position, close)
            return

        rng = context.daily_high - context.daily_low

        if rng <= 0:
            return

        buffer = rng * (self.BreakoutBufferPercent / 100.0)
        long_trigger = context.daily_high + buffer
        short_trigger = context.daily_low - buffer

        if not context.long_triggered and context.bias != BIAS_SHORT:
            if close > long_trigger:
                self._try_enter_position(context, Sides.Buy, close, "Daily breakout long")
                context.long_triggered = True

        if not context.short_triggered and context.bias != BIAS_LONG:
            if close < short_trigger:
                self._try_enter_position(context, Sides.Sell, close, "Daily breakout short")
                context.short_triggered = True

    def _try_enter_position(self, context, side, price, reason):
        if context.exit_in_progress:
            return

        volume = self._adjust_volume_for_limits(self.TradeVolume)

        if volume <= 0:
            return

        self._register_market_order(context, side, volume, reason)

        context.entry_side = side
        context.entry_price = price
        context.stop_price = None
        context.take_profit_price = None
        context.exit_in_progress = False

        self.LogInfo("[{0}] Enter {1} at {2} vol={3}. Reason={4}".format(
            context.alias, side, price, volume, reason))

    def _manage_open_position(self, context, position, close_price):
        if context.entry_side is None:
            return

        is_long = position > 0

        if context.stop_price is None or context.take_profit_price is None:
            entry_price = context.entry_price if context.entry_price is not None else close_price
            stop_offset = entry_price * (self.StopLossPercent / 100.0)
            take_offset = entry_price * (self.TakeProfitPercent / 100.0)

            if is_long:
                context.stop_price = entry_price - stop_offset
                context.take_profit_price = entry_price + take_offset
            else:
                context.stop_price = entry_price + stop_offset
                context.take_profit_price = entry_price - take_offset

        if context.exit_in_progress:
            return

        if is_long:
            if context.stop_price is not None and close_price <= context.stop_price:
                self._exit_position(context, position, Sides.Sell, "StopLoss at {0}".format(context.stop_price))
                return

            if context.take_profit_price is not None and close_price >= context.take_profit_price:
                self._exit_position(context, position, Sides.Sell, "TakeProfit at {0}".format(context.take_profit_price))
        else:
            volume = abs(position)

            if context.stop_price is not None and close_price >= context.stop_price:
                self._exit_position(context, volume, Sides.Buy, "StopLoss at {0}".format(context.stop_price))
                return

            if context.take_profit_price is not None and close_price <= context.take_profit_price:
                self._exit_position(context, volume, Sides.Buy, "TakeProfit at {0}".format(context.take_profit_price))

    def _exit_position(self, context, volume, side, reason):
        if volume <= 0:
            return

        context.exit_in_progress = True

        self._register_market_order(context, side, volume, reason)

        self.LogInfo("[{0}] Exit {1} vol={2}. Reason={3}".format(context.alias, side, volume, reason))

    def _register_market_order(self, context, side, volume, reason):
        order = Order()
        order.Security = context.security
        order.Portfolio = self.Portfolio
        order.Side = side
        order.Volume = volume
        order.Type = OrderTypes.Market
        order.Comment = "{0}:{1}".format(context.alias, reason)
        self.RegisterOrder(order)

    def _adjust_volume_for_limits(self, desired):
        """Clamp one entry between the volume bounds and against the aggregate exposure."""
        if desired <= 0:
            return 0.0

        volume = min(desired, self.MaxTradeVolume)

        if volume < self.MinTradeVolume:
            return 0.0

        total_exposure = 0.0

        for context in self._contexts:
            total_exposure += abs(self._position_of(context))

        remaining = self.MaxAggregateVolume - total_exposure

        if remaining <= 0:
            return 0.0

        return min(volume, remaining)

    def _update_position_snapshot(self, context, price, position):
        if position == context.last_known_position:
            return

        if position == 0:
            context.reset_position()
            return

        context.last_known_position = position
        context.entry_side = Sides.Buy if position > 0 else Sides.Sell
        context.entry_price = price
        context.exit_in_progress = False

        stop_offset = price * (self.StopLossPercent / 100.0)
        take_offset = price * (self.TakeProfitPercent / 100.0)

        if position > 0:
            context.stop_price = price - stop_offset
            context.take_profit_price = price + take_offset
        else:
            context.stop_price = price + stop_offset
            context.take_profit_price = price - take_offset

    def _position_of(self, context):
        value = self.GetPositionValue(context.security, self.Portfolio)
        return float(value) if value is not None else 0.0

    def CreateClone(self):
        return ch2010_structure_strategy()
