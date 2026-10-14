import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class cbc_with_trend_confirmation_and_separate_stop_loss_strategy(Strategy):
    """
    CBC with Trend Confirmation and Separate Stop Loss strategy.
    The color bar change (CBC) state turns bullish when the close breaks above the previous candle's high and bearish when it breaks
    below the previous candle's low. A flip to bullish goes long when the slow EMA is above the daily VWAP, a flip to bearish goes short
    when it is below, reversing an opposite position; with StrongFlipsOnly the flip candle also has to close in its direction. Entries
    are taken only between EntryStartHour and EntryEndHour (UTC). Each entry freezes a target ProfitTargetMultiplier ATRs away and a
    stop at the previous candle's low (long) or high (short).
    """

    def __init__(self):
        super(cbc_with_trend_confirmation_and_separate_stop_loss_strategy, self).__init__()
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk")
        self._profit_target_multiplier = self.Param("ProfitTargetMultiplier", 1.0).SetGreaterThanZero().SetDisplay("Profit Target Multiplier", "ATR multiple of the profit target", "Risk")
        self._strong_flips_only = self.Param("StrongFlipsOnly", True).SetDisplay("Strong Flips Only", "Trade only flips whose candle closes in the flip direction", "Signals")
        self._entry_start_hour = self.Param("EntryStartHour", 10).SetRange(0, 23).SetDisplay("Entry Start Hour", "First entry hour (UTC)", "Session")
        self._entry_end_hour = self.Param("EntryEndHour", 15).SetRange(1, 24).SetDisplay("Entry End Hour", "Hour entries stop (UTC, exclusive)", "Session")
        self._slow_ema_length = self.Param("SlowEmaLength", 21).SetGreaterThanZero().SetDisplay("Slow EMA Length", "Period of the slow trend EMA", "Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_candle = None
        self._cbc_bullish = None
        self._vwap_day = None
        self._vwap_price_volume = Decimal(0)
        self._vwap_volume = Decimal(0)
        self._stop_price = None
        self._target_price = None

    def OnReseted(self):
        super(cbc_with_trend_confirmation_and_separate_stop_loss_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(cbc_with_trend_confirmation_and_separate_stop_loss_strategy, self).OnStarted2(time)

        self._reset_state()

        ema = ExponentialMovingAverage()
        ema.Length = self._slow_ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        # Session VWAP restarts every UTC day.
        day = candle.OpenTime.Date
        if day != self._vwap_day:
            self._vwap_day = day
            self._vwap_price_volume = Decimal(0)
            self._vwap_volume = Decimal(0)

        typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(3)
        self._vwap_price_volume += typical * candle.TotalVolume
        self._vwap_volume += candle.TotalVolume

        prev = self._prev_candle
        self._prev_candle = candle

        if prev is None:
            return

        previous_state = self._cbc_bullish

        if candle.ClosePrice > prev.HighPrice:
            self._cbc_bullish = True
        elif candle.ClosePrice < prev.LowPrice:
            self._cbc_bullish = False

        if not ema_value.IsFormed or not atr_value.IsFormed or self._vwap_volume <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Stop and target frozen at entry.
        if self.Position > 0 and self._stop_price is not None and self._target_price is not None and (candle.LowPrice <= self._stop_price or candle.HighPrice >= self._target_price):
            self.SellMarket(self.Position)
            self._stop_price = None
            self._target_price = None
            return

        if self.Position < 0 and self._stop_price is not None and self._target_price is not None and (candle.HighPrice >= self._stop_price or candle.LowPrice <= self._target_price):
            self.BuyMarket(-self.Position)
            self._stop_price = None
            self._target_price = None
            return

        is_bullish = self._cbc_bullish
        if previous_state is None or is_bullish is None or previous_state == is_bullish:
            return

        hour = candle.OpenTime.Hour
        if hour < self._entry_start_hour.Value or hour >= self._entry_end_hour.Value:
            return

        ema = ema_value.GetValue[Decimal](None)
        vwap = self._vwap_price_volume / self._vwap_volume
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        strong_only = self._strong_flips_only.Value
        multiplier = Decimal(self._profit_target_multiplier.Value)

        if is_bullish and (not strong_only or close > candle.OpenPrice) and ema > vwap and self.Position <= 0 and prev.LowPrice < close:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = prev.LowPrice
            self._target_price = close + atr * multiplier
        elif not is_bullish and (not strong_only or close < candle.OpenPrice) and ema < vwap and self.Position >= 0 and prev.HighPrice > close:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = prev.HighPrice
            self._target_price = close - atr * multiplier

    def CreateClone(self):
        return cbc_with_trend_confirmation_and_separate_stop_loss_strategy()
