import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, BollingerBands
from StockSharp.Algo.Strategies import Strategy


class lorenzo_super_scalp_strategy(Strategy):
    def __init__(self):
        super(lorenzo_super_scalp_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
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
        super(lorenzo_super_scalp_strategy, self).OnReseted()
        self._last_signal_ticks = 0

    def OnStarted2(self, time):
        super(lorenzo_super_scalp_strategy, self).OnStarted2(time)
        self._last_signal_ticks = 0
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        self._bb = BollingerBands()
        self._bb.Length = self._bb_length.Value
        self._bb.Width = 2
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._rsi, self._bb, self.OnProcess).Start()

    def OnProcess(self, candle, rsi_val, bb_val):
        if candle.State != CandleStates.Finished:
            return
        if not rsi_val.IsFormed or not bb_val.IsFormed:
            return
        r = float(rsi_val)
        bb_upper = bb_val.UpBand
        bb_lower = bb_val.LowBand
        if bb_upper is None or bb_lower is None:
            return
        upper = float(bb_upper)
        lower = float(bb_lower)
        close = float(candle.ClosePrice)
        cooldown_ticks = TimeSpan.FromMinutes(360).Ticks
        current_ticks = candle.OpenTime.Ticks
        if current_ticks - self._last_signal_ticks < cooldown_ticks:
            return
        if r < 45.0 and close <= lower and self.Position <= 0:
            self.BuyMarket()
            self._last_signal_ticks = current_ticks
        elif r > 55.0 and close >= upper and self.Position >= 0:
            self.SellMarket()
            self._last_signal_ticks = current_ticks

    def CreateClone(self):
        return lorenzo_super_scalp_strategy()
