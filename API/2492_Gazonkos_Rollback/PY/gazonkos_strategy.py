import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

class gazonkos_strategy(Strategy):
    """
    Gazonkos Rollback: momentum breakout with rollback confirmation.
    Waits for spread between two historical closes, then joins
    the trend after a pullback. Uses StartProtection for SL/TP.
    """

    def __init__(self):
        super(gazonkos_strategy, self).__init__()
        self._take_profit = self.Param("TakeProfit", 700.0) \
            .SetDisplay("Take Profit", "Take profit distance in price units", "Risk Management")
        self._rollback = self.Param("Rollback", 300.0) \
            .SetDisplay("Rollback", "Required pullback before entering", "Signals")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss distance in price units", "Risk Management")
        self._delta = self.Param("Delta", 200.0) \
            .SetDisplay("Delta", "Minimum difference between closes", "Signals")
        self._first_shift = self.Param("FirstShift", 3) \
            .SetDisplay("First Shift", "Older close shift for comparison", "Signals")
        self._second_shift = self.Param("SecondShift", 2) \
            .SetDisplay("Second Shift", "Recent close shift for comparison", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle series used for signals", "General")

        self._close_history = []
        self._state = 0
        self._trade_direction = 0
        self._max_price = 0.0
        self._min_price = float('inf')
        self._last_trade_hour = -1
        self._last_signal_hour = -1

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gazonkos_strategy, self).OnReseted()
        self._close_history = []
        self._state = 0
        self._trade_direction = 0
        self._max_price = 0.0
        self._min_price = float('inf')
        self._last_trade_hour = -1
        self._last_signal_hour = -1

    def OnStarted(self, time):
        super(gazonkos_strategy, self).OnStarted(time)

        tp = self._take_profit.Value
        sl = self._stop_loss.Value
        self.StartProtection(
            Unit(tp, UnitTypes.Absolute),
            Unit(sl, UnitTypes.Absolute))

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        max_history = max(self._first_shift.Value, self._second_shift.Value) + 1
        self._close_history.insert(0, close)
        while len(self._close_history) > max_history:
            self._close_history.pop()

        hour = candle.CloseTime.Hour

        if self._state == 0:
            can_trade = True
            if self._last_trade_hour == hour:
                can_trade = False
            if can_trade:
                self._state = 1

        if self._state == 1:
            fs = self._first_shift.Value
            ss = self._second_shift.Value
            if len(self._close_history) <= fs or len(self._close_history) <= ss:
                return

            close_first = self._close_history[fs]
            close_second = self._close_history[ss]
            delta = self._delta.Value

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

            rollback = self._rollback.Value
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
            if self._trade_direction == 1 and self.Position <= 0:
                self.BuyMarket()
                self._last_trade_hour = hour
                self._reset_to_idle()
            elif self._trade_direction == -1 and self.Position >= 0:
                self.SellMarket()
                self._last_trade_hour = hour
                self._reset_to_idle()

    def _reset_to_idle(self):
        self._state = 0
        self._trade_direction = 0
        self._max_price = 0.0
        self._min_price = float('inf')
        self._last_signal_hour = -1

    def CreateClone(self):
        return gazonkos_strategy()
