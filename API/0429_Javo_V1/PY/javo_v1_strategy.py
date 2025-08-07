import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import HeikinAshi, ExponentialMovingAverage
from StockSharp.Messages import CandleStates
from datatype_extensions import *

class javo_v1_strategy(Strategy):
    """Javo v1 strategy with Heikin Ashi candles and dual EMAs."""

    def __init__(self):
        super(javo_v1_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(60)) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._fast = self.Param("FastEmaPeriod", 1) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicator")
        self._slow = self.Param("SlowEmaPeriod", 30) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicator")
        self._ha = HeikinAshi()
        self._fast_ema = ExponentialMovingAverage()
        self._slow_ema = ExponentialMovingAverage()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(javo_v1_strategy, self).OnReseted()
        self._ha = HeikinAshi()
        self._fast_ema = ExponentialMovingAverage()
        self._slow_ema = ExponentialMovingAverage()

    def OnStarted(self, time):
        super(javo_v1_strategy, self).OnStarted(time)
        self._fast_ema.Length = self._fast.Value
        self._slow_ema.Length = self._slow.Value
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._ha, self._fast_ema, self._slow_ema, self._on_process).Start()

    def _on_process(self, candle, ha_val, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        bullish = ha_val.Close > ha_val.Open
        bearish = ha_val.Close < ha_val.Open
        if bullish and fast_val > slow_val and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif bearish and fast_val < slow_val and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return javo_v1_strategy()
