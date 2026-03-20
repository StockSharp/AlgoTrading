import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, BollingerBands
from StockSharp.Algo.Strategies import Strategy


class octopus_nest_strategy(Strategy):
    def __init__(self):
        super(octopus_nest_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 100) \
            .SetGreaterThanZero()
        self._bb_length = self.Param("BbLength", 20) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._last_signal_ticks = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(octopus_nest_strategy, self).OnReseted()
        self._last_signal_ticks = 0

    def OnStarted(self, time):
        super(octopus_nest_strategy, self).OnStarted(time)
        self._last_signal_ticks = 0
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_length.Value
        self._bb = BollingerBands()
        self._bb.Length = self._bb_length.Value
        self._bb.Width = 2
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._ema, self._bb, self.OnProcess).Start()

    def OnProcess(self, candle, ema_value, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if not ema_value.IsFormed or not bb_value.IsFormed:
            return
        ema_val = float(ema_value.ToDecimal())
        bb_upper = bb_value.UpBand
        bb_lower = bb_value.LowBand
        if bb_upper is None or bb_lower is None:
            return
        bb_upper = float(bb_upper)
        bb_lower = float(bb_lower)
        close = float(candle.ClosePrice)
        bb_width = bb_upper - bb_lower
        squeeze = bb_width < close * 0.01
        cooldown_ticks = TimeSpan.FromMinutes(360).Ticks
        current_ticks = candle.OpenTime.Ticks
        if squeeze or current_ticks - self._last_signal_ticks < cooldown_ticks:
            return
        if close > ema_val and close > bb_upper and self.Position <= 0:
            self.BuyMarket()
            self._last_signal_ticks = current_ticks
        elif close < ema_val and close < bb_lower and self.Position >= 0:
            self.SellMarket()
            self._last_signal_ticks = current_ticks

    def CreateClone(self):
        return octopus_nest_strategy()
