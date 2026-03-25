import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class gazonkos_strategy(Strategy):
    def __init__(self):
        super(gazonkos_strategy, self).__init__()
        self._take_profit = self.Param("TakeProfit", 700.0)
        self._rollback = self.Param("Rollback", 300.0)
        self._stop_loss = self.Param("StopLoss", 1000.0)
        self._delta = self.Param("Delta", 200.0)
        self._trade_volume = self.Param("TradeVolume", 0.1)
        self._first_shift = self.Param("FirstShift", 3)
        self._second_shift = self.Param("SecondShift", 2)
        self._active_trades = self.Param("ActiveTrades", 1)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._close_history = []
        self._state = 0
        self._trade_direction = 0
        self._max_price = 0.0
        self._min_price = 999999999.0
        self._can_trade = True
        self._last_trade_hour = -1
        self._last_signal_hour = -1
        self._max_history = 1

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(gazonkos_strategy, self).OnReseted()
        self._close_history = []
        self._state = 0
        self._trade_direction = 0
        self._max_price = 0.0
        self._min_price = 999999999.0
        self._can_trade = True
        self._last_trade_hour = -1
        self._last_signal_hour = -1
        self._update_history_size()

    def OnStarted(self, time):
        super(gazonkos_strategy, self).OnStarted(time)

        self._close_history = []
        self._state = 0
        self._trade_direction = 0
        self._max_price = 0.0
        self._min_price = 999999999.0
        self._can_trade = True
        self._last_trade_hour = -1
        self._last_signal_hour = -1

        self.Volume = self._trade_volume.Value
        self._update_history_size()

        self.StartProtection(
            takeProfit=Unit(float(self._take_profit.Value), UnitTypes.Absolute),
            stopLoss=Unit(float(self._stop_loss.Value), UnitTypes.Absolute),
            isStopTrailing=False,
            useMarketOrders=True)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _update_history_size(self):
        required = max(max(int(self._first_shift.Value), int(self._second_shift.Value)) + 1, 1)
        if self._max_history == required:
            return
        self._max_history = required
        if len(self._close_history) > self._max_history:
            self._close_history = self._close_history[:self._max_history]

    def _add_close(self, close):
        self._close_history.insert(0, close)
        if len(self._close_history) > self._max_history:
            self._close_history.pop()

    def _try_get_close(self, shift):
        if shift < 0:
            return None
        if len(self._close_history) <= shift:
            return None
        return self._close_history[shift]

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_history_size()
        self._add_close(float(candle.ClosePrice))

        hour = candle.CloseTime.Hour

        if self._state == 0:
            self._can_trade = True
            if self._last_trade_hour == hour:
                self._can_trade = False

            vol = float(self.Volume)
            active = int(self._active_trades.Value)
            if active > 0 and vol > 0 and abs(float(self.Position)) >= active * vol:
                self._can_trade = False

            if self._can_trade:
                self._state = 1

        if self._state == 1:
            fs = int(self._first_shift.Value)
            ss = int(self._second_shift.Value)
            close_first = self._try_get_close(fs)
            close_second = self._try_get_close(ss)
            if close_first is None or close_second is None:
                return

            delta = float(self._delta.Value)
            close = float(candle.ClosePrice)
            if close_second - close_first > delta:
                self._trade_direction = 1
                self._max_price = close
                self._last_signal_hour = hour
                self._state = 2
            elif close_first - close_second > delta:
                self._trade_direction = -1
                self._min_price = close
                self._last_signal_hour = hour
                self._state = 2

        if self._state == 2:
            if self._last_signal_hour != hour:
                self._reset_to_idle()
                return

            rollback = float(self._rollback.Value)
            high = float(candle.HighPrice)
            low = float(candle.LowPrice)

            if self._trade_direction == 1:
                if high > self._max_price:
                    self._max_price = high
                if low < self._max_price - rollback:
                    self._state = 3
            elif self._trade_direction == -1:
                if low < self._min_price:
                    self._min_price = low
                if high > self._min_price + rollback:
                    self._state = 3

        if self._state == 3:
            pos = float(self.Position)
            if self._trade_direction == 1 and pos <= 0:
                self.BuyMarket()
                self._last_trade_hour = hour
                self._reset_to_idle()
            elif self._trade_direction == -1 and pos >= 0:
                self.SellMarket()
                self._last_trade_hour = hour
                self._reset_to_idle()

    def _reset_to_idle(self):
        self._state = 0
        self._trade_direction = 0
        self._max_price = 0.0
        self._min_price = 999999999.0
        self._can_trade = True
        self._last_signal_hour = -1

    def CreateClone(self):
        return gazonkos_strategy()
