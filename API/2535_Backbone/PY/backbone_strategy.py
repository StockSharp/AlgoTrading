import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class backbone_strategy(Strategy):
    def __init__(self):
        super(backbone_strategy, self).__init__()

        self._max_risk = self.Param("MaxRisk", 0.5)
        self._max_trades = self.Param("MaxTrades", 1)
        self._take_profit_pips = self.Param("TakeProfitPips", 170.0)
        self._stop_loss_pips = self.Param("StopLossPips", 40.0)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 300.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1)))

        self._bid_max = -1e18
        self._ask_min = 1e18
        self._last_direction = 0
        self._current_direction = 0
        self._long_count = 0
        self._short_count = 0
        self._long_avg_price = 0.0
        self._short_avg_price = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._adjusted_point = 0.0

    @property
    def MaxRisk(self):
        return self._max_risk.Value

    @MaxRisk.setter
    def MaxRisk(self, value):
        self._max_risk.Value = value

    @property
    def MaxTrades(self):
        return self._max_trades.Value

    @MaxTrades.setter
    def MaxTrades(self, value):
        self._max_trades.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @TrailingStopPips.setter
    def TrailingStopPips(self, value):
        self._trailing_stop_pips.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(backbone_strategy, self).OnStarted2(time)

        self._reset_state()
        self._adjusted_point = self._get_adjusted_point()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        # protection handled manually via SL/TP/trailing

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._adjusted_point <= 0.0:
            self._adjusted_point = self._get_adjusted_point()

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        self._update_extreme_levels(candle)

        if self._current_direction == 1:
            if self._handle_long_exit(candle):
                return
        elif self._current_direction == -1:
            if self._handle_short_exit(candle):
                return
        else:
            self._reset_long_state()
            self._reset_short_state()

        if self._should_enter_long():
            self._enter_long(candle)
        elif self._should_enter_short():
            self._enter_short(candle)

    def _enter_long(self, candle):
        close = float(candle.ClosePrice)

        if self._current_direction == -1:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._reset_short_state()
            self._current_direction = 0

        self.BuyMarket()
        self._long_count += 1
        self._current_direction = 1

        if self._long_count == 1:
            self._long_avg_price = close
        else:
            self._long_avg_price = (self._long_avg_price * (self._long_count - 1) + close) / self._long_count

        sl_pips = float(self.StopLossPips)
        tp_pips = float(self.TakeProfitPips)

        if sl_pips > 0.0 and self._adjusted_point > 0.0:
            self._long_stop = self._long_avg_price - sl_pips * self._adjusted_point
        else:
            self._long_stop = None

        if tp_pips > 0.0 and self._adjusted_point > 0.0:
            self._long_take = self._long_avg_price + tp_pips * self._adjusted_point
        else:
            self._long_take = None

        self._last_direction = 1

    def _enter_short(self, candle):
        close = float(candle.ClosePrice)

        if self._current_direction == 1:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._reset_long_state()
            self._current_direction = 0

        self.SellMarket()
        self._short_count += 1
        self._current_direction = -1

        if self._short_count == 1:
            self._short_avg_price = close
        else:
            self._short_avg_price = (self._short_avg_price * (self._short_count - 1) + close) / self._short_count

        sl_pips = float(self.StopLossPips)
        tp_pips = float(self.TakeProfitPips)

        if sl_pips > 0.0 and self._adjusted_point > 0.0:
            self._short_stop = self._short_avg_price + sl_pips * self._adjusted_point
        else:
            self._short_stop = None

        if tp_pips > 0.0 and self._adjusted_point > 0.0:
            self._short_take = self._short_avg_price - tp_pips * self._adjusted_point
        else:
            self._short_take = None

        self._last_direction = -1

    def _handle_long_exit(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        exit_triggered = False

        if self._long_take is not None and high >= self._long_take:
            self.SellMarket()
            exit_triggered = True
        elif self._long_stop is not None and low <= self._long_stop:
            self.SellMarket()
            exit_triggered = True
        else:
            trail_pips = float(self.TrailingStopPips)
            sl_pips = float(self.StopLossPips)
            if trail_pips > 0.0 and sl_pips > 0.0 and self._long_count > 0 and self._adjusted_point > 0.0:
                trail_distance = trail_pips * self._adjusted_point
                profit = close - self._long_avg_price
                if trail_distance > 0.0 and profit > trail_distance:
                    new_stop = close - trail_distance
                    if self._long_stop is None or self._long_stop < new_stop:
                        self._long_stop = new_stop

        if exit_triggered:
            self._reset_long_state()
            self._current_direction = 0
            return True
        return False

    def _handle_short_exit(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        exit_triggered = False

        if self._short_take is not None and low <= self._short_take:
            self.BuyMarket()
            exit_triggered = True
        elif self._short_stop is not None and high >= self._short_stop:
            self.BuyMarket()
            exit_triggered = True
        else:
            trail_pips = float(self.TrailingStopPips)
            sl_pips = float(self.StopLossPips)
            if trail_pips > 0.0 and sl_pips > 0.0 and self._short_count > 0 and self._adjusted_point > 0.0:
                trail_distance = trail_pips * self._adjusted_point
                profit = self._short_avg_price - close
                if trail_distance > 0.0 and profit > trail_distance:
                    new_stop = close + trail_distance
                    if self._short_stop is None or self._short_stop > new_stop:
                        self._short_stop = new_stop

        if exit_triggered:
            self._reset_short_state()
            self._current_direction = 0
            return True
        return False

    def _should_enter_long(self):
        open_positions = self._long_count if self._current_direction == 1 else 0
        max_trades = int(self.MaxTrades)
        if max_trades <= 0:
            return False
        first_entry = self._last_direction == -1 and open_positions == 0
        add_entry = self._last_direction == 1 and open_positions > 0 and open_positions < max_trades
        return first_entry or add_entry

    def _should_enter_short(self):
        open_positions = self._short_count if self._current_direction == -1 else 0
        max_trades = int(self.MaxTrades)
        if max_trades <= 0:
            return False
        first_entry = self._last_direction == 1 and open_positions == 0
        add_entry = self._last_direction == -1 and open_positions > 0 and open_positions < max_trades
        return first_entry or add_entry

    def _update_extreme_levels(self, candle):
        if self._last_direction != 0:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        trail_pips = float(self.TrailingStopPips)
        trail_distance = trail_pips * self._adjusted_point
        if trail_distance <= 0.0:
            return

        if high > self._bid_max:
            self._bid_max = high
        if low < self._ask_min:
            self._ask_min = low

        if self._bid_max > -1e17 and low < self._bid_max - trail_distance:
            self._last_direction = -1
            return

        if self._ask_min < 1e17 and high > self._ask_min + trail_distance:
            self._last_direction = 1

    def _get_adjusted_point(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
        if step <= 0.0:
            return 1.0
        return step

    def _reset_state(self):
        self._bid_max = -1e18
        self._ask_min = 1e18
        self._last_direction = 0
        self._current_direction = 0
        self._reset_long_state()
        self._reset_short_state()
        self._adjusted_point = 0.0

    def _reset_long_state(self):
        self._long_count = 0
        self._long_avg_price = 0.0
        self._long_stop = None
        self._long_take = None

    def _reset_short_state(self):
        self._short_count = 0
        self._short_avg_price = 0.0
        self._short_stop = None
        self._short_take = None

    def OnReseted(self):
        super(backbone_strategy, self).OnReseted()
        self._reset_state()

    def CreateClone(self):
        return backbone_strategy()
