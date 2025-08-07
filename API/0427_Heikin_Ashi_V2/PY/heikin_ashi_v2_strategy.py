import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import HeikinAshi, ExponentialMovingAverage
from StockSharp.Messages import CandleStates
from datatype_extensions import *

class heikin_ashi_v2_strategy(Strategy):
    """Heikin Ashi V2 strategy.

    Adds an EMA trend filter to the universal Heikin Ashi approach. Trades only
    when the HA candle direction aligns with the EMA trend.
    """

    def __init__(self):
        super(heikin_ashi_v2_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._ema_len = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "Period of EMA filter", "Indicator")
        self._ha = HeikinAshi()
        self._ema = ExponentialMovingAverage()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(heikin_ashi_v2_strategy, self).OnReseted()
        self._ha = HeikinAshi()
        self._ema = ExponentialMovingAverage()

    def OnStarted(self, time):
        super(heikin_ashi_v2_strategy, self).OnStarted(time)
        self._ema.Length = self._ema_len.Value
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._ha, self._ema, self._on_process).Start()

    def _on_process(self, candle, ha_val, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        open_ = ha_val.Open
        close = ha_val.Close
        if close > open_ and candle.ClosePrice > ema_val and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif close < open_ and candle.ClosePrice < ema_val and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return heikin_ashi_v2_strategy()
