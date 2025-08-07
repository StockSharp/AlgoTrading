import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Array, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Messages import CandleStates
from datatype_extensions import *

class improvisando_strategy(Strategy):
    """Improvisando strategy mixing EMA and RSI concepts."""

    def __init__(self):
        super(improvisando_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._ema_len = self.Param("EmaLength", 10) \
            .SetDisplay("EMA Length", "Trend filter period", "Indicator")
        self._rsi_len = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "Oscillator period", "Indicator")
        self._show_long = self.Param("ShowLong", True)
        self._show_short = self.Param("ShowShort", False)

        self._ema = ExponentialMovingAverage()
        self._rsi = RelativeStrengthIndex()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(improvisando_strategy, self).OnReseted()
        self._ema = ExponentialMovingAverage()
        self._rsi = RelativeStrengthIndex()

    def OnStarted(self, time):
        super(improvisando_strategy, self).OnStarted(time)
        self._ema.Length = self._ema_len.Value
        self._rsi.Length = self._rsi_len.Value
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._ema, self._rsi, self._on_process).Start()

    def _on_process(self, candle, ema_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if ema_val < candle.ClosePrice and rsi_val > 50 and self._show_long.Value and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif ema_val > candle.ClosePrice and rsi_val < 50 and self._show_short.Value and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return improvisando_strategy()
