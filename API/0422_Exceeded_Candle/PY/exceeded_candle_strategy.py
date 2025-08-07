import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Messages import CandleStates
from datatype_extensions import *

class exceeded_candle_strategy(Strategy):
    """Exceeded Candle strategy using Bollinger Bands and engulfing patterns.

    A bullish trade is taken when a green candle fully engulfs the previous red
    candle while price remains below the middle band. Three consecutive red
    candles block new entries. Positions exit when price touches the upper band.
    """

    def __init__(self):
        super(exceeded_candle_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_length = self.Param("BBLength", 20) \
            .SetDisplay("BB Length", "Bollinger band period", "Bollinger")
        self._bb_mult = self.Param("BBMultiplier", 2.0) \
            .SetDisplay("BB Mult", "Band width multiplier", "Bollinger")

        self._bollinger = BollingerBands()
        self._prev = None
        self._prev2 = None
        self._prev3 = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exceeded_candle_strategy, self).OnReseted()
        self._bollinger = BollingerBands()
        self._prev = None
        self._prev2 = None
        self._prev3 = None

    def OnStarted(self, time):
        super(exceeded_candle_strategy, self).OnStarted(time)
        self._bollinger.Length = self._bb_length.Value
        self._bollinger.Width = self._bb_mult.Value
        sub = self.SubscribeCandles(self.candle_type)
        sub.BindEx(self._bollinger, self._on_process).Start()

    def _on_process(self, candle, boll_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        bb = boll_val
        up = bb.UpBand
        mid = bb.MovingAverage
        close = candle.ClosePrice
        open_ = candle.OpenPrice

        green_exceeded = False
        red_exceeded = False
        if self._prev is not None:
            green_exceeded = (self._prev.ClosePrice < self._prev.OpenPrice and
                              close > open_ and close > self._prev.OpenPrice)
            red_exceeded = (self._prev.ClosePrice > self._prev.OpenPrice and
                            close < open_ and close < self._prev.OpenPrice)
        last3red = (self._prev and self._prev2 and self._prev3 and
                    self._prev.ClosePrice < self._prev.OpenPrice and
                    self._prev2.ClosePrice < self._prev2.OpenPrice and
                    self._prev3.ClosePrice < self._prev3.OpenPrice)

        entry_long = green_exceeded and close < mid and not last3red
        exit_long = close > up

        if entry_long and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif exit_long and self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))

        self._prev3 = self._prev2
        self._prev2 = self._prev
        self._prev = candle

    def CreateClone(self):
        return exceeded_candle_strategy()
