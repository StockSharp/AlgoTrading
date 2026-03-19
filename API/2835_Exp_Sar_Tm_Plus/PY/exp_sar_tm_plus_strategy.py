import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy

class exp_sar_tm_plus_strategy(Strategy):
    """
    Parabolic SAR strategy with time-based exit.
    Enters on SAR crossover, exits on reverse cross, SL/TP, or time limit.
    """

    def __init__(self):
        super(exp_sar_tm_plus_strategy, self).__init__()
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Stop Loss (points)", "Stop distance in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Take Profit (points)", "Take profit in price steps", "Risk")
        self._use_time_exit = self.Param("UseTimeExit", True) \
            .SetDisplay("Enable Time Exit", "Close after holding period", "Risk")
        self._holding_minutes = self.Param("HoldingMinutes", 240) \
            .SetDisplay("Holding Minutes", "Max position holding time", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for SAR", "Data")
        self._sar_step = self.Param("SarStep", 0.02) \
            .SetDisplay("SAR Step", "Acceleration step", "Indicators")
        self._sar_max = self.Param("SarMaximum", 0.2) \
            .SetDisplay("SAR Maximum", "Maximum acceleration", "Indicators")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar Offset", "Bars for signal confirmation", "Data")

        self._close_buffer = []
        self._sar_buffer = []
        self._buffer_index = 0
        self._buffer_count = 0
        self._stop_price = None
        self._take_price = None
        self._entry_time = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_sar_tm_plus_strategy, self).OnReseted()
        self._init_buffers()
        self._reset_risk()

    def OnStarted(self, time):
        super(exp_sar_tm_plus_strategy, self).OnStarted(time)

        self._init_buffers()

        sar = ParabolicSar()
        sar.Acceleration = self._sar_step.Value
        sar.AccelerationMax = self._sar_max.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sar, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sar_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        sar_val = float(sar_val)

        self._update_buffers(close, sar_val)

        sig_bar = max(0, self._signal_bar.Value)
        if self._buffer_count <= sig_bar + 1:
            return

        vals = self._get_signal_values(sig_bar)
        if vals[0] is None or vals[1] is None or vals[2] is None or vals[3] is None:
            return

        cur_close, cur_sar, prev_close, prev_sar = vals
        is_above = cur_close > cur_sar
        was_above = prev_close > prev_sar

        self._handle_exits(candle, is_above)

        crossed_up = not was_above and is_above
        crossed_down = was_above and not is_above

        if crossed_up and self.Position <= 0:
            self._enter_long(candle)
        elif crossed_down and self.Position >= 0:
            self._enter_short(candle)

    def _init_buffers(self):
        size = max(2, max(0, self._signal_bar.Value) + 2)
        self._close_buffer = [None] * size
        self._sar_buffer = [None] * size
        self._buffer_index = 0
        self._buffer_count = 0

    def _update_buffers(self, close, sar):
        size = len(self._close_buffer)
        if size == 0:
            return
        self._close_buffer[self._buffer_index] = close
        self._sar_buffer[self._buffer_index] = sar
        self._buffer_index = (self._buffer_index + 1) % size
        if self._buffer_count < size:
            self._buffer_count += 1

    def _get_signal_values(self, sig_offset):
        size = len(self._close_buffer)
        if size == 0:
            return (None, None, None, None)
        cur_idx = (self._buffer_index - 1 - sig_offset + size) % size
        prev_idx = (self._buffer_index - 2 - sig_offset + size) % size
        return (self._close_buffer[cur_idx], self._sar_buffer[cur_idx],
                self._close_buffer[prev_idx], self._sar_buffer[prev_idx])

    def _handle_exits(self, candle, is_above):
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)

        if self.Position > 0:
            if self._should_exit_by_time(candle):
                self.SellMarket()
                self._reset_risk()
                return
            if not is_above:
                self.SellMarket()
                self._reset_risk()
                return
            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket()
                self._reset_risk()
                return
            if self._take_price is not None and high >= self._take_price:
                self.SellMarket()
                self._reset_risk()
        elif self.Position < 0:
            if self._should_exit_by_time(candle):
                self.BuyMarket()
                self._reset_risk()
                return
            if is_above:
                self.BuyMarket()
                self._reset_risk()
                return
            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket()
                self._reset_risk()
                return
            if self._take_price is not None and low <= self._take_price:
                self.BuyMarket()
                self._reset_risk()

    def _should_exit_by_time(self, candle):
        if not self._use_time_exit.Value or self._entry_time is None:
            return False
        mins = max(0, self._holding_minutes.Value)
        if mins <= 0:
            return False
        close_time = candle.CloseTime if candle.CloseTime != candle.CloseTime.__class__() else candle.OpenTime
        return (close_time - self._entry_time).TotalMinutes >= mins

    def _enter_long(self, candle):
        self._reset_risk()
        if self.Position < 0:
            self.BuyMarket()
        self.BuyMarket()

        close_time = candle.CloseTime if candle.CloseTime != candle.CloseTime.__class__() else candle.OpenTime
        self._entry_time = close_time

        ps = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
        if ps <= 0:
            ps = 1.0

        close = float(candle.ClosePrice)
        sl = self._stop_loss_points.Value
        tp = self._take_profit_points.Value
        self._stop_price = close - ps * sl if sl > 0 else None
        self._take_price = close + ps * tp if tp > 0 else None

    def _enter_short(self, candle):
        self._reset_risk()
        if self.Position > 0:
            self.SellMarket()
        self.SellMarket()

        close_time = candle.CloseTime if candle.CloseTime != candle.CloseTime.__class__() else candle.OpenTime
        self._entry_time = close_time

        ps = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
        if ps <= 0:
            ps = 1.0

        close = float(candle.ClosePrice)
        sl = self._stop_loss_points.Value
        tp = self._take_profit_points.Value
        self._stop_price = close + ps * sl if sl > 0 else None
        self._take_price = close - ps * tp if tp > 0 else None

    def _reset_risk(self):
        self._stop_price = None
        self._take_price = None
        self._entry_time = None

    def CreateClone(self):
        return exp_sar_tm_plus_strategy()
