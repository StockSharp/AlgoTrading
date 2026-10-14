import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, DateTime, DateTimeKind, TimeZoneInfo
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class options_v13_strategy(Strategy):
    """
    Options strategy V1.3.
    A long opens when the short EMA crosses above the long EMA with RSI at or above RsiLongThreshold and candle volume at or above its
    SMA; a short on the opposite cross with RSI at or below RsiShortThreshold. Optionally the close must also be beyond the New York
    opening range. The stop is SlMultiplier times ATR from the entry and the target TpSlRatio times the stop distance. An opposite
    cross closes (or reverses) the position, an optional no-trade window blocks entries and everything is flat at 15:55 New York time.
    """

    def __init__(self):
        super(options_v13_strategy, self).__init__()
        self._ema_short_length = self.Param("EmaShortLength", 8).SetGreaterThanZero().SetDisplay("EMA Short", "Short EMA length", "Indicators")
        self._ema_long_length = self.Param("EmaLongLength", 28).SetGreaterThanZero().SetDisplay("EMA Long", "Long EMA length", "Indicators")
        self._rsi_length = self.Param("RsiLength", 12).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Indicators")
        self._rsi_long_threshold = self.Param("RsiLongThreshold", 50.0).SetDisplay("RSI Long", "Minimum RSI for a long entry", "Indicators")
        self._rsi_short_threshold = self.Param("RsiShortThreshold", 50.0).SetDisplay("RSI Short", "Maximum RSI for a short entry", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR length", "Risk")
        self._sl_multiplier = self.Param("SlMultiplier", 1.4).SetGreaterThanZero().SetDisplay("SL Multiplier", "Stop distance in ATR multiples", "Risk")
        self._tp_sl_ratio = self.Param("TpSlRatio", 4.0).SetGreaterThanZero().SetDisplay("TP/SL Ratio", "Target distance in multiples of the stop distance", "Risk")
        self._volume_ma_length = self.Param("VolumeMaLength", 20).SetGreaterThanZero().SetDisplay("Volume MA Length", "Length of the volume SMA", "Indicators")
        self._use_opening_range = self.Param("UseOpeningRange", False).SetDisplay("Use Opening Range", "Require a close beyond the opening range", "Session")
        self._opening_range_start = self.Param("OpeningRangeStart", TimeSpan(9, 30, 0)).SetDisplay("OR Start", "Opening range start (New York time)", "Session")
        self._opening_range_end = self.Param("OpeningRangeEnd", TimeSpan(10, 0, 0)).SetDisplay("OR End", "Opening range end (New York time)", "Session")
        self._close_time = self.Param("CloseTime", TimeSpan(15, 55, 0)).SetDisplay("Close Time", "Time when positions are closed (New York time)", "Session")
        self._use_no_trade_window = self.Param("UseNoTradeWindow", False).SetDisplay("Use No-Trade Window", "Block entries inside the no-trade window", "Session")
        self._no_trade_start = self.Param("NoTradeStart", TimeSpan(12, 0, 0)).SetDisplay("No-Trade Start", "No-trade window start (New York time)", "Session")
        self._no_trade_end = self.Param("NoTradeEnd", TimeSpan(13, 0, 0)).SetDisplay("No-Trade End", "No-trade window end (New York time)", "Session")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._new_york = TimeZoneInfo.FindSystemTimeZoneById("America/New_York")
        self._volume_ma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_short = None
        self._prev_long = None
        self._or_day = None
        self._or_high = None
        self._or_low = None
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)

    def OnReseted(self):
        super(options_v13_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(options_v13_strategy, self).OnStarted2(time)

        self._reset_state()

        ema_short = ExponentialMovingAverage()
        ema_short.Length = self._ema_short_length.Value
        ema_long = ExponentialMovingAverage()
        ema_long.Length = self._ema_long_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        self._volume_ma = SimpleMovingAverage()
        self._volume_ma.Length = self._volume_ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema_short, ema_long, rsi, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_short)
            self.DrawIndicator(area, ema_long)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_short, ema_long, rsi, atr):
        if candle.State != CandleStates.Finished:
            return

        volume_input = DecimalIndicatorValue(self._volume_ma, candle.TotalVolume, candle.OpenTime)
        volume_input.IsFinal = True
        volume_value = self._volume_ma.Process(volume_input)
        volume_ma = volume_value.GetValue[Decimal](None) if self._volume_ma.IsFormed else None

        utc = DateTime.SpecifyKind(candle.OpenTime.ToUniversalTime(), DateTimeKind.Unspecified)
        ny_time = TimeZoneInfo.ConvertTimeFromUtc(utc, self._new_york)
        ny_day = ny_time.Date
        tod = ny_time.TimeOfDay.Ticks

        if self._or_day is None or ny_day != self._or_day:
            self._or_day = ny_day
            self._or_high = None
            self._or_low = None

        or_start = self._opening_range_start.Value.Ticks
        or_end = self._opening_range_end.Value.Ticks
        in_opening_range = tod >= or_start and tod < or_end
        if in_opening_range:
            self._or_high = candle.HighPrice if self._or_high is None else max(self._or_high, candle.HighPrice)
            self._or_low = candle.LowPrice if self._or_low is None else min(self._or_low, candle.LowPrice)

        prev_short = self._prev_short
        prev_long = self._prev_long
        self._prev_short = ema_short
        self._prev_long = ema_long

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if tod >= self._close_time.Value.Ticks:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
            return

        if self.Position > 0 and (candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price):
            self.SellMarket(self.Position)
            return

        if self.Position < 0 and (candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price):
            self.BuyMarket(-self.Position)
            return

        if prev_short is None or prev_long is None:
            return

        cross_up = prev_short <= prev_long and ema_short > ema_long
        cross_down = prev_short >= prev_long and ema_short < ema_long

        if not cross_up and not cross_down:
            return

        close = candle.ClosePrice
        volume_ok = volume_ma is not None and candle.TotalVolume >= volume_ma
        use_or = self._use_opening_range.Value
        blocked = (self._use_no_trade_window.Value and tod >= self._no_trade_start.Value.Ticks and tod < self._no_trade_end.Value.Ticks) \
            or (use_or and (in_opening_range or tod < or_start))
        stop_distance = atr * Decimal(self._sl_multiplier.Value)
        tp_ratio = Decimal(self._tp_sl_ratio.Value)

        if cross_up:
            or_ok = not use_or or (self._or_high is not None and close > self._or_high)
            if self.Position <= 0 and not blocked and rsi >= Decimal(self._rsi_long_threshold.Value) and volume_ok and or_ok and stop_distance > 0:
                self.BuyMarket(self.Volume + abs(self.Position))
                self._stop_price = close - stop_distance
                self._take_price = close + stop_distance * tp_ratio
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
        else:
            or_ok = not use_or or (self._or_low is not None and close < self._or_low)
            if self.Position >= 0 and not blocked and rsi <= Decimal(self._rsi_short_threshold.Value) and volume_ok and or_ok and stop_distance > 0:
                self.SellMarket(self.Volume + abs(self.Position))
                self._stop_price = close + stop_distance
                self._take_price = close - stop_distance * tp_ratio
            elif self.Position > 0:
                self.SellMarket(self.Position)

    def CreateClone(self):
        return options_v13_strategy()
