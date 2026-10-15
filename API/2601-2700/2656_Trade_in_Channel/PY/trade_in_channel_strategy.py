import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import DonchianChannels, AverageTrueRange


class trade_in_channel_strategy(Strategy):
    """Channel breakout reversal strategy based on Donchian channel and ATR stops with trailing."""

    def __init__(self):
        super(trade_in_channel_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Channel Period", "Donchian channel lookback", "Channel")
        self._atr_period = self.Param("AtrPeriod", 4) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Average True Range length", "Volatility")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 30.0) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for signals", "General")

        self._prev_upper = None
        self._prev_lower = None
        self._prev_close = None
        self._long_entry = None
        self._short_entry = None
        self._long_stop = None
        self._short_stop = None
        self._long_best = None
        self._short_best = None
        self._long_trail = None
        self._short_trail = None
        self._price_step = 1.0

    @property
    def ChannelPeriod(self):
        return int(self._channel_period.Value)
    @property
    def AtrPeriod(self):
        return int(self._atr_period.Value)
    @property
    def TrailingStopPips(self):
        return float(self._trailing_stop_pips.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(trade_in_channel_strategy, self).OnStarted2(time)

        sec = self.Security
        ps = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        self._price_step = ps

        self._prev_upper = None
        self._prev_lower = None
        self._prev_close = None
        self._reset_long()
        self._reset_short()

        self._donchian = DonchianChannels()
        self._donchian.Length = self.ChannelPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._donchian, self._atr, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._donchian)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, donchian_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._donchian.IsFormed or not self._atr.IsFormed:
            return

        upper = float(donchian_val.UpperBand) if donchian_val.UpperBand is not None else None
        lower = float(donchian_val.LowerBand) if donchian_val.LowerBand is not None else None

        if upper is None or lower is None:
            return

        if not atr_val.IsFinal:
            return

        atr = float(atr_val)

        if self._prev_upper is None or self._prev_lower is None or self._prev_close is None:
            self._prev_upper = upper
            self._prev_lower = lower
            self._prev_close = float(candle.ClosePrice)
            return

        pivot = (upper + lower + self._prev_close) / 3.0
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        closed_long = self._manage_long(candle, upper, self._prev_upper)
        closed_short = self._manage_short(candle, lower, self._prev_lower)

        if self.Position == 0 and not closed_long and not closed_short:
            self._evaluate_entries(candle, upper, lower, self._prev_upper, self._prev_lower, self._prev_close, pivot, atr)

        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_close = c

    def _manage_long(self, candle, upper, prev_upper):
        if self.Position <= 0:
            return False

        lo = float(candle.LowPrice)
        h = float(candle.HighPrice)

        if self._long_stop is not None and lo <= self._long_stop:
            self.SellMarket()
            self._reset_long()
            return True

        if upper == prev_upper and h >= upper:
            self.SellMarket()
            self._reset_long()
            return True

        return self._apply_long_trailing(candle)

    def _manage_short(self, candle, lower, prev_lower):
        if self.Position >= 0:
            return False

        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self._short_stop is not None and h >= self._short_stop:
            self.BuyMarket()
            self._reset_short()
            return True

        if lower == prev_lower and lo <= lower:
            self.BuyMarket()
            self._reset_short()
            return True

        return self._apply_short_trailing(candle)

    def _apply_long_trailing(self, candle):
        if self.Position <= 0:
            return False

        offset = self.TrailingStopPips * self._price_step
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if offset <= 0 or self._long_entry is None:
            self._long_best = h
            return False

        self._long_best = max(self._long_best, h) if self._long_best is not None else h

        if self._long_best is not None and self._long_best - self._long_entry > offset:
            new_level = self._long_best - offset
            if self._long_trail is None or new_level > self._long_trail:
                self._long_trail = new_level
            if self._long_trail is not None and lo <= self._long_trail:
                self.SellMarket()
                self._reset_long()
                return True

        return False

    def _apply_short_trailing(self, candle):
        if self.Position >= 0:
            return False

        offset = self.TrailingStopPips * self._price_step
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if offset <= 0 or self._short_entry is None:
            self._short_best = lo
            return False

        self._short_best = min(self._short_best, lo) if self._short_best is not None else lo

        if self._short_best is not None and self._short_entry - self._short_best > offset:
            new_level = self._short_best + offset
            if self._short_trail is None or new_level < self._short_trail:
                self._short_trail = new_level
            if self._short_trail is not None and h >= self._short_trail:
                self.BuyMarket()
                self._reset_short()
                return True

        return False

    def _evaluate_entries(self, candle, upper, lower, prev_upper, prev_lower, prev_close, pivot, atr):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        resistance_flat = upper == prev_upper
        support_flat = lower == prev_lower

        should_short = resistance_flat and (h >= upper or (prev_close < upper and prev_close > pivot))
        should_long = support_flat and (lo <= lower or (prev_close > lower and prev_close < pivot))

        if should_long:
            self.BuyMarket()
            self._long_entry = c
            self._long_best = c
            self._long_trail = None
            self._long_stop = lower - atr
            self._reset_short()
        elif should_short:
            self.SellMarket()
            self._short_entry = c
            self._short_best = c
            self._short_trail = None
            self._short_stop = upper + atr
            self._reset_long()

    def _reset_long(self):
        self._long_entry = None
        self._long_stop = None
        self._long_best = None
        self._long_trail = None

    def _reset_short(self):
        self._short_entry = None
        self._short_stop = None
        self._short_best = None
        self._short_trail = None

    def OnReseted(self):
        super(trade_in_channel_strategy, self).OnReseted()
        self._prev_upper = None
        self._prev_lower = None
        self._prev_close = None
        self._reset_long()
        self._reset_short()
        self._price_step = 1.0

    def CreateClone(self):
        return trade_in_channel_strategy()
