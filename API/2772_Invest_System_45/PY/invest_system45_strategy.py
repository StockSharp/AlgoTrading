import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class invest_system45_strategy(Strategy):

    def __init__(self):
        super(invest_system45_strategy, self).__init__()
        self._stop_loss_pips = self.Param("StopLossPips", 240)
        self._take_profit_pips = self.Param("TakeProfitPips", 40)
        self._entry_window_minutes = self.Param("EntryWindowMinutes", 15)
        self._signal_candle_type = self.Param("SignalCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._trend_candle_type = self.Param("TrendCandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._base_lot = self.Param("BaseLot", 0.1)

        self._pip_size = 0.0
        self._min_balance = 0.0
        self._max_balance = 0.0
        self._lot_stage = 1
        self._plan_b_active = False
        self._stage_lot1 = 0.0
        self._stage_lot2 = 0.0
        self._stage_lot3 = 0.0
        self._stage_lot4 = 0.0
        self._lot_option1 = 0.0
        self._lot_option2 = 0.0
        self._current_volume = 0.0
        self._needs_post_trade_adjustment = False
        self._has_open_position = False
        self._pnl_at_entry = 0.0
        self._last_trade_pnl = 0.0
        self._trend_direction = 0
        self._entry_window_start = None
        self._entry_window_end = None
        self._entry_window_active = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def EntryWindowMinutes(self):
        return self._entry_window_minutes.Value

    @property
    def SignalCandleType(self):
        return self._signal_candle_type.Value

    @property
    def TrendCandleType(self):
        return self._trend_candle_type.Value

    @property
    def BaseLot(self):
        return self._base_lot.Value

    def OnStarted(self, time):
        super(invest_system45_strategy, self).OnStarted(time)
        self._reset_state()
        self._pip_size = self._calculate_pip_size()
        self._recalculate_lot_options()

        trend_sub = self.SubscribeCandles(self.TrendCandleType)
        trend_sub.Bind(self._process_trend_candle).Start()

        entry_sub = self.SubscribeCandles(self.SignalCandleType)
        entry_sub.Bind(self._process_entry_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, entry_sub)
            self.DrawOwnTrades(area)

    def _process_trend_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if float(candle.ClosePrice) > float(candle.OpenPrice):
            self._trend_direction = 1
        elif float(candle.ClosePrice) < float(candle.OpenPrice):
            self._trend_direction = -1

        self._entry_window_start = candle.CloseTime
        self._entry_window_end = candle.CloseTime.AddMinutes(self.EntryWindowMinutes)
        self._entry_window_active = True

    def _process_entry_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        pos = float(self.Position)

        # SL/TP management
        if pos > 0 and self._entry_price > 0:
            if self._stop_price > 0 and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(pos)
                self._reset_targets()
                return
            if self._take_price > 0 and float(candle.HighPrice) >= self._take_price:
                self.SellMarket(pos)
                self._reset_targets()
                return
        elif pos < 0 and self._entry_price > 0:
            if self._stop_price > 0 and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(abs(pos))
                self._reset_targets()
                return
            if self._take_price > 0 and float(candle.LowPrice) <= self._take_price:
                self.BuyMarket(abs(pos))
                self._reset_targets()
                return

        self._update_balance_state()

        if not self._entry_window_active or self._entry_window_start is None or self._entry_window_end is None:
            return

        open_time = candle.OpenTime
        if open_time < self._entry_window_start:
            return
        if open_time > self._entry_window_end:
            self._entry_window_active = False
            return
        if self._trend_direction == 0:
            return
        if float(self.Position) != 0:
            return

        if self._current_volume <= 0:
            self._current_volume = self._lot_option1
        if self._current_volume <= 0:
            return

        if self._trend_direction > 0:
            self.BuyMarket(self._current_volume)
        else:
            self.SellMarket(self._current_volume)

        self._entry_window_active = False

    def _update_balance_state(self):
        portfolio = self.Portfolio
        if portfolio is None:
            return
        balance = portfolio.CurrentValue
        if balance is None or float(balance) <= 0:
            return
        bal = float(balance)

        if self._min_balance <= 0:
            self._min_balance = bal
            self._max_balance = bal

        if bal > self._max_balance:
            self._max_balance = bal
            if self._plan_b_active:
                self._plan_b_active = False
                self._recalculate_lot_options()

        new_stage = 1
        if self._min_balance > 0:
            for stage in range(6, 1, -1):
                if bal > self._min_balance * stage:
                    new_stage = stage
                    break

        if new_stage != self._lot_stage:
            self._lot_stage = new_stage
            self._recalculate_lot_options()

    def _calculate_pip_size(self):
        sec = self.Security
        if sec is None:
            return 1.0
        step = float(sec.PriceStep) if sec.PriceStep is not None else 1.0
        decimals = int(sec.Decimals) if sec.Decimals is not None else 0
        if decimals == 3 or decimals == 5:
            step *= 10.0
        return step

    def _recalculate_lot_options(self):
        base_lot = float(self.BaseLot) * self._lot_stage
        self._stage_lot1 = base_lot
        self._stage_lot2 = base_lot * 2.0
        self._stage_lot3 = base_lot * 7.0
        self._stage_lot4 = base_lot * 14.0

        if self._plan_b_active:
            self._lot_option1 = self._stage_lot2
            self._lot_option2 = self._stage_lot4
        else:
            self._lot_option1 = self._stage_lot1
            self._lot_option2 = self._stage_lot3

        if self._current_volume <= 0:
            self._current_volume = self._lot_option1

    def _handle_post_trade_adjustment(self):
        if not self._needs_post_trade_adjustment:
            return
        self._needs_post_trade_adjustment = False
        self._update_balance_state()

        if self._last_trade_pnl < 0:
            if self._current_volume == self._lot_option2 and not self._plan_b_active:
                self._plan_b_active = True
                self._recalculate_lot_options()
            else:
                self._current_volume = self._lot_option2
        elif self._last_trade_pnl > 0:
            self._current_volume = self._lot_option1

    def OnOwnTradeReceived(self, trade):
        super(invest_system45_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Trade is None:
            return
        pos = float(self.Position)
        if pos != 0 and self._entry_price == 0:
            self._entry_price = float(trade.Trade.Price)
            sl_dist = self.StopLossPips * self._pip_size
            tp_dist = self.TakeProfitPips * self._pip_size
            if pos > 0:
                self._stop_price = self._entry_price - sl_dist if sl_dist > 0 else 0.0
                self._take_price = self._entry_price + tp_dist if tp_dist > 0 else 0.0
            else:
                self._stop_price = self._entry_price + sl_dist if sl_dist > 0 else 0.0
                self._take_price = self._entry_price - tp_dist if tp_dist > 0 else 0.0
        if pos == 0:
            self._reset_targets()

    def OnPositionReceived(self, position):
        super(invest_system45_strategy, self).OnPositionReceived(position)
        pos = float(self.Position)
        if pos != 0:
            if not self._has_open_position:
                self._has_open_position = True
                self._needs_post_trade_adjustment = True
                self._pnl_at_entry = float(self.PnL)
            self._entry_window_active = False
            return
        if not self._has_open_position:
            return
        self._has_open_position = False
        self._last_trade_pnl = float(self.PnL) - self._pnl_at_entry
        self._handle_post_trade_adjustment()

    def _reset_targets(self):
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def _reset_state(self):
        self._pip_size = 0.0
        self._min_balance = 0.0
        self._max_balance = 0.0
        self._lot_stage = 1
        self._plan_b_active = False
        self._stage_lot1 = 0.0
        self._stage_lot2 = 0.0
        self._stage_lot3 = 0.0
        self._stage_lot4 = 0.0
        self._lot_option1 = 0.0
        self._lot_option2 = 0.0
        self._current_volume = 0.0
        self._needs_post_trade_adjustment = False
        self._has_open_position = False
        self._pnl_at_entry = 0.0
        self._last_trade_pnl = 0.0
        self._trend_direction = 0
        self._entry_window_start = None
        self._entry_window_end = None
        self._entry_window_active = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(invest_system45_strategy, self).OnReseted()
        self._reset_state()

    def CreateClone(self):
        return invest_system45_strategy()
