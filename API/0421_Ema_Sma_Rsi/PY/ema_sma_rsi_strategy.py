import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Array, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Messages import CandleStates
from datatype_extensions import *

class ema_sma_rsi_strategy(Strategy):
    """EMA/SMA crossover filtered by RSI.

    The strategy watches three exponential moving averages. A long trade
    is entered when the fast average crosses above the medium one while
    price is above the slow average and the candle closes bullish. Shorts
    are the mirror condition. Positions may optionally be closed after a
    user defined number of profitable bars.
    """

    def __init__(self):
        super(ema_sma_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._ema_a = self.Param("EmaALength", 10) \
            .SetDisplay("Fast EMA", "Period of the fast EMA", "Moving Averages")
        self._ema_b = self.Param("EmaBLength", 20) \
            .SetDisplay("Medium EMA", "Period of the medium EMA", "Moving Averages")
        self._ema_c = self.Param("EmaCLength", 100) \
            .SetDisplay("Slow EMA", "Period of the slow EMA", "Moving Averages")

        self._rsi_len = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "Period of RSI filter", "Oscillator")

        self._close_after = self.Param("CloseAfterXBars", True) \
            .SetDisplay("Close After Bars", "Close trade after X profitable bars", "General")
        self._x_bars = self.Param("XBars", 24) \
            .SetDisplay("X Bars", "Number of bars before forced exit", "General")

        # indicators
        self._emaA = None
        self._emaB = None
        self._emaC = None
        self._rsi = None

        # state
        self._bars_in_pos = 0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ema_sma_rsi_strategy, self).OnReseted()
        self._emaA = None
        self._emaB = None
        self._emaC = None
        self._rsi = None
        self._bars_in_pos = 0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(ema_sma_rsi_strategy, self).OnStarted(time)
        self._emaA = ExponentialMovingAverage(); self._emaA.Length = self._ema_a.Value
        self._emaB = ExponentialMovingAverage(); self._emaB.Length = self._ema_b.Value
        self._emaC = ExponentialMovingAverage(); self._emaC.Length = self._ema_c.Value
        self._rsi = RelativeStrengthIndex(); self._rsi.Length = self._rsi_len.Value

        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self._emaA, self._emaB, self._emaC, self._rsi, self._on_process).Start()

    def _on_process(self, candle, emaA, emaB, emaC, rsi):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if not (self._emaA.IsFormed and self._emaB.IsFormed and self._emaC.IsFormed and self._rsi.IsFormed):
            return

        prevA = self._emaA.GetValue(1)
        prevB = self._emaB.GetValue(1)

        is_cross_up = prevA <= prevB and emaA > emaB and candle.ClosePrice > emaC and candle.ClosePrice > candle.OpenPrice
        is_cross_down = prevA >= prevB and emaA < emaB and candle.ClosePrice < emaC and candle.ClosePrice < candle.OpenPrice

        exit_long = self.Position > 0 and rsi > 70
        exit_short = self.Position < 0 and rsi < 30

        if self.Position != 0:
            self._bars_in_pos += 1
        else:
            self._bars_in_pos = 0
            self._entry_price = 0.0

        if self._close_after.Value and self._entry_price and self._bars_in_pos >= self._x_bars.Value:
            if self.Position > 0 and candle.ClosePrice > self._entry_price:
                exit_long = True
            elif self.Position < 0 and candle.ClosePrice < self._entry_price:
                exit_short = True

        if exit_long or exit_short:
            self.ClosePosition()
            self._entry_price = 0.0
            self._bars_in_pos = 0
        elif is_cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self._entry_price = candle.ClosePrice
            self._bars_in_pos = 0
        elif is_cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._entry_price = candle.ClosePrice
            self._bars_in_pos = 0

    def CreateClone(self):
        return ema_sma_rsi_strategy()
