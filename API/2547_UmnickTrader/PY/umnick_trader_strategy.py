import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class umnick_trader_strategy(Strategy):
    def __init__(self):
        super(umnick_trader_strategy, self).__init__()

        self._buffer_length = self.Param("BufferLength", 8)
        self._stop_base = self.Param("StopBase", 0.017)
        self._trade_volume = self.Param("TradeVolume", 0.1)
        self._spread = self.Param("Spread", 0.0005)
        self._entry_cooldown_bars = self.Param("EntryCooldownBars", 6)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._profit_buffer = []
        self._loss_buffer = []
        self._last_average_price = 0.0
        self._entry_price = 0.0
        self._take_profit_price = 0.0
        self._stop_loss_price = 0.0
        self._max_profit = 0.0
        self._drawdown = 0.0
        self._last_trade_profit = 0.0
        self._current_index = 0
        self._current_direction = 1
        self._cooldown_remaining = 0
        self._position_active = False
        self._is_long_position = False
        self._position_just_closed = False

    @property
    def BufferLength(self):
        return self._buffer_length.Value

    @BufferLength.setter
    def BufferLength(self, value):
        self._buffer_length.Value = value

    @property
    def StopBase(self):
        return self._stop_base.Value

    @StopBase.setter
    def StopBase(self, value):
        self._stop_base.Value = value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @TradeVolume.setter
    def TradeVolume(self, value):
        self._trade_volume.Value = value

    @property
    def Spread(self):
        return self._spread.Value

    @Spread.setter
    def Spread(self, value):
        self._spread.Value = value

    @property
    def EntryCooldownBars(self):
        return self._entry_cooldown_bars.Value

    @EntryCooldownBars.setter
    def EntryCooldownBars(self, value):
        self._entry_cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _resize_buffers(self):
        length = max(1, int(self.BufferLength))
        self._profit_buffer = [0.0] * length
        self._loss_buffer = [0.0] * length
        self._current_index = 0

    def OnStarted(self, time):
        super(umnick_trader_strategy, self).OnStarted(time)

        self._resize_buffers()
        self._last_average_price = 0.0
        self._entry_price = 0.0
        self._take_profit_price = 0.0
        self._stop_loss_price = 0.0
        self._max_profit = 0.0
        self._drawdown = 0.0
        self._last_trade_profit = 0.0
        self._current_direction = 1
        self._cooldown_remaining = 0
        self._position_active = False
        self._is_long_position = False
        self._position_just_closed = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_open_position(candle)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        average_price = (open_price + high + low + close) / 4.0
        if not self._should_process_average(average_price):
            return

        if self.Position != 0:
            return

        stop_base = float(self.StopBase)
        limit_distance = stop_base
        stop_distance = stop_base

        buffer_length = len(self._profit_buffer)
        if buffer_length == 0:
            return

        sum_profit = 0.0
        sum_loss = 0.0
        for i in range(buffer_length):
            sum_profit += self._profit_buffer[i]
            sum_loss += self._loss_buffer[i]

        if sum_profit > stop_base / 2.0:
            limit_distance = sum_profit / buffer_length

        if sum_loss > stop_base / 2.0:
            stop_distance = sum_loss / buffer_length

        if self._position_just_closed:
            self._position_just_closed = False
            spread = float(self.Spread)

            if self._last_trade_profit > 0.0:
                self._profit_buffer[self._current_index] = self._max_profit - spread * 3.0
                self._loss_buffer[self._current_index] = stop_base + spread * 7.0
            else:
                self._profit_buffer[self._current_index] = stop_base - spread * 3.0
                self._loss_buffer[self._current_index] = self._drawdown + spread * 7.0
                self._current_direction = -self._current_direction

            self._current_index += 1
            if self._current_index >= buffer_length:
                self._current_index = 0

            self._cooldown_remaining = int(self.EntryCooldownBars)
            return

        if limit_distance <= 0.0 or stop_distance <= 0.0:
            return

        if self._cooldown_remaining > 0:
            return

        if self._current_direction > 0:
            self._open_long(close, limit_distance, stop_distance)
        else:
            self._open_short(close, limit_distance, stop_distance)

    def _should_process_average(self, average_price):
        if self._last_average_price == 0.0:
            self._last_average_price = average_price
            return True

        difference = abs(average_price - self._last_average_price)
        if difference >= float(self.StopBase):
            self._last_average_price = average_price
            return True

        return False

    def _update_open_position(self, candle):
        if not self._position_active:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self._is_long_position:
            profit_move = high - self._entry_price
            if profit_move > self._max_profit:
                self._max_profit = profit_move

            loss_move = self._entry_price - low
            if loss_move > self._drawdown:
                self._drawdown = loss_move

            if low <= self._stop_loss_price:
                self._close_current_position(self._stop_loss_price)
                return

            if high >= self._take_profit_price:
                self._close_current_position(self._take_profit_price)
                return
        else:
            profit_move = self._entry_price - low
            if profit_move > self._max_profit:
                self._max_profit = profit_move

            loss_move = high - self._entry_price
            if loss_move > self._drawdown:
                self._drawdown = loss_move

            if high >= self._stop_loss_price:
                self._close_current_position(self._stop_loss_price)
                return

            if low <= self._take_profit_price:
                self._close_current_position(self._take_profit_price)
                return

    def _close_current_position(self, exit_price):
        if self._is_long_position:
            profit = exit_price - self._entry_price
        else:
            profit = self._entry_price - exit_price

        self._position_active = False
        self._is_long_position = False
        self._entry_price = 0.0
        self._take_profit_price = 0.0
        self._stop_loss_price = 0.0

        self._last_trade_profit = profit
        self._position_just_closed = True

        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

    def _open_long(self, price, limit_distance, stop_distance):
        self.BuyMarket()
        self._entry_price = price
        self._take_profit_price = price + limit_distance
        self._stop_loss_price = price - stop_distance
        self._position_active = True
        self._is_long_position = True
        self._last_trade_profit = 0.0
        self._max_profit = 0.0
        self._drawdown = 0.0

    def _open_short(self, price, limit_distance, stop_distance):
        self.SellMarket()
        self._entry_price = price
        self._take_profit_price = price - limit_distance
        self._stop_loss_price = price + stop_distance
        self._position_active = True
        self._is_long_position = False
        self._last_trade_profit = 0.0
        self._max_profit = 0.0
        self._drawdown = 0.0

    def OnReseted(self):
        super(umnick_trader_strategy, self).OnReseted()
        self._last_average_price = 0.0
        self._entry_price = 0.0
        self._take_profit_price = 0.0
        self._stop_loss_price = 0.0
        self._max_profit = 0.0
        self._drawdown = 0.0
        self._last_trade_profit = 0.0
        self._current_index = 0
        self._current_direction = 1
        self._cooldown_remaining = 0
        self._position_active = False
        self._is_long_position = False
        self._position_just_closed = False
        for i in range(len(self._profit_buffer)):
            self._profit_buffer[i] = 0.0
            self._loss_buffer[i] = 0.0

    def CreateClone(self):
        return umnick_trader_strategy()
