import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    RelativeStrengthIndex,
    MoneyFlowIndex,
    AverageTrueRange,
)


class self_optimizing_rsi_or_mfi_trader_v3_strategy(Strategy):
    """Self-optimizing RSI/MFI: dynamically finds best overbought/oversold levels over rolling history."""

    def __init__(self):
        super(self_optimizing_rsi_or_mfi_trader_v3_strategy, self).__init__()

        self._optimizing_periods = self.Param("OptimizingPeriods", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Optimization Bars", "Number of bars used for optimization", "General")
        self._use_aggressive_entries = self.Param("UseAggressiveEntries", False) \
            .SetDisplay("Aggressive Entries", "Allow entries without indicator crosses", "Trading")
        self._trade_reverse = self.Param("TradeReverse", False) \
            .SetDisplay("Reverse Trading", "Swap profitability preference for opposite trades", "Trading")
        self._one_order_at_a_time = self.Param("OneOrderAtATime", True) \
            .SetDisplay("One Position", "Permit only one open position", "Trading")
        self._base_volume = self.Param("BaseVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Base Volume", "Static order volume when sizing manually", "Risk")
        self._use_dynamic_volume = self.Param("UseDynamicVolume", True) \
            .SetDisplay("Dynamic Volume", "Use risk percentage for position sizing", "Risk")
        self._risk_percent = self.Param("RiskPercent", 2.0) \
            .SetDisplay("Risk %", "Percent of capital risked per trade", "Risk")
        # 0=RSI, 1=MFI
        self._indicator_choice = self.Param("IndicatorChoice", 0) \
            .SetDisplay("Indicator", "0=RSI, 1=MFI", "Indicator")
        self._indicator_top_value = self.Param("IndicatorTopValue", 100) \
            .SetDisplay("Top Level", "Upper bound for level search", "Indicator")
        self._indicator_bottom_value = self.Param("IndicatorBottomValue", 0) \
            .SetDisplay("Bottom Level", "Lower bound for level search", "Indicator")
        self._indicator_period = self.Param("IndicatorPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Indicator Period", "Averaging period for RSI or MFI", "Indicator")
        self._use_dynamic_targets = self.Param("UseDynamicTargets", True) \
            .SetDisplay("Dynamic Targets", "Use ATR based stop-loss and take-profit", "Risk")
        self._static_stop_loss_points = self.Param("StaticStopLossPoints", 1000) \
            .SetGreaterThanZero() \
            .SetDisplay("Static Stop", "Stop-loss in points when dynamic targets disabled", "Risk")
        self._static_take_profit_points = self.Param("StaticTakeProfitPoints", 2000) \
            .SetGreaterThanZero() \
            .SetDisplay("Static Take", "Take-profit in points when dynamic targets disabled", "Risk")
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Stop Mult", "Stop-loss multiplier applied to ATR", "Risk")
        self._take_profit_atr_multiplier = self.Param("TakeProfitAtrMultiplier", 7.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Take Mult", "Take-profit multiplier applied to ATR", "Risk")
        self._use_break_even = self.Param("UseBreakEven", True) \
            .SetDisplay("Use Breakeven", "Move stop to breakeven after trigger", "Risk")
        self._break_even_trigger_points = self.Param("BreakEvenTriggerPoints", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("Breakeven Trigger", "Profit in points required to arm breakeven", "Risk")
        self._break_even_padding_points = self.Param("BreakEvenPaddingPoints", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Breakeven Padding", "Padding in points applied after trigger", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe used for analysis", "General")

        self._history = []
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    @property
    def OptimizingPeriods(self):
        return int(self._optimizing_periods.Value)
    @property
    def UseAggressiveEntries(self):
        return self._use_aggressive_entries.Value
    @property
    def TradeReverse(self):
        return self._trade_reverse.Value
    @property
    def OneOrderAtATime(self):
        return self._one_order_at_a_time.Value
    @property
    def BaseVolume(self):
        return float(self._base_volume.Value)
    @property
    def UseDynamicVolume(self):
        return self._use_dynamic_volume.Value
    @property
    def RiskPercent(self):
        return float(self._risk_percent.Value)
    @property
    def IndicatorChoice(self):
        return int(self._indicator_choice.Value)
    @property
    def IndicatorTopValue(self):
        return int(self._indicator_top_value.Value)
    @property
    def IndicatorBottomValue(self):
        return int(self._indicator_bottom_value.Value)
    @property
    def IndicatorPeriod(self):
        return int(self._indicator_period.Value)
    @property
    def UseDynamicTargets(self):
        return self._use_dynamic_targets.Value
    @property
    def StaticStopLossPoints(self):
        return int(self._static_stop_loss_points.Value)
    @property
    def StaticTakeProfitPoints(self):
        return int(self._static_take_profit_points.Value)
    @property
    def StopLossAtrMultiplier(self):
        return float(self._stop_loss_atr_multiplier.Value)
    @property
    def TakeProfitAtrMultiplier(self):
        return float(self._take_profit_atr_multiplier.Value)
    @property
    def UseBreakEven(self):
        return self._use_break_even.Value
    @property
    def BreakEvenTriggerPoints(self):
        return int(self._break_even_trigger_points.Value)
    @property
    def BreakEvenPaddingPoints(self):
        return int(self._break_even_padding_points.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(self_optimizing_rsi_or_mfi_trader_v3_strategy, self).OnStarted2(time)

        self._history = []
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

        if self.IndicatorChoice == 1:
            self._indicator = MoneyFlowIndex()
        else:
            self._indicator = RelativeStrengthIndex()
        self._indicator.Length = self.IndicatorPeriod

        self._atr = AverageTrueRange()
        self._atr.Length = self.IndicatorPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._indicator, self._atr, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._indicator)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, indicator_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        ind_val = float(indicator_value)
        atr_val = float(atr_value)
        close = float(candle.ClosePrice)

        self._history.append((ind_val, close))
        max_needed = max(self.OptimizingPeriods + 1, 3)
        while len(self._history) > max_needed:
            self._history.pop(0)

        sec = self.Security
        price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        if price_step <= 0:
            price_step = 1.0

        trigger_diff = self.BreakEvenTriggerPoints * price_step if self.UseBreakEven else 0.0
        padding_pts = self.BreakEvenPaddingPoints if self.BreakEvenPaddingPoints <= self.BreakEvenTriggerPoints else 0
        padding_diff = padding_pts * price_step if self.UseBreakEven else 0.0

        self._manage_open_position(candle, trigger_diff, padding_diff)

        if len(self._history) < max_needed:
            return

        # Build arrays (newest first)
        indicator_values = []
        close_values = []
        for i in range(len(self._history) - 1, -1, -1):
            indicator_values.append(self._history[i][0])
            close_values.append(self._history[i][1])

        if self.UseDynamicTargets:
            if atr_val <= 0:
                return
            stop_loss_diff = atr_val * self.StopLossAtrMultiplier
            take_profit_diff = atr_val * self.TakeProfitAtrMultiplier
        else:
            stop_loss_diff = self.StaticStopLossPoints * price_step
            take_profit_diff = self.StaticTakeProfitPoints * price_step

        if stop_loss_diff <= 0 or take_profit_diff <= 0:
            return

        volume = self.BaseVolume
        step_multiplier = 1.0

        sell_level, sell_profit = self._calc_best_sell_level(indicator_values, close_values, stop_loss_diff, take_profit_diff, volume, step_multiplier)
        buy_level, buy_profit = self._calc_best_buy_level(indicator_values, close_values, stop_loss_diff, take_profit_diff, volume, step_multiplier)

        adjusted_sell = sell_profit
        adjusted_buy = buy_profit
        if self.TradeReverse:
            adjusted_sell = buy_profit
            adjusted_buy = sell_profit

        can_enter = not self.OneOrderAtATime or self.Position == 0
        current_ind = indicator_values[0]
        prev_ind = indicator_values[1]

        if adjusted_sell > adjusted_buy:
            if can_enter and ((current_ind < sell_level and prev_ind > sell_level) or self.UseAggressiveEntries):
                self._enter_short(candle, stop_loss_diff, take_profit_diff)
        elif adjusted_sell < adjusted_buy:
            if can_enter and ((current_ind > buy_level and prev_ind < buy_level) or self.UseAggressiveEntries):
                self._enter_long(candle, stop_loss_diff, take_profit_diff)

    def _calc_best_sell_level(self, ind_vals, close_vals, sl_diff, tp_diff, volume, step_mult):
        bottom = min(self.IndicatorBottomValue, self.IndicatorTopValue)
        top = max(self.IndicatorBottomValue, self.IndicatorTopValue)
        best_profit = 0.0
        best_level = bottom
        updated = False
        for level in range(bottom, top + 1):
            profit = self._eval_sell(ind_vals, close_vals, level, sl_diff, tp_diff, volume, step_mult)
            if profit > best_profit:
                best_profit = profit
                best_level = level
                updated = True
        return (best_level, best_profit if updated else 0.0)

    def _calc_best_buy_level(self, ind_vals, close_vals, sl_diff, tp_diff, volume, step_mult):
        bottom = min(self.IndicatorBottomValue, self.IndicatorTopValue)
        top = max(self.IndicatorBottomValue, self.IndicatorTopValue)
        best_profit = 0.0
        best_level = top
        updated = False
        for level in range(top, bottom - 1, -1):
            profit = self._eval_buy(ind_vals, close_vals, level, sl_diff, tp_diff, volume, step_mult)
            if profit > best_profit:
                best_profit = profit
                best_level = level
                updated = True
        return (best_level, best_profit if updated else 0.0)

    def _eval_sell(self, ind_vals, close_vals, level, sl_diff, tp_diff, volume, step_mult):
        total = 0.0
        n = len(ind_vals)
        if n < 3:
            return 0.0
        threshold = float(level)
        i = n - 2
        while i >= 2:
            if ind_vals[i] < threshold and ind_vals[i + 1] > threshold:
                entry = close_vals[i]
                j = i - 1
                while j >= 1:
                    price = close_vals[j]
                    if price >= entry + sl_diff:
                        total -= (price - entry) * step_mult * volume
                        i = j
                        break
                    if price <= entry - tp_diff:
                        total += (entry - price) * step_mult * volume
                        i = j
                        break
                    j -= 1
            i -= 1
        return total

    def _eval_buy(self, ind_vals, close_vals, level, sl_diff, tp_diff, volume, step_mult):
        total = 0.0
        n = len(ind_vals)
        if n < 3:
            return 0.0
        threshold = float(level)
        i = n - 2
        while i >= 2:
            if ind_vals[i] > threshold and ind_vals[i + 1] < threshold:
                entry = close_vals[i]
                j = i - 1
                while j >= 1:
                    price = close_vals[j]
                    if price <= entry - sl_diff:
                        total -= (entry - price) * step_mult * volume
                        i = j
                        break
                    if price >= entry + tp_diff:
                        total += (price - entry) * step_mult * volume
                        i = j
                        break
                    j -= 1
            i -= 1
        return total

    def _enter_long(self, candle, sl_diff, tp_diff):
        self.BuyMarket()
        self._entry_price = float(candle.ClosePrice)
        self._stop_price = self._entry_price - sl_diff
        self._take_profit_price = self._entry_price + tp_diff

    def _enter_short(self, candle, sl_diff, tp_diff):
        self.SellMarket()
        self._entry_price = float(candle.ClosePrice)
        self._stop_price = self._entry_price + sl_diff
        self._take_profit_price = self._entry_price - tp_diff

    def _manage_open_position(self, candle, trigger_diff, padding_diff):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self.Position > 0:
            if self.UseBreakEven and self._entry_price is not None and self._stop_price is not None:
                trigger_price = self._entry_price + trigger_diff
                target_stop = self._entry_price + padding_diff
                if trigger_diff > 0 and h >= trigger_price and self._stop_price < target_stop:
                    self._stop_price = target_stop

            if self._stop_price is not None and lo <= self._stop_price:
                self.SellMarket()
                self._reset_position_state()
                return
            if self._take_profit_price is not None and h >= self._take_profit_price:
                self.SellMarket()
                self._reset_position_state()
                return

        elif self.Position < 0:
            if self.UseBreakEven and self._entry_price is not None and self._stop_price is not None:
                trigger_price = self._entry_price - trigger_diff
                target_stop = self._entry_price - padding_diff
                if trigger_diff > 0 and lo <= trigger_price and self._stop_price > target_stop:
                    self._stop_price = target_stop

            if self._stop_price is not None and h >= self._stop_price:
                self.BuyMarket()
                self._reset_position_state()
                return
            if self._take_profit_price is not None and lo <= self._take_profit_price:
                self.BuyMarket()
                self._reset_position_state()
                return
        else:
            self._reset_position_state()

    def _reset_position_state(self):
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    def OnReseted(self):
        super(self_optimizing_rsi_or_mfi_trader_v3_strategy, self).OnReseted()
        self._history = []
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None

    def CreateClone(self):
        return self_optimizing_rsi_or_mfi_trader_v3_strategy()
