import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

class gazonkos_expert_strategy(Strategy):
    """
    Gazonkos Expert: momentum pullback strategy.
    Detects impulse via close difference, waits for retracement,
    then enters. Uses StartProtection for SL/TP.
    """

    def __init__(self):
        super(gazonkos_expert_strategy, self).__init__()
        self._take_profit_pips = self.Param("TakeProfitPips", 16.0) \
            .SetDisplay("Take Profit (pips)", "Distance to take profit level", "Risk")
        self._retracement_pips = self.Param("RetracementPips", 16.0) \
            .SetDisplay("Retracement (pips)", "Pullback distance for confirmation", "Signals")
        self._stop_loss_pips = self.Param("StopLossPips", 40.0) \
            .SetDisplay("Stop Loss (pips)", "Distance to protective stop", "Risk")
        self._t1_shift = self.Param("T1Shift", 3) \
            .SetDisplay("T1 Shift", "Older reference close index", "Signals")
        self._t2_shift = self.Param("T2Shift", 2) \
            .SetDisplay("T2 Shift", "Newer reference close index", "Signals")
        self._delta_pips = self.Param("DeltaPips", 40.0) \
            .SetDisplay("Delta (pips)", "Minimum distance between reference closes", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Timeframe for momentum signal", "General")

        self._close_history = []
        self._state = 0  # 0=WaitSlot, 1=WaitImpulse, 2=MonitorRetracement, 3=Execute
        self._pending_direction = None
        self._extreme_price = 0.0
        self._last_trade_hour = None
        self._last_signal_hour = None
        self._point_value = 0.0001

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gazonkos_expert_strategy, self).OnReseted()
        self._close_history = []
        self._state = 0
        self._pending_direction = None
        self._extreme_price = 0.0
        self._last_trade_hour = None
        self._last_signal_hour = None

    def OnStarted(self, time):
        super(gazonkos_expert_strategy, self).OnStarted(time)

        ps = 0.0001
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
        if ps <= 0:
            ps = 0.0001
        self._point_value = ps

        tp = self._take_profit_pips.Value * ps
        sl = self._stop_loss_pips.Value * ps

        if tp > 0 and sl > 0:
            self.StartProtection(
                Unit(tp, UnitTypes.Absolute),
                Unit(sl, UnitTypes.Absolute))

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        capacity = max(self._t1_shift.Value, self._t2_shift.Value) + 5
        self._close_history.append(close)
        while len(self._close_history) > capacity:
            self._close_history.pop(0)

        t1 = self._t1_shift.Value
        t2 = self._t2_shift.Value
        if len(self._close_history) - 1 - t1 < 0 or len(self._close_history) - 1 - t2 < 0:
            return

        t1_close = self._close_history[len(self._close_history) - 1 - t1]
        t2_close = self._close_history[len(self._close_history) - 1 - t2]
        hour = candle.CloseTime.Hour

        if self._state == 0:
            if self._can_start_new_cycle(hour):
                self._state = 1

        if self._state == 1:
            delta_threshold = self._delta_pips.Value * self._point_value
            if delta_threshold <= 0:
                return

            diff = t2_close - t1_close
            if diff > delta_threshold:
                self._pending_direction = 1  # buy
                self._extreme_price = max(high, close)
                self._last_signal_hour = hour
                self._state = 2
            elif -diff > delta_threshold:
                self._pending_direction = -1  # sell
                self._extreme_price = min(low, close) if low > 0 else close
                self._last_signal_hour = hour
                self._state = 2

        if self._state == 2:
            if self._pending_direction is None:
                self._reset_state()
                return

            if self._last_signal_hour is not None and self._last_signal_hour != hour:
                self._reset_state()
                return

            retracement = self._retracement_pips.Value * self._point_value
            if retracement <= 0:
                self._reset_state()
                return

            if self._pending_direction == 1:
                self._extreme_price = max(self._extreme_price, max(high, close))
                if close <= self._extreme_price - retracement:
                    self._state = 3
            elif self._pending_direction == -1:
                if self._extreme_price <= 0:
                    self._extreme_price = low
                self._extreme_price = min(self._extreme_price, min(low, close))
                if close >= self._extreme_price + retracement:
                    self._state = 3

        if self._state == 3:
            if self._pending_direction is None:
                self._reset_state()
                return

            if not self._can_start_new_cycle(hour):
                self._reset_state()
                return

            if self._pending_direction == 1:
                self.BuyMarket()
                self._last_trade_hour = hour
            elif self._pending_direction == -1:
                self.SellMarket()
                self._last_trade_hour = hour

            self._reset_state()

    def _can_start_new_cycle(self, hour):
        if self._last_trade_hour is not None and self._last_trade_hour == hour:
            return False
        return True

    def _reset_state(self):
        self._state = 0
        self._pending_direction = None
        self._extreme_price = 0.0
        self._last_signal_hour = None

    def CreateClone(self):
        return gazonkos_expert_strategy()
