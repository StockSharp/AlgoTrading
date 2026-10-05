import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class heiken_ashi_supertrend_atr_sl_strategy(Strategy):
    """
    Heiken Ashi Supertrend ATR-SL strategy.
    A green Heiken Ashi candle without a lower wick buys and a red one without an upper wick sells; with UseSupertrend the
    Supertrend (AtrPeriod, AtrFactor) must point the same way. A long closes on a red candle without an upper wick and a short on a
    green candle without a lower wick, reversing when the entry filter agrees. With UseHardStop the stop sits
    StopLossAtrMultiplier ATRs from the entry; with UseBreakEven it moves to the entry price once price has run
    BreakEvenAtrMultiplier ATRs in favour. ATR values are taken at entry.
    """

    def __init__(self):
        super(heiken_ashi_supertrend_atr_sl_strategy, self).__init__()
        self._use_supertrend = self.Param("UseSupertrend", True).SetDisplay("Use Supertrend", "Require the Supertrend direction for entries", "Filters")
        self._atr_period = self.Param("AtrPeriod", 10).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Indicators")
        self._atr_factor = self.Param("AtrFactor", 3.0).SetGreaterThanZero().SetDisplay("ATR Factor", "Supertrend ATR factor", "Indicators")
        self._use_break_even = self.Param("UseBreakEven", False).SetDisplay("Use Break Even", "Move the stop to the entry price after a favourable move", "Risk")
        self._break_even_atr_multiplier = self.Param("BreakEvenAtrMultiplier", 1.0).SetNotNegative().SetDisplay("Break Even ATR", "Favourable move in ATRs that activates the break even", "Risk")
        self._use_hard_stop = self.Param("UseHardStop", False).SetDisplay("Use Hard Stop", "Use an ATR stop loss from the entry", "Risk")
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 2.0).SetNotNegative().SetDisplay("Stop Loss ATR", "Stop loss distance in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._ha_open = None
        self._ha_close = None
        self._upper_band = None
        self._lower_band = None
        self._prev_close = None
        self._trend = 1
        self._entry_price = 0.0
        self._entry_atr = 0.0
        self._stop_price = None

    def OnReseted(self):
        super(heiken_ashi_supertrend_atr_sl_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(heiken_ashi_supertrend_atr_sl_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _enter(self, is_long, close, atr):
        if is_long:
            self.BuyMarket(self.Volume + abs(self.Position))
        else:
            self.SellMarket(self.Volume + abs(self.Position))

        self._entry_price = close
        self._entry_atr = atr
        sl_mult = float(self._stop_loss_atr_multiplier.Value)
        if self._use_hard_stop.Value and sl_mult > 0:
            self._stop_price = close - sl_mult * atr if is_long else close + sl_mult * atr
        else:
            self._stop_price = None

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        atr = float(atr_value)

        # Heiken Ashi candle.
        ha_close = (open_price + high + low + close) / 4.0
        if self._ha_open is not None and self._ha_close is not None:
            ha_open = (self._ha_open + self._ha_close) / 2.0
        else:
            ha_open = (open_price + close) / 2.0
        ha_high = max(high, ha_open, ha_close)
        ha_low = min(low, ha_open, ha_close)

        self._ha_open = ha_open
        self._ha_close = ha_close

        # Supertrend on the regular candles.
        factor = float(self._atr_factor.Value)
        hl2 = (high + low) / 2.0
        lower = hl2 - factor * atr
        upper = hl2 + factor * atr

        if self._lower_band is not None and self._prev_close is not None and self._prev_close > self._lower_band:
            lower = max(lower, self._lower_band)
        if self._upper_band is not None and self._prev_close is not None and self._prev_close < self._upper_band:
            upper = min(upper, self._upper_band)

        if self._trend < 0 and self._upper_band is not None and close > self._upper_band:
            self._trend = 1
        elif self._trend > 0 and self._lower_band is not None and close < self._lower_band:
            self._trend = -1

        self._lower_band = lower
        self._upper_band = upper
        self._prev_close = close

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        green_candle = ha_close > ha_open and ha_low >= ha_open
        red_candle = ha_close < ha_open and ha_high <= ha_open
        use_st = self._use_supertrend.Value

        long_signal = green_candle and ((not use_st) or self._trend > 0)
        short_signal = red_candle and ((not use_st) or self._trend < 0)
        be_mult = float(self._break_even_atr_multiplier.Value)

        if self.Position > 0:
            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket(self.Position)
                return
            if red_candle:
                if short_signal:
                    self._enter(False, close, atr)
                else:
                    self.SellMarket(self.Position)
                return
            if self._use_break_even.Value and high - self._entry_price >= be_mult * self._entry_atr and (self._stop_price is None or self._stop_price < self._entry_price):
                self._stop_price = self._entry_price
        elif self.Position < 0:
            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket(-self.Position)
                return
            if green_candle:
                if long_signal:
                    self._enter(True, close, atr)
                else:
                    self.BuyMarket(-self.Position)
                return
            if self._use_break_even.Value and self._entry_price - low >= be_mult * self._entry_atr and (self._stop_price is None or self._stop_price > self._entry_price):
                self._stop_price = self._entry_price
        elif long_signal:
            self._enter(True, close, atr)
        elif short_signal:
            self._enter(False, close, atr)

    def CreateClone(self):
        return heiken_ashi_supertrend_atr_sl_strategy()
