import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (ExponentialMovingAverage,
    MovingAverageConvergenceDivergenceSignal, CommodityChannelIndex,
    AverageDirectionalIndex)
from StockSharp.Algo.Strategies import Strategy

class cyberia_trader_adaptive_strategy(Strategy):
    """
    Adaptive port of CyberiaTrader EA. Combines probability based decision tree
    with optional indicator filters (EMA, MACD, CCI, ADX, fractals).
    """

    DECISION_SELL = 0
    DECISION_BUY = 1
    DECISION_UNKNOWN = 2

    FRACTAL_NONE = 0
    FRACTAL_UP = 1
    FRACTAL_DOWN = 2

    def __init__(self):
        super(cyberia_trader_adaptive_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle series used for calculations", "General")
        self._auto_select_period = self.Param("AutoSelectPeriod", True) \
            .SetDisplay("Auto Period", "Automatically scan for the best probability window", "General")
        self._initial_period = self.Param("InitialPeriod", 5) \
            .SetDisplay("Initial Period", "Fallback period for probability sampling", "General")
        self._max_period = self.Param("MaxPeriod", 23) \
            .SetDisplay("Max Period", "Upper bound for adaptive period search", "General")
        self._history_multiplier = self.Param("HistoryMultiplier", 5) \
            .SetDisplay("History Multiplier", "Number of samples per period used for statistics", "General")
        self._spread_filter = self.Param("SpreadFilter", 0.0) \
            .SetDisplay("Spread Filter", "Minimum move treated as actionable", "General")
        self._enable_cyberia_logic = self.Param("EnableCyberiaLogic", True) \
            .SetDisplay("Enable Cyberia Logic", "Use original probability based decision rules", "Logic")
        self._enable_ma = self.Param("EnableMa", False) \
            .SetDisplay("Enable EMA", "Use EMA slope filter", "Logic")
        self._enable_macd = self.Param("EnableMacd", False) \
            .SetDisplay("Enable MACD", "Use MACD trend filter", "Logic")
        self._enable_cci = self.Param("EnableCci", False) \
            .SetDisplay("Enable CCI", "Use CCI overbought/oversold filter", "Logic")
        self._enable_adx = self.Param("EnableAdx", False) \
            .SetDisplay("Enable ADX", "Use ADX directional filter", "Logic")
        self._enable_fractals = self.Param("EnableFractals", False) \
            .SetDisplay("Enable Fractals", "Block trades opposite to the latest fractal", "Logic")
        self._enable_reversal_detector = self.Param("EnableReversalDetector", False) \
            .SetDisplay("Enable Reversal Detector", "Toggle direction when probabilities spike", "Logic")
        self._ma_period = self.Param("MaPeriod", 23) \
            .SetDisplay("EMA Period", "Length of the EMA used by the filter", "Indicators")
        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators")
        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators")
        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicators")
        self._cci_period = self.Param("CciPeriod", 13) \
            .SetDisplay("CCI Period", "Commodity Channel Index length", "Indicators")
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Average Directional Index length", "Indicators")
        self._fractal_depth = self.Param("FractalDepth", 5) \
            .SetDisplay("Fractal Depth", "Number of candles used to detect fractals", "Indicators")
        self._reversal_index = self.Param("ReversalIndex", 3.0) \
            .SetDisplay("Reversal Index", "Multiplier for spike based reversal detection", "Logic")
        self._block_buy = self.Param("BlockBuy", False) \
            .SetDisplay("Block Buy", "Prevent buy orders regardless of signals", "Risk")
        self._block_sell = self.Param("BlockSell", False) \
            .SetDisplay("Block Sell", "Prevent sell orders regardless of signals", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 0.0) \
            .SetDisplay("Take Profit", "Absolute take profit distance", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 0.0) \
            .SetDisplay("Stop Loss", "Absolute stop loss distance", "Risk")

        self._history = []
        self._current_value_period = 5
        self._previous_value_period = 5
        self._current_values_period_count = 25

        self._previous_ema_value = None
        self._last_ema_value = None
        self._last_macd_value = None
        self._last_macd_signal_val = None
        self._last_cci_value = None
        self._last_plus_di = None
        self._last_minus_di = None
        self._fractal_direction = self.FRACTAL_NONE

        self._disable_buy = False
        self._disable_sell = False
        self._block_buy_flag = False
        self._block_sell_flag = False

        self._current_decision = self.DECISION_UNKNOWN
        self._buy_possibility = 0.0
        self._sell_possibility = 0.0
        self._undefined_possibility = 0.0

        self._buy_possibility_mid = 0.0
        self._sell_possibility_mid = 0.0
        self._undefined_possibility_mid = 0.0
        self._buy_suc_possibility_mid = 0.0
        self._sell_suc_possibility_mid = 0.0
        self._undefined_suc_possibility_mid = 0.0

        self._buy_possibility_quality = 0.0
        self._sell_possibility_quality = 0.0
        self._undefined_possibility_quality = 0.0
        self._buy_suc_possibility_quality = 0.0
        self._sell_suc_possibility_quality = 0.0
        self._undefined_suc_possibility_quality = 0.0
        self._possibility_quality = 0.0
        self._possibility_success_quality = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(cyberia_trader_adaptive_strategy, self).OnStarted(time)

        self._current_value_period = max(1, self._initial_period.Value)
        self._previous_value_period = self._current_value_period
        self._current_values_period_count = max(1, self._current_value_period * self._history_multiplier.Value)
        self._history = []

        ema = ExponentialMovingAverage()
        ema.Length = self._ma_period.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, macd, cci, adx, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

        tp = float(self._take_profit_points.Value)
        sl = float(self._stop_loss_points.Value)
        tp_unit = Unit(tp, UnitTypes.Absolute) if tp > 0 else None
        sl_unit = Unit(sl, UnitTypes.Absolute) if sl > 0 else None
        if tp_unit is not None or sl_unit is not None:
            self.StartProtection(tp_unit, sl_unit)

    def on_process(self, candle, ema_val, macd_val, cci_val, adx_val):
        if candle.State != CandleStates.Finished:
            return
        if not ema_val.IsFinal or not macd_val.IsFinal or not cci_val.IsFinal or not adx_val.IsFinal:
            return

        ema_dec = float(ema_val.ToDecimal())
        cci_dec = float(cci_val.ToDecimal())

        macd_typed = macd_val
        macd_v = float(macd_typed.Macd) if macd_typed.Macd is not None else None
        macd_s = float(macd_typed.Signal) if macd_typed.Signal is not None else None

        adx_typed = adx_val
        plus_di = float(adx_typed.Dx.Plus) if adx_typed.Dx.Plus is not None else None
        minus_di = float(adx_typed.Dx.Minus) if adx_typed.Dx.Minus is not None else None

        self._previous_ema_value = self._last_ema_value
        self._last_ema_value = ema_dec
        self._last_macd_value = macd_v
        self._last_macd_signal_val = macd_s
        self._last_cci_value = cci_dec
        self._last_plus_di = plus_di
        self._last_minus_di = minus_di

        self._add_candle(candle)
        self._update_fractal_state()

        if not self._update_adaptive_period():
            return

        self._calculate_direction()
        self._execute_trading_logic()

    def _calculate_direction(self):
        self._disable_buy = False
        self._disable_sell = False
        self._block_buy_flag = self._block_buy.Value
        self._block_sell_flag = self._block_sell.Value

        if self._enable_cyberia_logic.Value:
            self._apply_cyberia_logic()
        if self._enable_macd.Value:
            self._apply_macd_filter()
        if self._enable_ma.Value:
            self._apply_ma_filter()
        if self._enable_cci.Value:
            self._apply_cci_filter()
        if self._enable_adx.Value:
            self._apply_adx_filter()
        if self._enable_fractals.Value:
            self._apply_fractal_filter()
        if self._enable_reversal_detector.Value:
            self._apply_reversal_detector()

    def _execute_trading_logic(self):
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        allow_buy = not self._disable_buy and not self._block_buy_flag
        allow_sell = not self._disable_sell and not self._block_sell_flag

        if self._current_decision == self.DECISION_BUY and allow_buy:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif self._current_decision == self.DECISION_SELL and allow_sell:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        elif self._current_decision == self.DECISION_UNKNOWN:
            if self._possibility_quality < 0.5:
                if self.Position > 0:
                    self.SellMarket()
                elif self.Position < 0:
                    self.BuyMarket()

    def _apply_cyberia_logic(self):
        left_score = self._sell_possibility_mid * self._sell_possibility_quality
        right_score = self._buy_possibility_mid * self._buy_possibility_quality
        left_success = self._sell_suc_possibility_mid * self._sell_suc_possibility_quality
        right_success = self._buy_suc_possibility_mid * self._buy_suc_possibility_quality

        if self._current_value_period > self._previous_value_period:
            if left_score > right_score:
                self._disable_sell = False
                self._disable_buy = True
                if left_success > right_success:
                    self._disable_sell = True
            elif left_score < right_score:
                self._disable_sell = True
                self._disable_buy = False
                if left_success < right_success:
                    self._disable_buy = True
        elif self._current_value_period < self._previous_value_period:
            self._disable_sell = True
            self._disable_buy = True

        if left_score == right_score:
            self._disable_sell = True
            self._disable_buy = True

        if (self._sell_possibility_mid > 0 and self._sell_suc_possibility_mid > 0 and
                self._sell_possibility > self._sell_suc_possibility_mid * 2):
            self._disable_sell = True
        if (self._buy_possibility_mid > 0 and self._buy_suc_possibility_mid > 0 and
                self._buy_possibility > self._buy_suc_possibility_mid * 2):
            self._disable_buy = True

    def _apply_macd_filter(self):
        if self._last_macd_value is None or self._last_macd_signal_val is None:
            return
        if self._last_macd_value > self._last_macd_signal_val:
            self._disable_sell = True
        elif self._last_macd_value < self._last_macd_signal_val:
            self._disable_buy = True

    def _apply_ma_filter(self):
        if self._previous_ema_value is None or self._last_ema_value is None:
            return
        if self._last_ema_value > self._previous_ema_value:
            self._disable_sell = True
        elif self._last_ema_value < self._previous_ema_value:
            self._disable_buy = True

    def _apply_cci_filter(self):
        if self._last_cci_value is None:
            return
        if self._last_cci_value < -100:
            self._disable_sell = True
        elif self._last_cci_value > 100:
            self._disable_buy = True

    def _apply_adx_filter(self):
        if self._last_plus_di is None or self._last_minus_di is None:
            return
        if self._last_plus_di > self._last_minus_di:
            self._disable_sell = True
        elif self._last_minus_di > self._last_plus_di:
            self._disable_buy = True

    def _apply_fractal_filter(self):
        if self._fractal_direction == self.FRACTAL_UP:
            self._block_buy_flag = True
            self._block_sell_flag = False
        elif self._fractal_direction == self.FRACTAL_DOWN:
            self._block_sell_flag = True
            self._block_buy_flag = False

    def _apply_reversal_detector(self):
        trigger = False
        ri = float(self._reversal_index.Value)
        if (self._buy_possibility != 0 and self._buy_possibility_mid != 0 and
                self._buy_possibility > self._buy_possibility_mid * ri):
            trigger = True
        if (self._sell_possibility != 0 and self._sell_possibility_mid != 0 and
                self._sell_possibility > self._sell_possibility_mid * ri):
            trigger = True
        if not trigger:
            return
        self._disable_sell = not self._disable_sell
        self._disable_buy = not self._disable_buy

    def _add_candle(self, candle):
        snapshot = (float(candle.OpenPrice), float(candle.HighPrice),
                    float(candle.LowPrice), float(candle.ClosePrice))
        self._history.append(snapshot)
        max_hist = max(self._max_period.Value, self._current_value_period) * (self._history_multiplier.Value + 2) + 2
        while len(self._history) > max_hist:
            self._history.pop(0)

    def _update_fractal_state(self):
        depth = max(5, self._fractal_depth.Value)
        if depth % 2 == 0:
            depth += 1
        if len(self._history) < depth:
            return
        start = len(self._history) - depth
        middle = start + depth // 2
        center = self._history[middle]
        is_upper = True
        is_lower = True
        for i in range(start, start + depth):
            if i == middle:
                continue
            sample = self._history[i]
            if sample[1] >= center[1]:
                is_upper = False
            if sample[2] <= center[2]:
                is_lower = False
        if is_upper:
            self._fractal_direction = self.FRACTAL_UP
        elif is_lower:
            self._fractal_direction = self.FRACTAL_DOWN

    def _update_adaptive_period(self):
        base_period = max(1, self._initial_period.Value)
        max_period_val = max(1, self._max_period.Value) if self._auto_select_period.Value else base_period

        best_stats = None
        best_quality = -1e18
        selected_period = base_period

        for period in range(1, max_period_val + 1):
            if not self._auto_select_period.Value and period != base_period:
                continue
            stats = self._calculate_statistics(period)
            if stats is None:
                continue
            if not self._auto_select_period.Value:
                best_stats = stats
                best_quality = stats['poss_success_quality']
                selected_period = period
                break
            if stats['poss_success_quality'] > best_quality:
                best_quality = stats['poss_success_quality']
                selected_period = period
                best_stats = stats

        if best_stats is None:
            return False

        self._previous_value_period = self._current_value_period
        self._current_value_period = selected_period
        self._current_values_period_count = max(1, self._current_value_period * self._history_multiplier.Value)
        self._apply_statistics(best_stats)
        return True

    def _calculate_statistics(self, period):
        modeling_bars = max(1, period * self._history_multiplier.Value)
        required = period * (modeling_bars + 1)
        if len(self._history) < required:
            return None

        spread = float(self._spread_filter.Value)
        buy_sum = sell_sum = undef_sum = 0.0
        buy_suc_sum = sell_suc_sum = undef_suc_sum = 0.0
        buy_count = sell_count = undef_count = 0
        buy_suc_count = sell_suc_count = undef_suc_count = 0
        buy_q = sell_q = undef_q = 0.0
        buy_suc_q = sell_suc_q = undef_suc_q = 0.0

        current_decision = self.DECISION_UNKNOWN
        current_buy = current_sell = current_undef = 0.0
        current_dv = prev_dv = 0.0

        shifts = min(modeling_bars, (len(self._history) // period) - 1)

        for i in range(shifts + 1):
            result = self._calculate_possibility(period, i)
            if i == 0:
                current_decision = result[0]
                current_buy = result[1]
                current_sell = result[2]
                current_undef = result[3]
                current_dv = result[4]
                prev_dv = result[5]

            if result[0] == self.DECISION_BUY:
                buy_q += 1.0
            elif result[0] == self.DECISION_SELL:
                sell_q += 1.0
            else:
                undef_q += 1.0

            if result[1] > spread:
                buy_suc_q += 1.0
                buy_suc_sum += result[1]
                buy_suc_count += 1
            if result[2] > spread:
                sell_suc_q += 1.0
                sell_suc_sum += result[2]
                sell_suc_count += 1
            if result[3] > spread:
                undef_suc_q += 1.0
                undef_suc_sum += result[3]
                undef_suc_count += 1

            buy_sum += result[1]
            sell_sum += result[2]
            undef_sum += result[3]
            buy_count += 1
            sell_count += 1
            undef_count += 1

        total_q = buy_q + sell_q + undef_q
        total_suc_q = buy_suc_q + sell_suc_q + undef_suc_q

        return {
            'decision': current_decision,
            'buy_poss': current_buy,
            'sell_poss': current_sell,
            'undef_poss': current_undef,
            'dv': current_dv,
            'prev_dv': prev_dv,
            'buy_mid': buy_sum / buy_count if buy_count > 0 else 0.0,
            'sell_mid': sell_sum / sell_count if sell_count > 0 else 0.0,
            'undef_mid': undef_sum / undef_count if undef_count > 0 else 0.0,
            'buy_suc_mid': buy_suc_sum / buy_suc_count if buy_suc_count > 0 else 0.0,
            'sell_suc_mid': sell_suc_sum / sell_suc_count if sell_suc_count > 0 else 0.0,
            'undef_suc_mid': undef_suc_sum / undef_suc_count if undef_suc_count > 0 else 0.0,
            'buy_q': buy_q,
            'sell_q': sell_q,
            'undef_q': undef_q,
            'buy_suc_q': buy_suc_q,
            'sell_suc_q': sell_suc_q,
            'undef_suc_q': undef_suc_q,
            'poss_quality': (sell_q + buy_q) / total_q if total_q > 0 else 0.0,
            'poss_success_quality': (sell_suc_q + buy_suc_q) / total_suc_q if total_suc_q > 0 else 0.0,
        }

    def _apply_statistics(self, stats):
        self._current_decision = stats['decision']
        self._buy_possibility = stats['buy_poss']
        self._sell_possibility = stats['sell_poss']
        self._undefined_possibility = stats['undef_poss']
        self._buy_possibility_mid = stats['buy_mid']
        self._sell_possibility_mid = stats['sell_mid']
        self._undefined_possibility_mid = stats['undef_mid']
        self._buy_suc_possibility_mid = stats['buy_suc_mid']
        self._sell_suc_possibility_mid = stats['sell_suc_mid']
        self._undefined_suc_possibility_mid = stats['undef_suc_mid']
        self._buy_possibility_quality = stats['buy_q']
        self._sell_possibility_quality = stats['sell_q']
        self._undefined_possibility_quality = stats['undef_q']
        self._buy_suc_possibility_quality = stats['buy_suc_q']
        self._sell_suc_possibility_quality = stats['sell_suc_q']
        self._undefined_suc_possibility_quality = stats['undef_suc_q']
        self._possibility_quality = stats['poss_quality']
        self._possibility_success_quality = stats['poss_success_quality']

    def _calculate_possibility(self, period, shift):
        current_index = period * shift
        previous_index = period * (shift + 1)
        current = self._get_candle(current_index)
        previous = self._get_candle(previous_index)

        dv = current[3] - current[0]
        prev_dv = previous[3] - previous[0]

        buy_p = sell_p = undef_p = 0.0
        decision = self.DECISION_UNKNOWN

        if dv > 0:
            if prev_dv < 0:
                decision = self.DECISION_SELL
                sell_p = dv
            else:
                decision = self.DECISION_UNKNOWN
                undef_p = dv
        elif dv < 0:
            if prev_dv > 0:
                decision = self.DECISION_BUY
                buy_p = -dv
            else:
                decision = self.DECISION_UNKNOWN
                undef_p = -dv

        return (decision, buy_p, sell_p, undef_p, dv, prev_dv)

    def _get_candle(self, shift):
        index = len(self._history) - 1 - shift
        if index < 0:
            return (0.0, 0.0, 0.0, 0.0)
        return self._history[index]

    def CreateClone(self):
        return cyberia_trader_adaptive_strategy()
