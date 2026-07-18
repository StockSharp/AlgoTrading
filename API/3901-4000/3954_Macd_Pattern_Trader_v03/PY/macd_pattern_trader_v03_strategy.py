import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    MovingAverageConvergenceDivergence, ExponentialMovingAverage,
    SimpleMovingAverage
)

class macd_pattern_trader_v03_strategy(Strategy):
    def __init__(self):
        super(macd_pattern_trader_v03_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Time frame used for calculations", "General")
        self._fast_ema_length = self.Param("FastEmaLength", 5).SetDisplay("Fast EMA", "Fast period used inside MACD", "MACD")
        self._slow_ema_length = self.Param("SlowEmaLength", 13).SetDisplay("Slow EMA", "Slow period used inside MACD", "MACD")
        self._upper_threshold = self.Param("UpperThreshold", 50.0).SetDisplay("Upper Threshold", "Level that confirms bearish exhaustion", "MACD")
        self._upper_activation = self.Param("UpperActivation", 30.0).SetDisplay("Upper Activation", "Level that arms the bearish pattern", "MACD")
        self._lower_threshold = self.Param("LowerThreshold", -50.0).SetDisplay("Lower Threshold", "Level that confirms bullish exhaustion", "MACD")
        self._lower_activation = self.Param("LowerActivation", -30.0).SetDisplay("Lower Activation", "Level that arms the bullish pattern", "MACD")
        self._ema_one_length = self.Param("EmaOneLength", 7).SetDisplay("EMA 1", "Short EMA used for scaling out", "Management")
        self._ema_two_length = self.Param("EmaTwoLength", 21).SetDisplay("EMA 2", "Second EMA used for scaling out", "Management")
        self._sma_length = self.Param("SmaLength", 98).SetDisplay("SMA", "Simple moving average used for scaling out", "Management")
        self._ema_four_length = self.Param("EmaFourLength", 365).SetDisplay("EMA 4", "Slow EMA used for scaling out", "Management")
        self._profit_threshold = self.Param("ProfitThreshold", 5.0).SetDisplay("Profit Threshold", "Unrealized PnL required before scaling out", "Management")
        self._previous_macd = None
        self._older_macd = None
        self._entry_price = 0.0
        self._is_above_upper_activation = False
        self._first_upper_drop_confirmed = False
        self._second_upper_drop_confirmed = False
        self._sell_ready = False
        self._first_upper_peak = 0.0
        self._second_upper_peak = 0.0
        self._is_below_lower_activation = False
        self._first_lower_rise_confirmed = False
        self._second_lower_rise_confirmed = False
        self._buy_ready = False
        self._first_lower_trough = 0.0
        self._second_lower_trough = 0.0
        self._ema_two_value = None
        self._sma_value = None
        self._ema_four_value = None
        self._previous_candle = None
        self._long_scale_stage = 0
        self._short_scale_stage = 0
        self._initial_long_position = 0.0
        self._initial_short_position = 0.0

    @property
    def CandleType(self): return self._candle_type.Value
    @property
    def FastEmaLength(self): return self._fast_ema_length.Value
    @property
    def SlowEmaLength(self): return self._slow_ema_length.Value
    @property
    def UpperThreshold(self): return self._upper_threshold.Value
    @property
    def UpperActivation(self): return self._upper_activation.Value
    @property
    def LowerThreshold(self): return self._lower_threshold.Value
    @property
    def LowerActivation(self): return self._lower_activation.Value
    @property
    def EmaOneLength(self): return self._ema_one_length.Value
    @property
    def EmaTwoLength(self): return self._ema_two_length.Value
    @property
    def SmaLength(self): return self._sma_length.Value
    @property
    def EmaFourLength(self): return self._ema_four_length.Value
    @property
    def ProfitThreshold(self): return self._profit_threshold.Value

    def OnStarted2(self, time):
        super(macd_pattern_trader_v03_strategy, self).OnStarted2(time)
        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.FastEmaLength
        macd.LongMa.Length = self.SlowEmaLength
        ema_one = ExponentialMovingAverage()
        ema_one.Length = self.EmaOneLength
        ema_two = ExponentialMovingAverage()
        ema_two.Length = self.EmaTwoLength
        sma = SimpleMovingAverage()
        sma.Length = self.SmaLength
        ema_four = ExponentialMovingAverage()
        ema_four.Length = self.EmaFourLength
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(macd, ema_one, ema_two, sma, ema_four, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, macd_main, ema_one, ema_two, sma, ema_four):
        if candle.State != CandleStates.Finished:
            return
        self._ema_two_value = float(ema_two)
        self._sma_value = float(sma)
        self._ema_four_value = float(ema_four)
        mc = float(macd_main)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._cache_macd(mc)
            self._previous_candle = candle
            return
        if self._previous_macd is None or self._older_macd is None:
            self._cache_macd(mc)
            self._previous_candle = candle
            return
        mp = self._previous_macd
        mp2 = self._older_macd
        self._evaluate_sell_pattern(mc, mp, mp2)
        self._evaluate_buy_pattern(mc, mp, mp2)
        self._manage_open_position(candle)
        self._cache_macd(mc)
        self._previous_candle = candle

    def _evaluate_sell_pattern(self, mc, mp, mp2):
        ua = float(self.UpperActivation)
        ut = float(self.UpperThreshold)
        if mc > ua:
            self._is_above_upper_activation = True
        if self._is_above_upper_activation and mc < mp and mp > mp2 and mp > self._first_upper_peak and not self._first_upper_drop_confirmed:
            self._first_upper_peak = mp
        if self._first_upper_peak > 0 and mc < ut:
            self._first_upper_drop_confirmed = True
        if mc < ua:
            self._reset_sell_pattern()
            return
        if self._first_upper_drop_confirmed and mc > ut and mc < mp and mp > mp2 and mp > self._first_upper_peak and mp > self._second_upper_peak and not self._second_upper_drop_confirmed:
            self._second_upper_peak = mp
        if self._second_upper_peak > 0 and mc < ut:
            self._second_upper_drop_confirmed = True
        if self._second_upper_drop_confirmed and mc < ut and mp < ut and mp2 < ut and mc < mp and mp > mp2 and mp < self._second_upper_peak:
            self._sell_ready = True
        if not self._sell_ready:
            return
        self._enter_short()

    def _evaluate_buy_pattern(self, mc, mp, mp2):
        la = float(self.LowerActivation)
        lt = float(self.LowerThreshold)
        if mc < la:
            self._is_below_lower_activation = True
        if self._is_below_lower_activation and mc > mp and mp < mp2 and mp < self._first_lower_trough and not self._first_lower_rise_confirmed:
            self._first_lower_trough = mp
        if self._first_lower_trough < 0 and mc > lt:
            self._first_lower_rise_confirmed = True
        if mc > la:
            self._reset_buy_pattern()
            return
        if self._first_lower_rise_confirmed and mc < lt and mc > mp and mp < mp2 and mp < self._first_lower_trough and mp < self._second_lower_trough and not self._second_lower_rise_confirmed:
            self._second_lower_trough = mp
        if self._second_lower_trough < 0 and mc > lt:
            self._second_lower_rise_confirmed = True
        if self._second_lower_rise_confirmed and mc > lt and mp > lt and mp2 > lt and mc > mp and mp < mp2 and mp > self._second_lower_trough:
            self._buy_ready = True
        if not self._buy_ready:
            return
        self._enter_long()

    def _enter_short(self):
        current_pos = self.Position
        flatten_vol = float(current_pos) if current_pos > 0 else 0.0
        if flatten_vol > 0:
            self.SellMarket(flatten_vol)
        entry_vol = float(self.Volume) + max(0.0, float(self.Position))
        if entry_vol <= 0:
            self._reset_sell_pattern()
            self._sell_ready = False
            return
        self.SellMarket(entry_vol)
        self._entry_price = float(self._previous_candle.ClosePrice) if self._previous_candle is not None else 0.0
        self._initial_short_position = abs(float(self.Position))
        self._short_scale_stage = 0
        self._long_scale_stage = 0
        self._sell_ready = False
        self._reset_sell_pattern()
        self._reset_buy_pattern()

    def _enter_long(self):
        current_pos = self.Position
        flatten_vol = -float(current_pos) if current_pos < 0 else 0.0
        if flatten_vol > 0:
            self.BuyMarket(flatten_vol)
        entry_vol = float(self.Volume) + max(0.0, -float(self.Position))
        if entry_vol <= 0:
            self._reset_buy_pattern()
            self._buy_ready = False
            return
        self.BuyMarket(entry_vol)
        self._entry_price = float(self._previous_candle.ClosePrice) if self._previous_candle is not None else 0.0
        self._initial_long_position = max(0.0, float(self.Position))
        self._long_scale_stage = 0
        self._short_scale_stage = 0
        self._buy_ready = False
        self._reset_buy_pattern()
        self._reset_sell_pattern()

    def _manage_open_position(self, candle):
        if self.Position == 0:
            self._long_scale_stage = 0
            self._short_scale_stage = 0
            self._initial_long_position = 0.0
            self._initial_short_position = 0.0
            return
        prev_c = self._previous_candle
        if prev_c is None:
            return
        pt = float(self.ProfitThreshold)
        if pt <= 0:
            return
        unrealized = self._get_unrealized_pnl(candle)
        if unrealized < pt:
            return
        if self.Position > 0:
            if self._ema_two_value is not None and float(prev_c.ClosePrice) > self._ema_two_value and self._long_scale_stage == 0:
                v = min(float(self.Position), self._initial_long_position / 3.0)
                if v > 0:
                    self.SellMarket(v)
                    self._long_scale_stage = 1
            if self._sma_value is not None and self._ema_four_value is not None and float(prev_c.HighPrice) > (self._sma_value + self._ema_four_value) / 2.0 and self._long_scale_stage == 1:
                v = min(float(self.Position), self._initial_long_position / 2.0)
                if v > 0:
                    self.SellMarket(v)
                    self._long_scale_stage = 2
        elif self.Position < 0:
            short_pos = -float(self.Position)
            if self._ema_two_value is not None and float(prev_c.ClosePrice) < self._ema_two_value and self._short_scale_stage == 0:
                v = min(short_pos, self._initial_short_position / 3.0)
                if v > 0:
                    self.BuyMarket(v)
                    self._short_scale_stage = 1
            if self._sma_value is not None and self._ema_four_value is not None and float(prev_c.LowPrice) < (self._sma_value + self._ema_four_value) / 2.0 and self._short_scale_stage == 1:
                v = min(short_pos, self._initial_short_position / 2.0)
                if v > 0:
                    self.BuyMarket(v)
                    self._short_scale_stage = 2

    def _cache_macd(self, macd_value):
        self._older_macd = self._previous_macd
        self._previous_macd = macd_value

    def _get_unrealized_pnl(self, candle):
        if self.Position == 0 or self._entry_price == 0:
            return 0.0
        diff = float(candle.ClosePrice) - self._entry_price
        return diff * float(self.Position)

    def _reset_sell_pattern(self):
        self._is_above_upper_activation = False
        self._first_upper_drop_confirmed = False
        self._second_upper_drop_confirmed = False
        self._sell_ready = False
        self._first_upper_peak = 0.0
        self._second_upper_peak = 0.0

    def _reset_buy_pattern(self):
        self._is_below_lower_activation = False
        self._first_lower_rise_confirmed = False
        self._second_lower_rise_confirmed = False
        self._buy_ready = False
        self._first_lower_trough = 0.0
        self._second_lower_trough = 0.0

    def CreateClone(self):
        return macd_pattern_trader_v03_strategy()
