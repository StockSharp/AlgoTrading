import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    AverageTrueRange, CommodityChannelIndex, StandardDeviation,
    MovingAverageConvergenceDivergenceSignal
)


class anubis_strategy(Strategy):
    """Anubis: CCI + StdDev on higher TF combined with MACD signals on main TF, with breakeven and stacking."""

    def __init__(self):
        super(anubis_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Order size used for entries", "Trading")
        self._cci_threshold = self.Param("CciThreshold", 80.0) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Threshold", "Absolute CCI level used to detect extremes", "Indicators")
        self._cci_period = self.Param("CciPeriod", 11) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "CCI lookback on the higher timeframe", "Indicators")
        self._stop_loss_pips = self.Param("StopLossPips", 500.0) \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance measured in pips", "Risk")
        self._breakeven_pips = self.Param("BreakevenPips", 300.0) \
            .SetDisplay("Breakeven (pips)", "Distance to move stop to entry", "Risk")
        self._threshold_pips = self.Param("ThresholdPips", 200.0) \
            .SetDisplay("MACD Exit Threshold (pips)", "Extra profit required before MACD exit", "Risk")
        self._take_std_multiplier = self.Param("TakeStdMultiplier", 2.9) \
            .SetGreaterThanZero() \
            .SetDisplay("StdDev Multiplier", "Multiplier for higher timeframe standard deviation", "Risk")
        self._close_atr_multiplier = self.Param("CloseAtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Previous candle range multiplier for exits", "Risk")
        self._spacing_pips = self.Param("SpacingPips", 20.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Entry Spacing (pips)", "Minimum distance between consecutive entries", "Trading")
        self._max_long_positions = self.Param("MaxLongPositions", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Long Entries", "Maximum stacked long positions", "Trading")
        self._max_short_positions = self.Param("MaxShortPositions", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Short Entries", "Maximum stacked short positions", "Trading")
        self._macd_fast_length = self.Param("MacdFastLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Fast Length", "Fast EMA period for MACD", "Indicators")
        self._macd_slow_length = self.Param("MacdSlowLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Slow Length", "Slow EMA period for MACD", "Indicators")
        self._macd_signal_length = self.Param("MacdSignalLength", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("MACD Signal Length", "Signal smoothing for MACD", "Indicators")
        self._atr_length = self.Param("AtrLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR lookback on the main timeframe", "Indicators")
        self._std_fast_length = self.Param("StdFastLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast StdDev Length", "SMA based standard deviation period", "Indicators")
        self._std_slow_length = self.Param("StdSlowLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow StdDev Length", "Secondary standard deviation period used for take-profit", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Signal Candle Type", "Timeframe used for MACD and ATR", "General")

        self._higher_time_frame = DataType.TimeFrame(TimeSpan.FromHours(4))

        self._last_atr = 0.0
        self._atr_ready = False
        self._cci_value = 0.0
        self._std_fast_value = 0.0
        self._std_slow_value = 0.0
        self._higher_ready = False

        self._macd_main_prev1 = 0.0
        self._macd_main_prev2 = 0.0
        self._macd_signal_prev1 = 0.0
        self._macd_signal_prev2 = 0.0
        self._macd_samples = 0

        self._prev_candle_open = 0.0
        self._prev_candle_close = 0.0
        self._has_prev_candle = False

        self._adjusted_point = 0.0
        self._stop_loss_distance = 0.0
        self._breakeven_distance = 0.0
        self._threshold_distance = 0.0
        self._spacing_distance = 0.0

        self._long_stop_price = 0.0
        self._long_take_price = 0.0
        self._long_breakeven_activated = False
        self._long_entries = 0
        self._last_long_signal_time = None
        self._last_long_price = 0.0

        self._short_stop_price = 0.0
        self._short_take_price = 0.0
        self._short_breakeven_activated = False
        self._short_entries = 0
        self._last_short_signal_time = None
        self._last_short_price = 0.0

        self._entry_price = 0.0

    @property
    def TradeVolume(self):
        return float(self._trade_volume.Value)
    @property
    def CciThreshold(self):
        return float(self._cci_threshold.Value)
    @property
    def CciPeriod(self):
        return int(self._cci_period.Value)
    @property
    def StopLossPips(self):
        return float(self._stop_loss_pips.Value)
    @property
    def BreakevenPips(self):
        return float(self._breakeven_pips.Value)
    @property
    def ThresholdPips(self):
        return float(self._threshold_pips.Value)
    @property
    def TakeStdMultiplier(self):
        return float(self._take_std_multiplier.Value)
    @property
    def CloseAtrMultiplier(self):
        return float(self._close_atr_multiplier.Value)
    @property
    def SpacingPips(self):
        return float(self._spacing_pips.Value)
    @property
    def MaxLongPositions(self):
        return int(self._max_long_positions.Value)
    @property
    def MaxShortPositions(self):
        return int(self._max_short_positions.Value)
    @property
    def MacdFastLength(self):
        return int(self._macd_fast_length.Value)
    @property
    def MacdSlowLength(self):
        return int(self._macd_slow_length.Value)
    @property
    def MacdSignalLength(self):
        return int(self._macd_signal_length.Value)
    @property
    def AtrLength(self):
        return int(self._atr_length.Value)
    @property
    def StdFastLength(self):
        return int(self._std_fast_length.Value)
    @property
    def StdSlowLength(self):
        return int(self._std_slow_length.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _init_distances(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 0.0001
        self._adjusted_point = step
        if step > 0 and step < 0.01:
            self._adjusted_point = step * 10.0
        self._stop_loss_distance = self.StopLossPips * self._adjusted_point
        self._breakeven_distance = self.BreakevenPips * self._adjusted_point
        self._threshold_distance = self.ThresholdPips * self._adjusted_point
        self._spacing_distance = self.SpacingPips * self._adjusted_point

    def OnStarted2(self, time):
        super(anubis_strategy, self).OnStarted2(time)

        self._last_atr = 0.0
        self._atr_ready = False
        self._cci_value = 0.0
        self._std_fast_value = 0.0
        self._std_slow_value = 0.0
        self._higher_ready = False
        self._macd_main_prev1 = 0.0
        self._macd_main_prev2 = 0.0
        self._macd_signal_prev1 = 0.0
        self._macd_signal_prev2 = 0.0
        self._macd_samples = 0
        self._prev_candle_open = 0.0
        self._prev_candle_close = 0.0
        self._has_prev_candle = False
        self._long_stop_price = 0.0
        self._long_take_price = 0.0
        self._long_breakeven_activated = False
        self._long_entries = 0
        self._last_long_signal_time = None
        self._last_long_price = 0.0
        self._short_stop_price = 0.0
        self._short_take_price = 0.0
        self._short_breakeven_activated = False
        self._short_entries = 0
        self._last_short_signal_time = None
        self._last_short_price = 0.0
        self._entry_price = 0.0

        self._init_distances()

        self._atr_indicator = AverageTrueRange()
        self._atr_indicator.Length = self.AtrLength
        self._cci_indicator = CommodityChannelIndex()
        self._cci_indicator.Length = self.CciPeriod
        self._fast_std_dev = StandardDeviation()
        self._fast_std_dev.Length = self.StdFastLength
        self._slow_std_dev = StandardDeviation()
        self._slow_std_dev.Length = self.StdSlowLength
        self._macd_indicator = MovingAverageConvergenceDivergenceSignal()
        self._macd_indicator.Macd.ShortMa.Length = self.MacdFastLength
        self._macd_indicator.Macd.LongMa.Length = self.MacdSlowLength
        self._macd_indicator.SignalMa.Length = self.MacdSignalLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(self._atr_indicator, self._process_atr_candle) \
            .BindEx(self._macd_indicator, self._process_main_candle) \
            .Start()

        self.SubscribeCandles(self._higher_time_frame) \
            .Bind(self._fast_std_dev, self._slow_std_dev, self._cci_indicator, self._process_higher_candle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._macd_indicator)
            self.DrawIndicator(area, self._cci_indicator)
            self.DrawOwnTrades(area)

    def _process_atr_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return
        self._last_atr = float(atr_value)
        self._atr_ready = self._atr_indicator.IsFormed

    def _process_higher_candle(self, candle, fast_std, slow_std, cci):
        if candle.State != CandleStates.Finished:
            return
        self._std_fast_value = float(fast_std)
        self._std_slow_value = float(slow_std)
        self._cci_value = float(cci)
        self._higher_ready = self._fast_std_dev.IsFormed and self._slow_std_dev.IsFormed and self._cci_indicator.IsFormed

    def _process_main_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        macd_v = macd_value.Macd
        signal_v = macd_value.Signal
        if macd_v is None or signal_v is None:
            return

        macd_current = float(macd_v)
        signal_current = float(signal_v)

        # Reset cached targets when strategy becomes flat
        if self.Position <= 0 and self._long_entries > 0:
            self._reset_long_targets()
        if self.Position >= 0 and self._short_entries > 0:
            self._reset_short_targets()

        macd1 = self._macd_main_prev1
        macd2 = self._macd_main_prev2
        signal1 = self._macd_signal_prev1
        signal2 = self._macd_signal_prev2
        has_macd_history = self._macd_samples >= 2

        price = float(candle.ClosePrice)

        if not self._macd_indicator.IsFormed or not self._higher_ready or not self._atr_ready or not has_macd_history or self._std_slow_value <= 0:
            self._update_state_after(macd_current, signal_current, candle)
            return

        cci = self._cci_value
        take_distance = self.TakeStdMultiplier * self._std_slow_value

        # Evaluate entry signals
        open_buy = cci < -self.CciThreshold and macd2 <= signal2 and macd1 > signal1 and macd1 < 0
        open_sell = cci > self.CciThreshold and macd2 >= signal2 and macd1 < signal1 and macd1 > 0

        if open_buy:
            if self.Position < 0:
                self.BuyMarket()
                self._reset_short_targets()

            allow_entry = self.Position >= 0 and self._long_entries < self.MaxLongPositions and take_distance > 0
            spaced_enough = self._last_long_price == 0 or abs(price - self._last_long_price) > self._spacing_distance
            new_bar = self._last_long_signal_time is None or self._last_long_signal_time != candle.OpenTime

            if allow_entry and spaced_enough and new_bar:
                self.BuyMarket()
                self._entry_price = price
                self._long_entries += 1
                self._last_long_price = price
                self._last_long_signal_time = candle.OpenTime
                self._long_stop_price = price - self._stop_loss_distance if self._stop_loss_distance > 0 else 0.0
                self._long_take_price = price + take_distance if take_distance > 0 else 0.0
                self._long_breakeven_activated = False

        elif open_sell:
            if self.Position > 0:
                self.SellMarket()
                self._reset_long_targets()

            allow_entry = self.Position <= 0 and self._short_entries < self.MaxShortPositions and take_distance > 0
            spaced_enough = self._last_short_price == 0 or abs(price - self._last_short_price) > self._spacing_distance
            new_bar = self._last_short_signal_time is None or self._last_short_signal_time != candle.OpenTime

            if allow_entry and spaced_enough and new_bar:
                self.SellMarket()
                self._entry_price = price
                self._short_entries += 1
                self._last_short_price = price
                self._last_short_signal_time = candle.OpenTime
                self._short_stop_price = price + self._stop_loss_distance if self._stop_loss_distance > 0 else 0.0
                self._short_take_price = price - take_distance if take_distance > 0 else 0.0
                self._short_breakeven_activated = False

        self._update_breakeven(price)

        if self.Position > 0:
            prev_range = self._prev_candle_close - self._prev_candle_open if self._has_prev_candle else 0.0
            exit_by_range = self._has_prev_candle and prev_range > self.CloseAtrMultiplier * self._last_atr
            exit_by_macd = macd1 < macd2 and price - self._entry_price > self._threshold_distance

            if exit_by_range or exit_by_macd:
                self.SellMarket()
                self._reset_long_targets()
            else:
                self._check_long_stops(price)

        elif self.Position < 0:
            prev_range = self._prev_candle_open - self._prev_candle_close if self._has_prev_candle else 0.0
            exit_by_range = self._has_prev_candle and prev_range > self.CloseAtrMultiplier * self._last_atr
            exit_by_macd = macd1 > macd2 and self._entry_price - price > self._threshold_distance

            if exit_by_range or exit_by_macd:
                self.BuyMarket()
                self._reset_short_targets()
            else:
                self._check_short_stops(price)
        else:
            self._reset_long_targets()
            self._reset_short_targets()

        self._update_state_after(macd_current, signal_current, candle)

    def _update_breakeven(self, price):
        if self.Position > 0 and not self._long_breakeven_activated and self._breakeven_distance > 0:
            if price - self._breakeven_distance > self._entry_price and self._long_stop_price > 0:
                self._long_breakeven_activated = True
                self._long_stop_price = self._entry_price
        elif self.Position <= 0:
            self._long_breakeven_activated = False

        if self.Position < 0 and not self._short_breakeven_activated and self._breakeven_distance > 0:
            if price + self._breakeven_distance < self._entry_price and self._short_stop_price > 0:
                self._short_breakeven_activated = True
                self._short_stop_price = self._entry_price
        elif self.Position >= 0:
            self._short_breakeven_activated = False

    def _check_long_stops(self, price):
        if self.Position <= 0:
            return
        if self._long_take_price > 0 and price >= self._long_take_price:
            self.SellMarket()
            self._reset_long_targets()
            return
        if self._long_stop_price > 0 and price <= self._long_stop_price:
            self.SellMarket()
            self._reset_long_targets()

    def _check_short_stops(self, price):
        if self.Position >= 0:
            return
        if self._short_take_price > 0 and price <= self._short_take_price:
            self.BuyMarket()
            self._reset_short_targets()
            return
        if self._short_stop_price > 0 and price >= self._short_stop_price:
            self.BuyMarket()
            self._reset_short_targets()

    def _reset_long_targets(self):
        if self.Position > 0:
            return
        self._long_stop_price = 0.0
        self._long_take_price = 0.0
        self._long_breakeven_activated = False
        self._long_entries = 0
        self._last_long_price = 0.0
        self._last_long_signal_time = None

    def _reset_short_targets(self):
        if self.Position < 0:
            return
        self._short_stop_price = 0.0
        self._short_take_price = 0.0
        self._short_breakeven_activated = False
        self._short_entries = 0
        self._last_short_price = 0.0
        self._last_short_signal_time = None

    def _update_state_after(self, macd_current, signal_current, candle):
        self._macd_main_prev2 = self._macd_main_prev1
        self._macd_main_prev1 = macd_current
        self._macd_signal_prev2 = self._macd_signal_prev1
        self._macd_signal_prev1 = signal_current
        if self._macd_samples < 2:
            self._macd_samples += 1
        self._prev_candle_open = float(candle.OpenPrice)
        self._prev_candle_close = float(candle.ClosePrice)
        self._has_prev_candle = True

    def OnReseted(self):
        super(anubis_strategy, self).OnReseted()
        self._last_atr = 0.0
        self._atr_ready = False
        self._cci_value = 0.0
        self._std_fast_value = 0.0
        self._std_slow_value = 0.0
        self._higher_ready = False
        self._macd_main_prev1 = 0.0
        self._macd_main_prev2 = 0.0
        self._macd_signal_prev1 = 0.0
        self._macd_signal_prev2 = 0.0
        self._macd_samples = 0
        self._prev_candle_open = 0.0
        self._prev_candle_close = 0.0
        self._has_prev_candle = False
        self._adjusted_point = 0.0
        self._stop_loss_distance = 0.0
        self._breakeven_distance = 0.0
        self._threshold_distance = 0.0
        self._spacing_distance = 0.0
        self._long_stop_price = 0.0
        self._long_take_price = 0.0
        self._long_breakeven_activated = False
        self._long_entries = 0
        self._last_long_signal_time = None
        self._last_long_price = 0.0
        self._short_stop_price = 0.0
        self._short_take_price = 0.0
        self._short_breakeven_activated = False
        self._short_entries = 0
        self._last_short_signal_time = None
        self._last_short_price = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return anubis_strategy()
