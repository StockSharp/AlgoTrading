import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, ExponentialMovingAverage, SmoothedMovingAverage,
    WeightedMovingAverage, TripleExponentialMovingAverage,
    KaufmanAdaptiveMovingAverage, DecimalIndicatorValue
)
from StockSharp.Algo.Strategies import Strategy


class x_didi_index_cloud_duplex_strategy(Strategy):
    def __init__(self):
        super(x_didi_index_cloud_duplex_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._fast_length = self.Param("FastLength", 3)
        self._medium_length = self.Param("MediumLength", 8)
        self._slow_length = self.Param("SlowLength", 20)
        self._signal_bar = self.Param("SignalBar", 0)
        self._enable_long_entries = self.Param("EnableLongEntries", True)
        self._enable_long_exits = self.Param("EnableLongExits", True)
        self._enable_short_entries = self.Param("EnableShortEntries", True)
        self._enable_short_exits = self.Param("EnableShortExits", True)
        self._stop_loss_points = self.Param("StopLossPoints", 1000.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000.0)

        self._fast_ma = None
        self._medium_ma = None
        self._slow_ma = None
        self._fast_history = []
        self._slow_history = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @property
    def MediumLength(self):
        return self._medium_length.Value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @property
    def SignalBar(self):
        return self._signal_bar.Value

    @property
    def EnableLongEntries(self):
        return self._enable_long_entries.Value

    @property
    def EnableLongExits(self):
        return self._enable_long_exits.Value

    @property
    def EnableShortEntries(self):
        return self._enable_short_entries.Value

    @property
    def EnableShortExits(self):
        return self._enable_short_exits.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    def OnStarted(self, time):
        super(x_didi_index_cloud_duplex_strategy, self).OnStarted(time)

        self._fast_ma = SimpleMovingAverage()
        self._fast_ma.Length = self.FastLength
        self._medium_ma = SimpleMovingAverage()
        self._medium_ma.Length = self.MediumLength
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self.SlowLength

        buf_size = max(self.SignalBar + 2, 2)
        self._fast_history = [None] * buf_size
        self._slow_history = [None] * buf_size

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_ma, self._medium_ma, self._slow_ma, self._process_candle).Start()

        sec = self.Security
        ps = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0
        sl_unit = Unit()
        tp_unit = Unit()
        if ps > 0:
            if self.StopLossPoints > 0:
                sl_unit = Unit(self.StopLossPoints * ps, UnitTypes.Absolute)
            if self.TakeProfitPoints > 0:
                tp_unit = Unit(self.TakeProfitPoints * ps, UnitTypes.Absolute)
        self.StartProtection(tp_unit, sl_unit)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_ma)
            self.DrawIndicator(area, self._medium_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_v, medium_v, slow_v):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast_ma.IsFormed or not self._medium_ma.IsFormed or not self._slow_ma.IsFormed:
            return

        med = float(medium_v)
        if med == 0:
            return

        fast_ratio = float(fast_v) / med
        slow_ratio = float(slow_v) / med

        self._update_history(self._fast_history, fast_ratio)
        self._update_history(self._slow_history, slow_ratio)

        if not self._has_signal_data():
            return

        sb = self.SignalBar
        cur_fast = self._fast_history[sb]
        cur_slow = self._slow_history[sb]
        prev_fast = self._fast_history[sb + 1]
        prev_slow = self._slow_history[sb + 1]

        open_long = False
        close_long = False
        open_short = False
        close_short = False

        if prev_fast > prev_slow and self.EnableLongEntries and cur_fast <= cur_slow:
            open_long = True
        if prev_fast < prev_slow and self.EnableLongExits:
            close_long = True
        if prev_fast < prev_slow and self.EnableShortEntries and cur_fast >= cur_slow:
            open_short = True
        if prev_fast > prev_slow and self.EnableShortExits:
            close_short = True

        if close_long and self.Position > 0:
            self.SellMarket()
        if open_long and self.Position <= 0:
            vol = self.Volume + (abs(self.Position) if self.Position < 0 else 0)
            if vol > 0:
                self.BuyMarket()

        if close_short and self.Position < 0:
            self.BuyMarket()
        if open_short and self.Position >= 0:
            vol = self.Volume + (self.Position if self.Position > 0 else 0)
            if vol > 0:
                self.SellMarket()

    def _update_history(self, buf, value):
        for i in range(len(buf) - 1, 0, -1):
            buf[i] = buf[i - 1]
        buf[0] = value

    def _has_signal_data(self):
        sb = self.SignalBar
        req = sb + 1
        if req >= len(self._fast_history) or req >= len(self._slow_history):
            return False
        return (self._fast_history[sb] is not None and
                self._fast_history[req] is not None and
                self._slow_history[sb] is not None and
                self._slow_history[req] is not None)

    def OnReseted(self):
        super(x_didi_index_cloud_duplex_strategy, self).OnReseted()
        self._fast_history = []
        self._slow_history = []

    def CreateClone(self):
        return x_didi_index_cloud_duplex_strategy()
