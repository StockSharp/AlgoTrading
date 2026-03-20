import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import VortexIndicator


class vortex_indicator_system_strategy(Strategy):
    """Vortex indicator crossover breakout: arms triggers on VI+/VI- cross, executes on price breakout."""

    def __init__(self):
        super(vortex_indicator_system_strategy, self).__init__()

        self._length = self.Param("Length", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Vortex Length", "Period for the Vortex indicator", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe used for analysis", "General")

        self._previous_plus = 0.0
        self._previous_minus = 0.0
        self._has_previous = False
        self._pending_buy_trigger = None
        self._pending_sell_trigger = None

    @property
    def Length(self):
        return int(self._length.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(vortex_indicator_system_strategy, self).OnStarted(time)

        self._previous_plus = 0.0
        self._previous_minus = 0.0
        self._has_previous = False
        self._pending_buy_trigger = None
        self._pending_sell_trigger = None

        self._vortex = VortexIndicator()
        self._vortex.Length = self.Length

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._vortex, self.process_candle).Start()

    def process_candle(self, candle, vortex_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._vortex.IsFormed:
            return

        vi_plus_n = vortex_value.PlusVi
        vi_minus_n = vortex_value.MinusVi
        if vi_plus_n is None or vi_minus_n is None:
            return

        vi_plus = float(vi_plus_n)
        vi_minus = float(vi_minus_n)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        # Check pending triggers
        if self._pending_buy_trigger is not None and h > self._pending_buy_trigger:
            if self.Position <= 0:
                self.BuyMarket()
            self._pending_buy_trigger = None
        elif self._pending_sell_trigger is not None and lo < self._pending_sell_trigger:
            if self.Position >= 0:
                self.SellMarket()
            self._pending_sell_trigger = None

        if not self._has_previous:
            self._previous_plus = vi_plus
            self._previous_minus = vi_minus
            self._has_previous = True
            return

        crossed_up = self._previous_plus <= self._previous_minus and vi_plus > vi_minus
        crossed_down = self._previous_plus >= self._previous_minus and vi_plus < vi_minus

        if crossed_up:
            if self.Position < 0:
                self.BuyMarket()
            self._pending_buy_trigger = h
            self._pending_sell_trigger = None
        elif crossed_down:
            if self.Position > 0:
                self.SellMarket()
            self._pending_sell_trigger = lo
            self._pending_buy_trigger = None

        self._previous_plus = vi_plus
        self._previous_minus = vi_minus

    def OnReseted(self):
        super(vortex_indicator_system_strategy, self).OnReseted()
        self._previous_plus = 0.0
        self._previous_minus = 0.0
        self._has_previous = False
        self._pending_buy_trigger = None
        self._pending_sell_trigger = None

    def CreateClone(self):
        return vortex_indicator_system_strategy()
