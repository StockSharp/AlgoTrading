import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class price_extreme_strategy(Strategy):
    def __init__(self):
        super(price_extreme_strategy, self).__init__()

        self._level_length = self.Param("LevelLength", 20)
        self._signal_shift = self.Param("SignalShift", 1)
        self._enable_long = self.Param("EnableLong", True)
        self._enable_short = self.Param("EnableShort", True)
        self._reverse_signals = self.Param("ReverseSignals", False)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._highs = []
        self._lows = []
        self._history = []
        self._prev_upper = 0.0
        self._prev_lower = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LevelLength(self):
        return self._level_length.Value

    @property
    def SignalShift(self):
        return self._signal_shift.Value

    @property
    def EnableLong(self):
        return self._enable_long.Value

    @property
    def EnableShort(self):
        return self._enable_short.Value

    @property
    def ReverseSignals(self):
        return self._reverse_signals.Value

    def OnStarted(self, time):
        super(price_extreme_strategy, self).OnStarted(time)

        self._highs = []
        self._lows = []
        self._history = []
        self._prev_upper = 0.0
        self._prev_lower = 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        self.StartProtection(
            Unit(3, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        self._highs.append(h)
        self._lows.append(l)
        self._history.append(c)

        max_hist = max(self.LevelLength + self.SignalShift + 2, 10)
        while len(self._history) > max_hist:
            self._history.pop(0)
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < self.LevelLength:
            return

        upper = max(self._highs[-self.LevelLength:])
        lower = min(self._lows[-self.LevelLength:])

        if len(self._history) < self.SignalShift:
            self._prev_upper = upper
            self._prev_lower = lower
            return

        breakout_up = c > self._prev_upper and self._prev_upper > 0
        breakout_down = c < self._prev_lower and self._prev_lower > 0

        self._prev_upper = upper
        self._prev_lower = lower

        want_long = breakout_down if self.ReverseSignals else breakout_up
        want_short = breakout_up if self.ReverseSignals else breakout_down

        if want_long and self.EnableLong and self.Position == 0:
            self.BuyMarket()
        elif want_short and self.EnableShort and self.Position == 0:
            self.SellMarket()

    def OnReseted(self):
        super(price_extreme_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._history = []
        self._prev_upper = 0.0
        self._prev_lower = 0.0

    def CreateClone(self):
        return price_extreme_strategy()
