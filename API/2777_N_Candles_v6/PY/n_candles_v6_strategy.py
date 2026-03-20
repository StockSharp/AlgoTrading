import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class n_candles_v6_strategy(Strategy):
    CLOSE_ALL = 0
    CLOSE_OPPOSITE = 1
    CLOSE_UNIDIRECTIONAL = 2

    def __init__(self):
        super(n_candles_v6_strategy, self).__init__()
        self._candles_count = self.Param("CandlesCount", 4)
        self._order_volume = self.Param("OrderVolume", 0.01)
        self._take_profit_pips = self.Param("TakeProfitPips", 50.0)
        self._stop_loss_pips = self.Param("StopLossPips", 50.0)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 10.0)
        self._trailing_step_pips = self.Param("TrailingStepPips", 4.0)
        self._max_position_volume = self.Param("MaxPositionVolume", 2.0)
        self._use_trading_hours = self.Param("UseTradingHours", False)
        self._start_hour = self.Param("StartHour", 11)
        self._end_hour = self.Param("EndHour", 18)
        self._closing_mode = self.Param("ClosingMode", self.CLOSE_ALL)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._pip_size = 0.0
        self._entry_price = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._trailing_long = None
        self._trailing_short = None
        self._streak_direction = 0
        self._bull_count = 0
        self._bear_count = 0
        self._black_sheep_triggered = False

    @property
    def CandlesCount(self):
        return self._candles_count.Value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    @property
    def MaxPositionVolume(self):
        return self._max_position_volume.Value

    @property
    def UseTradingHours(self):
        return self._use_trading_hours.Value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @property
    def EndHour(self):
        return self._end_hour.Value

    @property
    def ClosingMode(self):
        return self._closing_mode.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(n_candles_v6_strategy, self).OnStarted(time)
        self.Volume = float(self.OrderVolume)
        self._pip_size = self._calculate_pip_size()
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_trailing_levels(candle)
        if self._apply_risk_management(candle):
            return

        direction = self._get_direction(candle)

        if direction == 0:
            if self._streak_direction != 0 and not self._black_sheep_triggered:
                self._handle_black_sheep(self._streak_direction)
            self._reset_counters()
            return

        if self._streak_direction == direction:
            if direction == 1:
                self._bull_count = min(self.CandlesCount, self._bull_count + 1)
                self._bear_count = 0
            else:
                self._bear_count = min(self.CandlesCount, self._bear_count + 1)
                self._bull_count = 0
        else:
            if self._streak_direction != 0 and not self._black_sheep_triggered:
                self._handle_black_sheep(self._streak_direction)
            self._streak_direction = direction
            self._bull_count = 1 if direction == 1 else 0
            self._bear_count = 1 if direction == -1 else 0

        allow_trading = not self.UseTradingHours or self._is_within_trading_hours(candle.OpenTime)

        if self._bull_count >= self.CandlesCount and allow_trading:
            self._enter_long(float(candle.ClosePrice))
        elif self._bear_count >= self.CandlesCount and allow_trading:
            self._enter_short(float(candle.ClosePrice))

    def _enter_long(self, price):
        vol = float(self.OrderVolume)
        if vol <= 0:
            return
        pos = float(self.Position)
        if pos < 0:
            vol += abs(pos)
        projected = pos + vol
        if projected > float(self.MaxPositionVolume):
            return
        self.BuyMarket(vol)
        self._entry_price = price
        self._stop_loss_price = price - self._get_price_offset(float(self.StopLossPips)) if float(self.StopLossPips) > 0 else None
        self._take_profit_price = price + self._get_price_offset(float(self.TakeProfitPips)) if float(self.TakeProfitPips) > 0 else None
        self._trailing_long = None
        self._trailing_short = None
        self._black_sheep_triggered = False

    def _enter_short(self, price):
        vol = float(self.OrderVolume)
        if vol <= 0:
            return
        pos = float(self.Position)
        if pos > 0:
            vol += abs(pos)
        projected = pos - vol
        if abs(projected) > float(self.MaxPositionVolume):
            return
        self.SellMarket(vol)
        self._entry_price = price
        self._stop_loss_price = price + self._get_price_offset(float(self.StopLossPips)) if float(self.StopLossPips) > 0 else None
        self._take_profit_price = price - self._get_price_offset(float(self.TakeProfitPips)) if float(self.TakeProfitPips) > 0 else None
        self._trailing_long = None
        self._trailing_short = None
        self._black_sheep_triggered = False

    def _handle_black_sheep(self, direction):
        if direction == 0 or self._black_sheep_triggered:
            return
        pos = float(self.Position)
        mode = self.ClosingMode
        if mode == self.CLOSE_ALL:
            self._close_position()
        elif mode == self.CLOSE_OPPOSITE:
            if direction == 1 and pos < 0:
                self.BuyMarket(abs(pos))
                self._reset_position_state()
            elif direction == -1 and pos > 0:
                self.SellMarket(abs(pos))
                self._reset_position_state()
        elif mode == self.CLOSE_UNIDIRECTIONAL:
            if direction == 1 and pos > 0:
                self.SellMarket(abs(pos))
                self._reset_position_state()
            elif direction == -1 and pos < 0:
                self.BuyMarket(abs(pos))
                self._reset_position_state()
        self._black_sheep_triggered = True

    def _close_position(self):
        pos = float(self.Position)
        if pos > 0:
            self.SellMarket(abs(pos))
            self._reset_position_state()
        elif pos < 0:
            self.BuyMarket(abs(pos))
            self._reset_position_state()

    def _update_trailing_levels(self, candle):
        trailing_stop = self._get_price_offset(float(self.TrailingStopPips))
        if trailing_stop <= 0:
            return
        trailing_step = self._get_price_offset(float(self.TrailingStepPips))
        pos = float(self.Position)
        if pos > 0:
            profit = float(candle.ClosePrice) - self._entry_price
            if profit > trailing_stop + trailing_step:
                candidate = float(candle.ClosePrice) - trailing_stop
                if self._trailing_long is None or candidate > self._trailing_long + trailing_step:
                    self._trailing_long = candidate
        elif pos < 0:
            profit = self._entry_price - float(candle.ClosePrice)
            if profit > trailing_stop + trailing_step:
                candidate = float(candle.ClosePrice) + trailing_stop
                if self._trailing_short is None or candidate < self._trailing_short - trailing_step:
                    self._trailing_short = candidate

    def _apply_risk_management(self, candle):
        pos = float(self.Position)
        if pos > 0:
            if self._stop_loss_price is not None and float(candle.LowPrice) <= self._stop_loss_price:
                self.SellMarket(abs(pos))
                self._reset_position_state()
                return True
            if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(abs(pos))
                self._reset_position_state()
                return True
            if self._trailing_long is not None and float(candle.LowPrice) <= self._trailing_long:
                self.SellMarket(abs(pos))
                self._reset_position_state()
                return True
        elif pos < 0:
            ap = abs(pos)
            if self._stop_loss_price is not None and float(candle.HighPrice) >= self._stop_loss_price:
                self.BuyMarket(ap)
                self._reset_position_state()
                return True
            if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(ap)
                self._reset_position_state()
                return True
            if self._trailing_short is not None and float(candle.HighPrice) >= self._trailing_short:
                self.BuyMarket(ap)
                self._reset_position_state()
                return True
        return False

    def _reset_counters(self):
        self._streak_direction = 0
        self._bull_count = 0
        self._bear_count = 0

    def _reset_position_state(self):
        self._entry_price = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._trailing_long = None
        self._trailing_short = None

    def _is_within_trading_hours(self, time):
        hour = time.TimeOfDay.Hours
        return hour >= self.StartHour and hour <= self.EndHour

    def _get_price_offset(self, pips):
        if pips <= 0:
            return 0.0
        return pips * self._pip_size

    def _get_direction(self, candle):
        if float(candle.ClosePrice) > float(candle.OpenPrice):
            return 1
        if float(candle.ClosePrice) < float(candle.OpenPrice):
            return -1
        return 0

    def _calculate_pip_size(self):
        sec = self.Security
        if sec is None:
            return 1.0
        step = float(sec.PriceStep) if sec.PriceStep is not None else 1.0
        if step <= 0:
            return 1.0
        decimals = self._count_decimals(step)
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def _count_decimals(self, value):
        value = abs(value)
        count = 0
        while value != int(value) and count < 10:
            value *= 10.0
            count += 1
        return count

    def OnReseted(self):
        super(n_candles_v6_strategy, self).OnReseted()
        self._pip_size = 0.0
        self._reset_counters()
        self._reset_position_state()
        self._black_sheep_triggered = False

    def CreateClone(self):
        return n_candles_v6_strategy()
