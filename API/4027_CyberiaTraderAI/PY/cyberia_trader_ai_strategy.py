import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from collections import deque
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    MovingAverageConvergenceDivergenceSignal,
    ExponentialMovingAverage,
)

# Trade decision constants
_DECISION_UNKNOWN = 0
_DECISION_BUY = 1
_DECISION_SELL = 2

class cyberia_trader_ai_strategy(Strategy):
    def __init__(self):
        super(cyberia_trader_ai_strategy, self).__init__()

        self._max_period = self.Param("MaxPeriod", 23) \
            .SetDisplay("Max Period", "Largest sampling stride tested by the probability engine", "Model")
        self._samples_per_period = self.Param("SamplesPerPeriod", 5) \
            .SetDisplay("Segments Per Period", "Number of historical segments processed for every period candidate", "Model")
        self._spread_threshold = self.Param("SpreadThreshold", 0.0) \
            .SetDisplay("Spread Threshold", "Minimal absolute move to count a probability as successful", "Model")
        self._enable_cyberia_logic = self.Param("EnableCyberiaLogic", True) \
            .SetDisplay("Enable Cyberia Logic", "Use the probability based disable/allow switches", "Filters")
        self._enable_macd = self.Param("EnableMacd", False) \
            .SetDisplay("Enable MACD", "Use MACD to block trading against momentum", "Filters")
        self._enable_ma = self.Param("EnableMa", False) \
            .SetDisplay("Enable EMA", "Use EMA slope to forbid trades against the trend", "Filters")
        self._enable_reversal_detector = self.Param("EnableReversalDetector", False) \
            .SetDisplay("Enable Reversal Detector", "Flip permissions on extreme probability spikes", "Filters")
        self._ma_period = self.Param("MaPeriod", 23) \
            .SetDisplay("EMA Period", "Length of the EMA used in the trend filter", "Indicators")
        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators")
        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators")
        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicators")
        self._reversal_factor = self.Param("ReversalFactor", 3.0) \
            .SetDisplay("Reversal Factor", "Threshold multiplier that triggers the reversal detector", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Primary timeframe processed by the model", "General")
        self._take_profit_percent = self.Param("TakeProfitPercent", 0.0) \
            .SetDisplay("Take Profit %", "Optional take profit distance expressed in percent", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 0.0) \
            .SetDisplay("Stop Loss %", "Optional stop loss distance expressed in percent", "Risk")

        self._history = deque()
        self._previous_ema = None
        self._previous_period = None
        self._current_stats = None

    @property
    def MaxPeriod(self):
        return self._max_period.Value

    @property
    def SamplesPerPeriod(self):
        return self._samples_per_period.Value

    @property
    def SpreadThreshold(self):
        return self._spread_threshold.Value

    @property
    def EnableCyberiaLogic(self):
        return self._enable_cyberia_logic.Value

    @property
    def EnableMacd(self):
        return self._enable_macd.Value

    @property
    def EnableMa(self):
        return self._enable_ma.Value

    @property
    def EnableReversalDetector(self):
        return self._enable_reversal_detector.Value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @property
    def MacdFast(self):
        return self._macd_fast.Value

    @property
    def MacdSlow(self):
        return self._macd_slow.Value

    @property
    def MacdSignal(self):
        return self._macd_signal.Value

    @property
    def ReversalFactor(self):
        return self._reversal_factor.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def TakeProfitPercent(self):
        return self._take_profit_percent.Value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    def OnStarted(self, time):
        super(cyberia_trader_ai_strategy, self).OnStarted(time)

        self._macd_indicator = MovingAverageConvergenceDivergenceSignal()
        self._macd_indicator.Macd.ShortMa.Length = self.MacdFast
        self._macd_indicator.Macd.LongMa.Length = self.MacdSlow
        self._macd_indicator.SignalMa.Length = self.MacdSignal

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._macd_indicator, self._ema, self.ProcessCandle).Start()

        tp = float(self.TakeProfitPercent)
        sl = float(self.StopLossPercent)
        take_profit = Unit(tp / 100.0, UnitTypes.Percent) if tp > 0 else Unit()
        stop_loss = Unit(sl / 100.0, UnitTypes.Percent) if sl > 0 else Unit()
        self.StartProtection(take_profit, stop_loss)

    def ProcessCandle(self, candle, macd_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        macd_main_val = None
        macd_signal_val = None
        if macd_value.IsFinal:
            if hasattr(macd_value, 'Macd') and hasattr(macd_value, 'Signal'):
                macd_main_val = float(macd_value.Macd)
                macd_signal_val = float(macd_value.Signal)
        elif self.EnableMacd:
            return

        ema_snapshot = None
        if ema_value.IsFinal:
            ema_snapshot = float(ema_value.ToDecimal())
        elif self.EnableMa:
            return

        self._update_history(candle)

        candles = list(self._history)
        self._current_stats = self._find_best_stats(candles)

        if ema_snapshot is not None:
            if self._previous_ema is None:
                self._previous_ema = ema_snapshot

        if not self.IsFormedAndOnlineAndAllowTrading():
            if ema_snapshot is not None:
                self._previous_ema = ema_snapshot
            return

        if self._current_stats is None or not self._current_stats['is_valid']:
            if ema_snapshot is not None:
                self._previous_ema = ema_snapshot
            return

        flags = self._calculate_direction(ema_snapshot, macd_main_val, macd_signal_val)
        self._handle_positions(flags)
        self._previous_period = self._current_stats['period']

    def _handle_positions(self, flags):
        stats = self._current_stats
        if stats is None or not stats['is_valid']:
            return

        if self.Position > 0:
            should_exit = (stats['current_decision'] == _DECISION_SELL and
                           stats['sell_possibility'] >= stats['sell_suc_possibility_mid'] and
                           stats['sell_suc_possibility_mid'] > 0) or \
                          (flags['disable_buy'] and stats['current_decision'] != _DECISION_BUY)
            if should_exit:
                self.SellMarket(abs(self.Position))
                return

        elif self.Position < 0:
            should_exit = (stats['current_decision'] == _DECISION_BUY and
                           stats['buy_possibility'] >= stats['buy_suc_possibility_mid'] and
                           stats['buy_suc_possibility_mid'] > 0) or \
                          (flags['disable_sell'] and stats['current_decision'] != _DECISION_SELL)
            if should_exit:
                self.BuyMarket(abs(self.Position))
                return

        if (stats['current_decision'] == _DECISION_BUY and
                not flags['disable_buy'] and
                stats['buy_possibility'] >= stats['buy_suc_possibility_mid'] and
                stats['buy_suc_possibility_mid'] > 0 and
                self.Position <= 0):
            volume = self.Volume + (abs(self.Position) if self.Position < 0 else 0)
            self.BuyMarket(volume)
            return

        if (stats['current_decision'] == _DECISION_SELL and
                not flags['disable_sell'] and
                stats['sell_possibility'] >= stats['sell_suc_possibility_mid'] and
                stats['sell_suc_possibility_mid'] > 0 and
                self.Position >= 0):
            volume = self.Volume + (abs(self.Position) if self.Position > 0 else 0)
            self.SellMarket(volume)

    def _calculate_direction(self, ema_val, macd_main, macd_signal):
        stats = self._current_stats
        disable_buy = False
        disable_sell = False
        disable_pipsator = False
        disable_buy_pips = False
        disable_sell_pips = False

        if self.EnableCyberiaLogic:
            buy_score = stats['buy_possibility_mid'] * stats['buy_possibility_quality']
            sell_score = stats['sell_possibility_mid'] * stats['sell_possibility_quality']

            if self._previous_period is not None:
                if stats['period'] > self._previous_period:
                    if sell_score > buy_score:
                        disable_sell = False
                        disable_buy = True
                        disable_buy_pips = True
                        if (stats['sell_suc_possibility_mid'] * stats['sell_suc_possibility_quality'] >
                                stats['buy_suc_possibility_mid'] * stats['buy_suc_possibility_quality']):
                            disable_sell = True
                    elif sell_score < buy_score:
                        disable_sell = True
                        disable_buy = False
                        disable_sell_pips = True
                        if (stats['sell_suc_possibility_mid'] * stats['sell_suc_possibility_quality'] <
                                stats['buy_suc_possibility_mid'] * stats['buy_suc_possibility_quality']):
                            disable_buy = True
                elif stats['period'] < self._previous_period:
                    disable_sell = True
                    disable_buy = True

            if sell_score == buy_score:
                disable_sell = True
                disable_buy = True
                disable_pipsator = False

            if stats['sell_suc_possibility_mid'] > 0 and stats['sell_possibility'] > stats['sell_suc_possibility_mid'] * 2:
                disable_sell = True
                disable_sell_pips = True

            if stats['buy_suc_possibility_mid'] > 0 and stats['buy_possibility'] > stats['buy_suc_possibility_mid'] * 2:
                disable_buy = True
                disable_buy_pips = True

        if self.EnableMa and ema_val is not None:
            if self._previous_ema is not None:
                if ema_val > self._previous_ema:
                    disable_sell = True
                    disable_sell_pips = True
                elif ema_val < self._previous_ema:
                    disable_buy = True
                    disable_buy_pips = True
            self._previous_ema = ema_val
        elif ema_val is not None:
            self._previous_ema = ema_val

        if self.EnableMacd and macd_main is not None and macd_signal is not None:
            if macd_main > macd_signal:
                disable_sell = True
            elif macd_main < macd_signal:
                disable_buy = True

        if self.EnableReversalDetector:
            trigger = False
            rev_factor = float(self.ReversalFactor)
            if stats['buy_possibility_mid'] > 0 and stats['buy_possibility'] > stats['buy_possibility_mid'] * rev_factor:
                trigger = True
            if stats['sell_possibility_mid'] > 0 and stats['sell_possibility'] > stats['sell_possibility_mid'] * rev_factor:
                trigger = True
            if trigger:
                disable_sell = not disable_sell
                disable_buy = not disable_buy
                disable_sell_pips = not disable_sell_pips
                disable_buy_pips = not disable_buy_pips
                disable_pipsator = not disable_pipsator

        return {
            'disable_buy': disable_buy,
            'disable_sell': disable_sell,
            'disable_pipsator': disable_pipsator,
            'disable_buy_pips': disable_buy_pips,
            'disable_sell_pips': disable_sell_pips,
        }

    def _update_history(self, candle):
        snapshot = (float(candle.OpenPrice), float(candle.HighPrice), float(candle.LowPrice), float(candle.ClosePrice))
        self._history.append(snapshot)
        max_period = self.MaxPeriod
        samples = self.SamplesPerPeriod
        max_history = max_period * (max_period * samples + 2)
        while len(self._history) > max_history:
            self._history.popleft()

    def _find_best_stats(self, candles):
        best_stats = None
        best_quality = -1e18
        max_period = self.MaxPeriod
        segments = self.SamplesPerPeriod
        spread = float(self.SpreadThreshold)

        for period in range(1, max_period + 1):
            modeling_bars = period * segments
            required = period * modeling_bars + 1
            if len(candles) < required:
                continue
            stats = self._calculate_stats(candles, period, modeling_bars, spread)
            if stats is None or not stats['is_valid']:
                continue
            if stats['possibility_success_ratio'] > best_quality:
                best_quality = stats['possibility_success_ratio']
                best_stats = stats

        return best_stats

    def _calculate_stats(self, candles, period, modeling_bars, spread_threshold):
        stats = {
            'is_valid': False,
            'period': period,
            'current_decision': _DECISION_UNKNOWN,
            'buy_possibility': 0.0,
            'sell_possibility': 0.0,
            'undefined_possibility': 0.0,
            'buy_possibility_quality': 0,
            'sell_possibility_quality': 0,
            'undefined_possibility_quality': 0,
            'buy_possibility_mid': 0.0,
            'sell_possibility_mid': 0.0,
            'undefined_possibility_mid': 0.0,
            'buy_suc_possibility_mid': 0.0,
            'sell_suc_possibility_mid': 0.0,
            'undefined_suc_possibility_mid': 0.0,
            'buy_suc_possibility_quality': 0,
            'sell_suc_possibility_quality': 0,
            'undefined_suc_possibility_quality': 0,
            'possibility_success_ratio': 0.0,
        }

        buy_quality = 0
        sell_quality = 0
        undefined_quality = 0
        buy_sum = 0.0
        sell_sum = 0.0
        undefined_sum = 0.0
        buy_success_sum = 0.0
        sell_success_sum = 0.0
        undefined_success_sum = 0.0

        for shift in range(modeling_bars):
            current_index = len(candles) - 1 - period * shift
            previous_index = current_index - period
            if previous_index < 0:
                return None

            current = candles[current_index]
            previous = candles[previous_index]

            decision_value = current[3] - current[0]  # close - open
            previous_value = previous[3] - previous[0]

            buy_poss = 0.0
            sell_poss = 0.0
            undef_poss = 0.0
            decision = _DECISION_UNKNOWN

            if decision_value > 0:
                if previous_value < 0:
                    decision = _DECISION_SELL
                    sell_poss = decision_value
                else:
                    undef_poss = decision_value
            elif decision_value < 0:
                if previous_value > 0:
                    decision = _DECISION_BUY
                    buy_poss = -decision_value
                else:
                    undef_poss = -decision_value

            if shift == 0:
                stats['current_decision'] = decision
                stats['buy_possibility'] = buy_poss
                stats['sell_possibility'] = sell_poss
                stats['undefined_possibility'] = undef_poss

            if decision == _DECISION_BUY:
                buy_quality += 1
                buy_sum += buy_poss
                if buy_poss > spread_threshold:
                    buy_success_sum += buy_poss
                    stats['buy_suc_possibility_quality'] += 1
            elif decision == _DECISION_SELL:
                sell_quality += 1
                sell_sum += sell_poss
                if sell_poss > spread_threshold:
                    sell_success_sum += sell_poss
                    stats['sell_suc_possibility_quality'] += 1
            else:
                undefined_quality += 1
                undefined_sum += undef_poss
                if undef_poss > spread_threshold:
                    undefined_success_sum += undef_poss
                    stats['undefined_suc_possibility_quality'] += 1

        stats['buy_possibility_quality'] = buy_quality
        stats['sell_possibility_quality'] = sell_quality
        stats['undefined_possibility_quality'] = undefined_quality

        stats['buy_possibility_mid'] = buy_sum / buy_quality if buy_quality > 0 else 0.0
        stats['sell_possibility_mid'] = sell_sum / sell_quality if sell_quality > 0 else 0.0
        stats['undefined_possibility_mid'] = undefined_sum / undefined_quality if undefined_quality > 0 else 0.0

        bsc = stats['buy_suc_possibility_quality']
        ssc = stats['sell_suc_possibility_quality']
        usc = stats['undefined_suc_possibility_quality']

        stats['buy_suc_possibility_mid'] = buy_success_sum / bsc if bsc > 0 else 0.0
        stats['sell_suc_possibility_mid'] = sell_success_sum / ssc if ssc > 0 else 0.0
        stats['undefined_suc_possibility_mid'] = undefined_success_sum / usc if usc > 0 else 0.0

        success_total = bsc + ssc + usc
        if success_total > 0:
            stats['possibility_success_ratio'] = (bsc + ssc) / float(success_total)
        else:
            stats['possibility_success_ratio'] = 0.0

        stats['is_valid'] = (buy_quality + sell_quality + undefined_quality) > 0
        return stats

    def OnReseted(self):
        super(cyberia_trader_ai_strategy, self).OnReseted()
        self._history = deque()
        self._previous_ema = None
        self._previous_period = None
        self._current_stats = None

    def CreateClone(self):
        return cyberia_trader_ai_strategy()
