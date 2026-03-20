import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import CommodityChannelIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class gold_warrior02b_strategy(Strategy):
    def __init__(self):
        super(gold_warrior02b_strategy, self).__init__()

        self._base_volume = self.Param("BaseVolume", 0.1)
        self._stop_loss_points = self.Param("StopLossPoints", 100.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 150.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 5.0)
        self._trailing_step_points = self.Param("TrailingStepPoints", 5.0)
        self._impulse_period = self.Param("ImpulsePeriod", 21)
        self._zig_zag_depth = self.Param("ZigZagDepth", 12)
        self._zig_zag_deviation = self.Param("ZigZagDeviation", 5.0)
        self._zig_zag_backstep = self.Param("ZigZagBackstep", 3)
        self._profit_target = self.Param("ProfitTarget", 300.0)
        self._impulse_sell_threshold = self.Param("ImpulseSellThreshold", -30.0)
        self._impulse_buy_threshold = self.Param("ImpulseBuyThreshold", 30.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2)))

        self._cci = None

        # Impulse indicator state (inline SMA of (open-close)/step)
        self._impulse_buffer = []
        self._impulse_sum = 0.0
        self._impulse_formed = False

        # ZigZag state
        self._last_zigzag = None
        self._previous_zigzag = None
        self._search_direction = 1
        self._current_extreme = None
        self._bars_since_extreme = 0

        # Previous indicator values
        self._previous_cci = 0.0
        self._previous_impulse = 0.0
        self._has_previous_cci = False
        self._has_previous_impulse = False

        # Position management
        self._last_trade_time = None
        self._entry_price = 0.0
        self._trailing_stop_price = 0.0
        self._trailing_active = False
        self._max_price_since_entry = 0.0
        self._min_price_since_entry = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(gold_warrior02b_strategy, self).OnStarted(time)

        self._cci = CommodityChannelIndex()
        self._cci.Length = self._impulse_period.Value

        self._impulse_buffer = []
        self._impulse_sum = 0.0
        self._impulse_formed = False

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _get_price_step(self):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        return step if step > 0 else 1.0

    def _compute_impulse(self, candle):
        step = self._get_price_step()
        value = (float(candle.OpenPrice) - float(candle.ClosePrice)) / step

        self._impulse_buffer.append(value)
        self._impulse_sum += value

        length = self._impulse_period.Value
        if len(self._impulse_buffer) > length:
            self._impulse_sum -= self._impulse_buffer.pop(0)

        if len(self._impulse_buffer) < length:
            self._impulse_formed = False
            return 0.0

        self._impulse_formed = True
        return self._impulse_sum / length

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        cci_result = self._cci.Process(candle)
        cci_value = float(cci_result) if not cci_result.IsEmpty else 0.0
        impulse_value = self._compute_impulse(candle)

        if not self._cci.IsFormed or not self._impulse_formed:
            self._previous_cci = cci_value
            self._previous_impulse = impulse_value
            self._has_previous_cci = True
            self._has_previous_impulse = True
            self._update_zigzag(candle)
            return

        self._update_zigzag(candle)

        has_zigzag = self._last_zigzag is not None and self._previous_zigzag is not None
        zigzag_up = has_zigzag and self._last_zigzag > self._previous_zigzag
        zigzag_down = has_zigzag and self._last_zigzag < self._previous_zigzag

        if not self._has_previous_cci or not self._has_previous_impulse:
            self._previous_cci = cci_value
            self._previous_impulse = impulse_value
            self._has_previous_cci = True
            self._has_previous_impulse = True
            return

        now = candle.CloseTime
        if self._last_trade_time is not None and (now - self._last_trade_time).TotalSeconds < 15:
            self._previous_cci = cci_value
            self._previous_impulse = impulse_value
            return

        sell_condition1 = cci_value < self._previous_cci and self._previous_cci > 20.0 and impulse_value < 0.0
        sell_condition2 = cci_value > 100.0 and self._previous_cci > cci_value
        buy_condition1 = cci_value > self._previous_cci and self._previous_cci < -20.0 and impulse_value > 0.0
        buy_condition2 = cci_value < -100.0 and self._previous_cci < cci_value

        sell_signal = has_zigzag and zigzag_up and (sell_condition1 or sell_condition2)
        buy_signal = has_zigzag and zigzag_down and (buy_condition1 or buy_condition2)

        if not has_zigzag or self.Position != 0:
            sell_signal = False
            buy_signal = False

        if self.Position == 0:
            if sell_signal:
                self._open_short(candle)
            elif buy_signal:
                self._open_long(candle)

        if self.Position != 0:
            self._handle_active_position(candle, now)

        self._previous_cci = cci_value
        self._previous_impulse = impulse_value

    def _handle_active_position(self, candle, now):
        step = self._get_price_step()

        sl_dist = self._stop_loss_points.Value * step
        tp_dist = self._take_profit_points.Value * step
        trail_dist = self._trailing_stop_points.Value * step
        trail_step_dist = self._trailing_step_points.Value * step

        if self.Position > 0:
            self._max_price_since_entry = max(self._max_price_since_entry, float(candle.HighPrice))

            if sl_dist > 0 and float(candle.LowPrice) <= self._entry_price - sl_dist:
                self.SellMarket(self.Position)
                self._last_trade_time = now
                self._reset_position_state()
                return

            if tp_dist > 0 and float(candle.HighPrice) >= self._entry_price + tp_dist:
                self.SellMarket(self.Position)
                self._last_trade_time = now
                self._reset_position_state()
                return

            if trail_dist > 0:
                move = float(candle.ClosePrice) - self._entry_price
                if move >= trail_dist + trail_step_dist:
                    new_trail = float(candle.ClosePrice) - trail_dist
                    if not self._trailing_active or new_trail > self._trailing_stop_price:
                        self._trailing_stop_price = new_trail
                        self._trailing_active = True

                if self._trailing_active and float(candle.LowPrice) <= self._trailing_stop_price:
                    self.SellMarket(self.Position)
                    self._last_trade_time = now
                    self._reset_position_state()
                    return

        elif self.Position < 0:
            self._min_price_since_entry = min(self._min_price_since_entry, float(candle.LowPrice))

            if sl_dist > 0 and float(candle.HighPrice) >= self._entry_price + sl_dist:
                self.BuyMarket(abs(self.Position))
                self._last_trade_time = now
                self._reset_position_state()
                return

            if tp_dist > 0 and float(candle.LowPrice) <= self._entry_price - tp_dist:
                self.BuyMarket(abs(self.Position))
                self._last_trade_time = now
                self._reset_position_state()
                return

            if trail_dist > 0:
                move = self._entry_price - float(candle.ClosePrice)
                if move >= trail_dist + trail_step_dist:
                    new_trail = float(candle.ClosePrice) + trail_dist
                    if not self._trailing_active or new_trail < self._trailing_stop_price:
                        self._trailing_stop_price = new_trail
                        self._trailing_active = True

                if self._trailing_active and float(candle.HighPrice) >= self._trailing_stop_price:
                    self.BuyMarket(abs(self.Position))
                    self._last_trade_time = now
                    self._reset_position_state()
                    return

        current_pnl = self._calculate_open_pnl(float(candle.ClosePrice), step)
        if self._profit_target.Value > 0 and current_pnl >= self._profit_target.Value:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self._last_trade_time = now
            self._reset_position_state()

    def _calculate_open_pnl(self, close_price, step):
        if self.Position == 0:
            return 0.0
        if step <= 0:
            step = 1.0
        if self.Position > 0:
            diff = close_price - self._entry_price
            return diff / step * step * self.Position
        else:
            diff = self._entry_price - close_price
            return diff / step * step * abs(self.Position)

    def _open_long(self, candle):
        self.BuyMarket(self._base_volume.Value)
        self._entry_price = float(candle.ClosePrice)
        self._max_price_since_entry = float(candle.ClosePrice)
        self._min_price_since_entry = float(candle.ClosePrice)
        self._trailing_active = False
        self._trailing_stop_price = 0.0
        self._last_trade_time = candle.CloseTime

    def _open_short(self, candle):
        self.SellMarket(self._base_volume.Value)
        self._entry_price = float(candle.ClosePrice)
        self._max_price_since_entry = float(candle.ClosePrice)
        self._min_price_since_entry = float(candle.ClosePrice)
        self._trailing_active = False
        self._trailing_stop_price = 0.0
        self._last_trade_time = candle.CloseTime

    def _reset_position_state(self):
        self._entry_price = 0.0
        self._max_price_since_entry = 0.0
        self._min_price_since_entry = 0.0
        self._trailing_active = False
        self._trailing_stop_price = 0.0

    def _update_zigzag(self, candle):
        step = self._get_price_step()
        deviation = self._zig_zag_deviation.Value * step
        min_bars = max(1, max(self._zig_zag_depth.Value, self._zig_zag_backstep.Value))

        if self._current_extreme is None:
            self._current_extreme = float(candle.HighPrice) if self._search_direction > 0 else float(candle.LowPrice)
            self._bars_since_extreme = 0
            return

        if self._search_direction > 0:
            if float(candle.HighPrice) > self._current_extreme:
                self._current_extreme = float(candle.HighPrice)
                self._bars_since_extreme = 0
            else:
                self._bars_since_extreme += 1

            drop = self._current_extreme - float(candle.LowPrice)
            if drop >= deviation and self._bars_since_extreme >= min_bars:
                self._previous_zigzag = self._last_zigzag
                self._last_zigzag = self._current_extreme
                self._search_direction = -1
                self._current_extreme = float(candle.LowPrice)
                self._bars_since_extreme = 0
        else:
            if float(candle.LowPrice) < self._current_extreme:
                self._current_extreme = float(candle.LowPrice)
                self._bars_since_extreme = 0
            else:
                self._bars_since_extreme += 1

            rise = float(candle.HighPrice) - self._current_extreme
            if rise >= deviation and self._bars_since_extreme >= min_bars:
                self._previous_zigzag = self._last_zigzag
                self._last_zigzag = self._current_extreme
                self._search_direction = 1
                self._current_extreme = float(candle.HighPrice)
                self._bars_since_extreme = 0

    def OnReseted(self):
        super(gold_warrior02b_strategy, self).OnReseted()
        self._cci = None
        self._impulse_buffer = []
        self._impulse_sum = 0.0
        self._impulse_formed = False
        self._last_zigzag = None
        self._previous_zigzag = None
        self._search_direction = 1
        self._current_extreme = None
        self._bars_since_extreme = 0
        self._previous_cci = 0.0
        self._previous_impulse = 0.0
        self._has_previous_cci = False
        self._has_previous_impulse = False
        self._last_trade_time = None
        self._reset_position_state()

    def CreateClone(self):
        return gold_warrior02b_strategy()
