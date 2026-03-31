import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    MovingAverageConvergenceDivergenceSignal,
    SimpleMovingAverage,
    AverageDirectionalIndex,
    Lowest,
    Highest,
    DecimalIndicatorValue,
)


class har_veste_r_strategy(Strategy):
    """Trend strategy combining MACD momentum, MA proximity, ADX filter with partial profit taking."""

    def __init__(self):
        super(har_veste_r_strategy, self).__init__()

        self._macd_fast = self.Param("MacdFast", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast EMA", "Short EMA period for MACD", "MACD")
        self._macd_slow = self.Param("MacdSlow", 24) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow EMA", "Long EMA period for MACD", "MACD")
        self._macd_signal_param = self.Param("MacdSignal", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal", "Signal averaging period", "MACD")
        self._macd_lookback = self.Param("MacdLookback", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Lookback", "Bars to confirm MACD sign change", "MACD")
        self._sma_fast_length = self.Param("SmaFastLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast SMA", "First moving average length", "Moving Averages")
        self._sma_slow_length = self.Param("SmaSlowLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow SMA", "Second moving average length", "Moving Averages")
        self._min_indentation = self.Param("MinIndentation", 500.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Indentation", "Distance from moving averages in pips", "Trading")
        self._stop_lookback = self.Param("StopLookback", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Lookback", "Bars for stop loss calculation", "Risk")
        self._use_adx = self.Param("UseAdxFilter", False) \
            .SetDisplay("Use ADX", "Enable ADX trend filter", "ADX")
        self._adx_buy_level = self.Param("AdxBuyLevel", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Buy Level", "Minimum ADX strength for longs", "ADX")
        self._adx_sell_level = self.Param("AdxSellLevel", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Sell Level", "Minimum ADX strength for shorts", "ADX")
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "ADX calculation length", "ADX")
        self._half_close_ratio = self.Param("HalfCloseRatio", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("Half Close Ratio", "Multiplier applied to stop distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")

        self._macd_history = []
        self._last_lowest = None
        self._last_highest = None
        self._long_entry = None
        self._long_stop = None
        self._long_stop_moved = False
        self._short_entry = None
        self._short_stop = None
        self._short_stop_moved = False

    @property
    def MacdFast(self):
        return int(self._macd_fast.Value)
    @property
    def MacdSlow(self):
        return int(self._macd_slow.Value)
    @property
    def MacdSignal(self):
        return int(self._macd_signal_param.Value)
    @property
    def MacdLookback(self):
        return int(self._macd_lookback.Value)
    @property
    def SmaFastLength(self):
        return int(self._sma_fast_length.Value)
    @property
    def SmaSlowLength(self):
        return int(self._sma_slow_length.Value)
    @property
    def MinIndentation(self):
        return float(self._min_indentation.Value)
    @property
    def StopLookback(self):
        return int(self._stop_lookback.Value)
    @property
    def UseAdxFilter(self):
        return self._use_adx.Value
    @property
    def AdxBuyLevel(self):
        return float(self._adx_buy_level.Value)
    @property
    def AdxSellLevel(self):
        return float(self._adx_sell_level.Value)
    @property
    def AdxPeriod(self):
        return int(self._adx_period.Value)
    @property
    def HalfCloseRatio(self):
        return int(self._half_close_ratio.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(har_veste_r_strategy, self).OnStarted2(time)

        self._macd_history = []
        self._last_lowest = None
        self._last_highest = None
        self._reset_long_state()
        self._reset_short_state()

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.MacdFast
        self._macd.Macd.LongMa.Length = self.MacdSlow
        self._macd.SignalMa.Length = self.MacdSignal

        self._sma_fast_ind = SimpleMovingAverage()
        self._sma_fast_ind.Length = self.SmaFastLength
        self._sma_slow_ind = SimpleMovingAverage()
        self._sma_slow_ind.Length = self.SmaSlowLength
        self._adx_ind = AverageDirectionalIndex()
        self._adx_ind.Length = self.AdxPeriod
        self._lowest_ind = Lowest()
        self._lowest_ind.Length = self.StopLookback
        self._highest_ind = Highest()
        self._highest_ind.Length = self.StopLookback

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._macd, self._sma_fast_ind, self._sma_slow_ind, self._adx_ind, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma_fast_ind)
            self.DrawIndicator(area, self._sma_slow_ind)
            self.DrawIndicator(area, self._macd)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, macd_value, sma_fast_value, sma_slow_value, adx_value):
        if candle.State != CandleStates.Finished:
            return

        # Update lowest/highest for stop calculation
        low_iv = DecimalIndicatorValue(self._lowest_ind, candle.LowPrice, candle.ServerTime)
        low_iv.IsFinal = True
        low_val = self._lowest_ind.Process(low_iv)
        if low_val.IsFormed:
            self._last_lowest = float(low_val.Value)

        high_iv = DecimalIndicatorValue(self._highest_ind, candle.HighPrice, candle.ServerTime)
        high_iv.IsFinal = True
        high_val = self._highest_ind.Process(high_iv)
        if high_val.IsFormed:
            self._last_highest = float(high_val.Value)

        if not macd_value.IsFinal or not sma_fast_value.IsFinal or not sma_slow_value.IsFinal:
            return

        macd_main_n = macd_value.Macd
        if macd_main_n is None:
            return

        macd_main = float(macd_main_n)
        sma_fast = float(sma_fast_value.Value)
        sma_slow = float(sma_slow_value.Value)

        adx_strength = None
        if self.UseAdxFilter:
            if not adx_value.IsFinal:
                return
            adx_ma = adx_value.MovingAverage
            if adx_ma is None:
                return
            adx_strength = float(adx_ma)

        self._macd_history.append(macd_main)
        while len(self._macd_history) > self.MacdLookback:
            self._macd_history.pop(0)

        indentation = self._get_indentation()
        close = float(candle.ClosePrice)

        if macd_main == 0.0 or sma_fast == 0.0 or sma_slow == 0.0 or close <= 0.0:
            return

        # Manage partial exits and break-even logic
        self._manage_open_positions(close, sma_fast, indentation)

        if not self._macd.IsFormed or not self._sma_fast_ind.IsFormed or not self._sma_slow_ind.IsFormed:
            return

        if len(self._macd_history) < self.MacdLookback:
            return

        had_negative = self._has_negative_macd()
        had_positive = self._has_positive_macd()

        adx_buy_ok = not self.UseAdxFilter
        adx_sell_ok = not self.UseAdxFilter
        if self.UseAdxFilter and adx_strength is not None:
            adx_buy_ok = adx_strength >= self.AdxBuyLevel
            adx_sell_ok = adx_strength >= self.AdxSellLevel

        ok_buy = close < sma_slow
        ok_sell = close > sma_slow

        if (macd_main > 0 and had_negative and adx_buy_ok and ok_buy
                and close + indentation > sma_fast and close + indentation > sma_slow
                and self.Position <= 0 and self._last_lowest is not None):
            self.BuyMarket()
            self._long_entry = close
            self._long_stop = self._last_lowest
            self._long_stop_moved = False
            self._reset_short_state()
        elif (macd_main < 0 and had_positive and adx_sell_ok and ok_sell
                and close - indentation < sma_fast and close - indentation < sma_slow
                and self.Position >= 0 and self._last_highest is not None):
            self.SellMarket()
            self._short_entry = close
            self._short_stop = self._last_highest
            self._short_stop_moved = False
            self._reset_long_state()

    def _manage_open_positions(self, close, sma_fast, indentation):
        if self.Position > 0 and self._long_entry is not None and self._long_stop is not None:
            distance = abs(self._long_entry - self._long_stop)
            if distance > 0:
                target = self._long_entry + distance * self.HalfCloseRatio
                if not self._long_stop_moved and close > target:
                    self.SellMarket()
                    self._long_stop = self._long_entry
                    self._long_stop_moved = True
                elif self._long_stop_moved and sma_fast > close - indentation:
                    self.SellMarket()
                    self._reset_long_state()
        elif self.Position <= 0:
            self._reset_long_state()

        if self.Position < 0 and self._short_entry is not None and self._short_stop is not None:
            distance = abs(self._short_entry - self._short_stop)
            if distance > 0:
                target = self._short_entry - distance * self.HalfCloseRatio
                if not self._short_stop_moved and close < target:
                    self.BuyMarket()
                    self._short_stop = self._short_entry
                    self._short_stop_moved = True
                elif self._short_stop_moved and sma_fast < close - indentation:
                    self.BuyMarket()
                    self._reset_short_state()
        elif self.Position >= 0:
            self._reset_short_state()

    def _has_negative_macd(self):
        for v in self._macd_history:
            if v < 0:
                return True
        return False

    def _has_positive_macd(self):
        for v in self._macd_history:
            if v > 0:
                return True
        return False

    def _get_indentation(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0
        if step <= 0:
            return self.MinIndentation
        decimals = int(sec.Decimals) if sec is not None and sec.Decimals is not None else 0
        factor = 10.0 if (decimals == 3 or decimals == 5) else 1.0
        return self.MinIndentation * step * factor

    def _reset_long_state(self):
        self._long_entry = None
        self._long_stop = None
        self._long_stop_moved = False

    def _reset_short_state(self):
        self._short_entry = None
        self._short_stop = None
        self._short_stop_moved = False

    def OnReseted(self):
        super(har_veste_r_strategy, self).OnReseted()
        self._macd_history = []
        self._last_lowest = None
        self._last_highest = None
        self._reset_long_state()
        self._reset_short_state()

    def CreateClone(self):
        return har_veste_r_strategy()
