import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    MovingAverageConvergenceDivergence, ExponentialMovingAverage,
    SimpleMovingAverage
)

class macd_pattern_trader_v02_strategy(Strategy):
    def __init__(self):
        super(macd_pattern_trader_v02_strategy, self).__init__()
        self._stop_loss_bars = self.Param("StopLossBars", 6).SetDisplay("Stop-Loss Bars", "Number of candles for stop-loss calculation", "Risk")
        self._take_profit_bars = self.Param("TakeProfitBars", 20).SetDisplay("Take-Profit Bars", "Window used when scanning for take-profit", "Risk")
        self._offset_points = self.Param("OffsetPoints", 10).SetDisplay("Offset Points", "Additional protective offset in points", "Risk")
        self._profit_threshold_points = self.Param("ProfitThresholdPoints", 500.0).SetDisplay("Profit Threshold Points", "Minimal profit in points before partial exits", "Risk")
        self._fast_ema_period = self.Param("FastEmaPeriod", 12).SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 26).SetDisplay("Slow EMA", "Slow EMA period for MACD", "Indicators")
        self._max_threshold = self.Param("MaxThreshold", 50.0).SetDisplay("Upper Threshold", "Maximum MACD threshold for longs", "Signals")
        self._min_threshold = self.Param("MinThreshold", -50.0).SetDisplay("Lower Threshold", "Minimum MACD threshold for shorts", "Signals")
        self._ema1_period = self.Param("Ema1Period", 7).SetDisplay("EMA 1", "First EMA period for management", "Management")
        self._ema2_period = self.Param("Ema2Period", 21).SetDisplay("EMA 2", "Second EMA period for management", "Management")
        self._sma_period = self.Param("SmaPeriod", 98).SetDisplay("SMA", "SMA period for management", "Management")
        self._ema3_period = self.Param("Ema3Period", 365).SetDisplay("EMA 3", "Slow EMA period for management", "Management")
        self._trade_volume = self.Param("TradeVolume", 0.1).SetDisplay("Trade Volume", "Market order volume", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Candle type used for indicators", "General")
        self._max_history_param = self.Param("MaxHistory", 1024).SetDisplay("History Limit", "Maximum candles stored", "General")
        self._history = []
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._ema1_prev = None
        self._ema2_prev = None
        self._sma_prev = None
        self._ema3_prev = None
        self._ema1_last = None
        self._ema2_last = None
        self._sma_last = None
        self._ema3_last = None
        self._max_threshold_reached = False
        self._min_threshold_reached = False
        self._sell_pattern_ready = False
        self._buy_pattern_ready = False
        self._pattern_min_value = 0.0
        self._pattern_max_value = 0.0
        self._point_size = 0.0
        self._entry_direction = 0
        self._entry_price = 0.0
        self._open_volume = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._long_partial_stage = 0
        self._short_partial_stage = 0

    @property
    def StopLossBars(self): return self._stop_loss_bars.Value
    @property
    def TakeProfitBars(self): return self._take_profit_bars.Value
    @property
    def OffsetPoints(self): return self._offset_points.Value
    @property
    def ProfitThresholdPoints(self): return self._profit_threshold_points.Value
    @property
    def FastEmaPeriod(self): return self._fast_ema_period.Value
    @property
    def SlowEmaPeriod(self): return self._slow_ema_period.Value
    @property
    def MaxThreshold(self): return self._max_threshold.Value
    @property
    def MinThreshold(self): return self._min_threshold.Value
    @property
    def Ema1Period(self): return self._ema1_period.Value
    @property
    def Ema2Period(self): return self._ema2_period.Value
    @property
    def SmaPeriod(self): return self._sma_period.Value
    @property
    def Ema3Period(self): return self._ema3_period.Value
    @property
    def TradeVolume(self): return self._trade_volume.Value
    @property
    def CandleType(self): return self._candle_type.Value
    @property
    def MaxHistoryParam(self): return self._max_history_param.Value

    def OnStarted(self, time):
        super(macd_pattern_trader_v02_strategy, self).OnStarted(time)
        ps = self.Security.PriceStep if self.Security is not None else None
        self._point_size = float(ps) if ps is not None and float(ps) > 0 else 0.0001
        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.FastEmaPeriod
        macd.LongMa.Length = self.SlowEmaPeriod
        ema1 = ExponentialMovingAverage()
        ema1.Length = self.Ema1Period
        ema2 = ExponentialMovingAverage()
        ema2.Length = self.Ema2Period
        sma = SimpleMovingAverage()
        sma.Length = self.SmaPeriod
        ema3 = ExponentialMovingAverage()
        ema3.Length = self.Ema3Period
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(macd, ema1, ema2, sma, ema3, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, macd_line, ema1_value, ema2_value, sma_value, ema3_value):
        if candle.State != CandleStates.Finished:
            return
        mv = float(macd_line)
        self._ema1_prev = self._ema1_last
        self._ema2_prev = self._ema2_last
        self._sma_prev = self._sma_last
        self._ema3_prev = self._ema3_last
        self._ema1_last = float(ema1_value)
        self._ema2_last = float(ema2_value)
        self._sma_last = float(sma_value)
        self._ema3_last = float(ema3_value)
        if self._macd_prev1 is None or self._macd_prev2 is None or self._macd_prev3 is None:
            self._macd_prev3 = self._macd_prev2
            self._macd_prev2 = self._macd_prev1
            self._macd_prev1 = mv
            self._add_candle(candle)
            return
        ml, ml2, ml3 = self._macd_prev1, self._macd_prev2, self._macd_prev3
        self._add_candle(candle)
        self._execute_pattern_logic(candle, mv, ml, ml2, ml3)
        self._macd_prev3 = self._macd_prev2
        self._macd_prev2 = self._macd_prev1
        self._macd_prev1 = mv

    def _execute_pattern_logic(self, candle, mc, mp1, mp2, mp3):
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._point_size <= 0:
            return
        min_thr = float(self.MinThreshold)
        max_thr = float(self.MaxThreshold)
        if mc > 0:
            self._max_threshold_reached = True
            self._sell_pattern_ready = False
        if mc > mp1 and mp1 < mp3 and self._max_threshold_reached and mc > min_thr and mc < 0 and not self._sell_pattern_ready:
            self._sell_pattern_ready = True
            self._pattern_min_value = abs(mp1 * 10000.0)
        cm = abs(mc * 10000.0)
        if self._sell_pattern_ready and mc < mp1 and mp1 > mp3 and mc < 0 and self._pattern_min_value <= cm:
            self._max_threshold_reached = False
        if self._sell_pattern_ready and mc < mp1 and mp1 > mp3 and mc < 0:
            self._try_open_short(candle)
            self._sell_pattern_ready = False
            self._max_threshold_reached = False
        if mc < 0:
            self._min_threshold_reached = True
            self._buy_pattern_ready = False
        if mc < max_thr and mc < mp1 and mp1 > mp3 and self._min_threshold_reached and mc > 0 and not self._buy_pattern_ready:
            self._buy_pattern_ready = True
            self._pattern_max_value = abs(mp1 * 10000.0)
        if self._buy_pattern_ready and mc > mp1 and mp1 < mp3 and mc > 0 and self._pattern_max_value <= cm:
            self._min_threshold_reached = False
        if self._buy_pattern_ready and mc > mp1 and mp1 < mp3 and mc > 0:
            self._try_open_long(candle)
            self._buy_pattern_ready = False
            self._min_threshold_reached = False
        self._manage_position(candle)

    def _try_open_short(self, candle):
        if self.Position > 0:
            self.SellMarket(abs(self.Position))
            self._reset_position_state()
        if self.Position < 0:
            return
        volume = float(self.TradeVolume)
        if volume <= 0:
            return
        self.SellMarket(volume)
        self._register_entry(-1, float(candle.ClosePrice), volume)

    def _try_open_long(self, candle):
        if self.Position < 0:
            self.BuyMarket(abs(self.Position))
            self._reset_position_state()
        if self.Position > 0:
            return
        volume = float(self.TradeVolume)
        if volume <= 0:
            return
        self.BuyMarket(volume)
        self._register_entry(1, float(candle.ClosePrice), volume)

    def _register_entry(self, direction, entry_price, volume):
        self._entry_direction = direction
        self._entry_price = entry_price
        self._open_volume = volume
        self._stop_loss_price = self._calculate_long_stop() if direction > 0 else self._calculate_short_stop()
        self._take_profit_price = self._calculate_long_target() if direction > 0 else self._calculate_short_target()
        self._long_partial_stage = 0
        self._short_partial_stage = 0

    def _manage_position(self, candle):
        if self._entry_direction == 0 or self._open_volume <= 0:
            return
        if self._check_risk_management(candle):
            return
        prev_candle = self._get_candle(1)
        if prev_candle is None or self._ema2_prev is None or self._ema3_prev is None or self._sma_prev is None:
            return
        pp = self._calculate_open_profit_points(float(candle.ClosePrice))
        pt = float(self.ProfitThresholdPoints)
        if self._entry_direction > 0:
            if pp > pt and float(prev_candle.ClosePrice) > self._ema2_prev and self._long_partial_stage == 0:
                v = self._open_volume / 3.0
                if v > 0:
                    self.SellMarket(v)
                    self._register_close(v)
                    self._long_partial_stage = 1
            elif pp > pt and float(prev_candle.HighPrice) > (self._sma_prev + self._ema3_prev) / 2.0 and self._long_partial_stage == 1:
                v = self._open_volume / 2.0
                if v > 0:
                    self.SellMarket(v)
                    self._register_close(v)
                    self._long_partial_stage = 2
        elif self._entry_direction < 0:
            if pp > pt and float(prev_candle.ClosePrice) < self._ema2_prev and self._short_partial_stage == 0:
                v = self._open_volume / 3.0
                if v > 0:
                    self.BuyMarket(v)
                    self._register_close(v)
                    self._short_partial_stage = 1
            elif pp > pt and float(prev_candle.LowPrice) < (self._sma_prev + self._ema3_prev) / 2.0 and self._short_partial_stage == 1:
                v = self._open_volume / 2.0
                if v > 0:
                    self.BuyMarket(v)
                    self._register_close(v)
                    self._short_partial_stage = 2

    def _check_risk_management(self, candle):
        if self._entry_direction == 0 or self._open_volume <= 0:
            return False
        if self._entry_direction > 0:
            if self._stop_loss_price is not None and float(candle.LowPrice) <= self._stop_loss_price:
                self.SellMarket(self._open_volume)
                self._reset_position_state()
                return True
            if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(self._open_volume)
                self._reset_position_state()
                return True
        else:
            if self._stop_loss_price is not None and float(candle.HighPrice) >= self._stop_loss_price:
                self.BuyMarket(self._open_volume)
                self._reset_position_state()
                return True
            if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(self._open_volume)
                self._reset_position_state()
                return True
        return False

    def _calculate_open_profit_points(self, current_price):
        if self._point_size <= 0:
            return 0.0
        diff = current_price - self._entry_price if self._entry_direction > 0 else self._entry_price - current_price
        return abs(diff / self._point_size)

    def _register_close(self, volume):
        self._open_volume -= volume
        if self._open_volume <= 0 or abs(self.Position) < 1e-6:
            self._reset_position_state()

    def _reset_position_state(self):
        self._entry_direction = 0
        self._entry_price = 0.0
        self._open_volume = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._long_partial_stage = 0
        self._short_partial_stage = 0

    def _calculate_short_stop(self):
        candles = self._get_candles_range(int(self.StopLossBars), 1)
        if len(candles) == 0:
            return None
        return max(float(c.HighPrice) for c in candles) + int(self.OffsetPoints) * self._point_size

    def _calculate_long_stop(self):
        candles = self._get_candles_range(int(self.StopLossBars), 1)
        if len(candles) == 0:
            return None
        return min(float(c.LowPrice) for c in candles) - int(self.OffsetPoints) * self._point_size

    def _calculate_short_target(self):
        return self._scan_sequential_extremum(int(self.TakeProfitBars), True)

    def _calculate_long_target(self):
        return self._scan_sequential_extremum(int(self.TakeProfitBars), False)

    def _scan_sequential_extremum(self, window, is_short):
        if window <= 0:
            return None
        best = None
        shift = 0
        while True:
            candles = self._get_candles_range(window, shift)
            if len(candles) == 0:
                break
            if is_short:
                candidate = min(float(c.LowPrice) for c in candles)
                if best is None or candidate < best:
                    best = candidate
                    shift += window
                    continue
            else:
                candidate = max(float(c.HighPrice) for c in candles)
                if best is None or candidate > best:
                    best = candidate
                    shift += window
                    continue
            break
        return best

    def _get_candles_range(self, length, shift):
        result = []
        if length <= 0:
            return result
        i = len(self._history) - 1 - shift
        while i >= 0 and len(result) < length:
            result.append(self._history[i])
            i -= 1
        return result

    def _get_candle(self, shift):
        idx = len(self._history) - 1 - shift
        if idx < 0 or idx >= len(self._history):
            return None
        return self._history[idx]

    def _add_candle(self, candle):
        self._history.append(candle)
        mh = int(self.MaxHistoryParam)
        if len(self._history) > mh:
            self._history.pop(0)

    def OnReseted(self):
        super(macd_pattern_trader_v02_strategy, self).OnReseted()
        self._history = []
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._ema1_prev = None
        self._ema2_prev = None
        self._sma_prev = None
        self._ema3_prev = None
        self._ema1_last = None
        self._ema2_last = None
        self._sma_last = None
        self._ema3_last = None
        self._max_threshold_reached = False
        self._min_threshold_reached = False
        self._sell_pattern_ready = False
        self._buy_pattern_ready = False
        self._pattern_min_value = 0.0
        self._pattern_max_value = 0.0
        self._point_size = 0.0
        self._reset_position_state()

    def CreateClone(self):
        return macd_pattern_trader_v02_strategy()
