import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class h4_l4_breakout_strategy(Strategy):
    def __init__(self):
        super(h4_l4_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._last_signal = 0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(h4_l4_breakout_strategy, self).OnStarted(time)

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._last_signal = 0
        self._has_prev = False

        sma = SimpleMovingAverage()
        sma.Length = 10

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ma = float(ma_value)

        if not self._has_prev:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            self._has_prev = True
            return

        if close > self._prev_high and close > ma and self._last_signal != 1 and self.Position <= 0:
            self.BuyMarket()
            self._last_signal = 1
        elif close < self._prev_low and close < ma and self._last_signal != -1 and self.Position >= 0:
            self.SellMarket()
            self._last_signal = -1

        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)

    def OnReseted(self):
        super(h4_l4_breakout_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._last_signal = 0
        self._has_prev = False

    def CreateClone(self):
        return h4_l4_breakout_strategy()
