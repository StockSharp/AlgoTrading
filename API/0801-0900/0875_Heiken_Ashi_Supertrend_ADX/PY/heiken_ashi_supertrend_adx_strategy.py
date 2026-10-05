import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class heiken_ashi_supertrend_adx_strategy(Strategy):
    """
    Heiken Ashi Supertrend ADX strategy.
    A bullish Heiken Ashi candle without a lower wick buys and a bearish one without an upper wick sells. With UseSupertrend the
    Supertrend (AtrPeriod, SupertrendMultiplier) must point the same way, and with UseAdxFilter the ADX must be above AdxThreshold.
    An opposite Heiken Ashi candle closes the position, reversing it when the entry filters agree. Positions also exit on an ATR
    trailing stop TrailAtrMultiplier ATRs behind the close.
    """

    def __init__(self):
        super(heiken_ashi_supertrend_adx_strategy, self).__init__()
        self._use_supertrend = self.Param("UseSupertrend", True).SetDisplay("Use Supertrend", "Require the Supertrend direction for entries", "Filters")
        self._atr_period = self.Param("AtrPeriod", 10).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period for Supertrend and the trailing stop", "Indicators")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Indicators")
        self._use_adx_filter = self.Param("UseAdxFilter", False).SetDisplay("Use ADX Filter", "Require ADX above the threshold for entries", "Filters")
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "ADX period", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 25.0).SetDisplay("ADX Threshold", "Minimum ADX value for entries", "Filters")
        self._trail_atr_multiplier = self.Param("TrailAtrMultiplier", 2.0).SetNotNegative().SetDisplay("Trail ATR Multiplier", "Trailing stop distance in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
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
        self._trail_stop = 0.0

    def OnReseted(self):
        super(heiken_ashi_supertrend_adx_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(heiken_ashi_supertrend_adx_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, adx, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _process_candle(self, candle, atr_value, adx_value):
        if candle.State != CandleStates.Finished:
            return

        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

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

        if not atr_value.IsFormed:
            self._prev_close = close
            return

        atr = float(atr_value.GetValue[Decimal](None))
        multiplier = float(self._supertrend_multiplier.Value)

        # Supertrend on the regular candles.
        hl2 = (high + low) / 2.0
        lower = hl2 - multiplier * atr
        upper = hl2 + multiplier * atr

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

        if not adx_value.IsFormed or adx_value.MovingAverage is None:
            return
        adx = float(adx_value.MovingAverage)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        bullish_candle = ha_close > ha_open and ha_low >= ha_open
        bearish_candle = ha_close < ha_open and ha_high <= ha_open
        adx_ok = (not self._use_adx_filter.Value) or adx > float(self._adx_threshold.Value)
        use_st = self._use_supertrend.Value

        long_signal = bullish_candle and ((not use_st) or self._trend > 0) and adx_ok
        short_signal = bearish_candle and ((not use_st) or self._trend < 0) and adx_ok
        trail_mult = float(self._trail_atr_multiplier.Value)
        trail_distance = trail_mult * atr

        if self.Position > 0:
            if trail_mult > 0 and low <= self._trail_stop:
                self.SellMarket(self.Position)
                return
            if bearish_candle:
                if short_signal:
                    self.SellMarket(self.Volume + abs(self.Position))
                    self._trail_stop = close + trail_distance
                else:
                    self.SellMarket(self.Position)
                return
            self._trail_stop = max(self._trail_stop, close - trail_distance)
        elif self.Position < 0:
            if trail_mult > 0 and high >= self._trail_stop:
                self.BuyMarket(-self.Position)
                return
            if bullish_candle:
                if long_signal:
                    self.BuyMarket(self.Volume + abs(self.Position))
                    self._trail_stop = close - trail_distance
                else:
                    self.BuyMarket(-self.Position)
                return
            self._trail_stop = min(self._trail_stop, close + trail_distance)
        elif long_signal:
            self.BuyMarket(self.Volume)
            self._trail_stop = close - trail_distance
        elif short_signal:
            self.SellMarket(self.Volume)
            self._trail_stop = close + trail_distance

    def CreateClone(self):
        return heiken_ashi_supertrend_adx_strategy()
